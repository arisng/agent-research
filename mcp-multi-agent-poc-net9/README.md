# MCP Multi-Agent POC with .NET 9

A proof-of-concept demonstrating multi-agent systems using .NET 9, Microsoft.Extensions.AI, and the Model Context Protocol (MCP) pattern. This project shows how to build intelligent agents that can search the internet and manage SQL Server databases, coordinated through a simple multi-agent system.

## Overview

This POC demonstrates:

- **Multi-agent architecture** with specialized agents for different tasks
- **MCP server pattern** for internet search (DuckDuckGo) and SQL Server management
- **Microsoft.Extensions.AI** integration for LLM-powered agent responses
- **Minimal ASP.NET Core Web API** with OpenAPI support
- **Docker container publishing** support for easy deployment
- **Working demo application** to showcase agent capabilities

## Architecture

### Components

1. **McpMultiAgent.SearchServer** - DuckDuckGo search MCP server
   - Free API integration (no key required)
   - Returns structured search results: abstracts, answers, definitions, related topics

2. **McpMultiAgent.SqlServer** - SQL Server management MCP server
   - Create databases and tables
   - Execute SELECT queries
   - List databases and tables
   - Built-in SQL injection protection

3. **McpMultiAgent.AgentCore** - Agent framework
   - **SearchAgent**: Processes search queries using DuckDuckGo + LLM summarization
   - **DatabaseAgent**: Manages SQL Server operations with natural language understanding
   - **MultiAgentCoordinator**: Routes requests to appropriate agents
   - **MockChatClient**: Demo LLM client (replace with OpenAI/Azure OpenAI in production)

4. **McpMultiAgent.WebApi** - RESTful API
   - POST `/api/search` - Search the internet
   - POST `/api/database` - Execute database operations
   - POST `/api/agent` - Use multi-agent coordinator
   - GET `/api/health` - Health check

5. **McpMultiAgent.Demo** - Console demo application

### Agent Workflow

```
User Request
     ↓
MultiAgentCoordinator (routing logic)
     ↓
     ├─→ SearchAgent → DuckDuckGoSearchServer → Internet Search Results
     │                      ↓
     │                    LLM Summary
     │
     └─→ DatabaseAgent → SqlServerManagementServer → SQL Operations
                              ↓
                            Result
```

## Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- (Optional) SQL Server for database operations
- (Optional) OpenAI API key for production LLM integration

### Build and Run

```bash
# Clone the repository
git clone https://github.com/arisng/agent-research.git
cd agent-research/mcp-multi-agent-poc-net9

# Build the solution
dotnet build

# Run the demo application
dotnet run --project src/McpMultiAgent.Demo

# Run the web API
dotnet run --project src/McpMultiAgent.WebApi
```

### API Examples

```bash
# Search the internet
curl -X POST http://localhost:5000/api/search \
  -H "Content-Type: application/json" \
  -d '{"query": "What is .NET 9?"}'

# List databases (requires SQL Server)
curl -X POST http://localhost:5000/api/database \
  -H "Content-Type: application/json" \
  -d '{"request": "List all databases"}'

# Use multi-agent coordinator
curl -X POST http://localhost:5000/api/agent \
  -H "Content-Type: application/json" \
  -d '{"request": "Search for Microsoft Agent Framework"}'

# Health check
curl http://localhost:5000/api/health
```

### Docker Container

Build and run as a Docker container:

```bash
# Build container image
dotnet publish src/McpMultiAgent.WebApi \
  --os linux --arch x64 /t:PublishContainer

# Run container
docker run -p 8080:8080 mcp-multi-agent-poc:latest
```

## Configuration

### SQL Server Connection

Edit `appsettings.json` to configure your SQL Server:

```json
{
  "ConnectionStrings": {
    "SqlServer": "Server=your-server;Database=TestDB;Integrated Security=true;TrustServerCertificate=true;"
  }
}
```

### LLM Provider (Production)

Replace `MockChatClient` in `Program.cs` with a real LLM provider:

```csharp
// Example with OpenAI
using OpenAI;

builder.Services.AddSingleton<IChatClient>(sp =>
{
    var apiKey = builder.Configuration["OpenAI:ApiKey"];
    return new OpenAIClient(apiKey)
        .AsChatClient("gpt-4");
});
```

Or with Azure OpenAI:

```csharp
// Example with Azure OpenAI
using Azure.AI.OpenAI;
using Azure;

builder.Services.AddSingleton<IChatClient>(sp =>
{
    var endpoint = new Uri(builder.Configuration["AzureOpenAI:Endpoint"]);
    var credential = new AzureKeyCredential(builder.Configuration["AzureOpenAI:ApiKey"]);
    var deployment = builder.Configuration["AzureOpenAI:DeploymentName"];
    
    return new AzureOpenAIClient(endpoint, credential)
        .AsChatClient(deployment);
});
```

## Project Structure

```
mcp-multi-agent-poc-net9/
├── src/
│   ├── McpMultiAgent.WebApi/          # ASP.NET Core Web API
│   │   ├── Program.cs                 # API endpoints & DI setup
│   │   ├── appsettings.json           # Configuration
│   │   └── McpMultiAgent.WebApi.csproj
│   ├── McpMultiAgent.SearchServer/    # DuckDuckGo search server
│   │   ├── DuckDuckGoSearchServer.cs
│   │   └── McpMultiAgent.SearchServer.csproj
│   ├── McpMultiAgent.SqlServer/       # SQL Server management
│   │   ├── SqlServerManagementServer.cs
│   │   └── McpMultiAgent.SqlServer.csproj
│   ├── McpMultiAgent.AgentCore/       # Agent implementations
│   │   ├── Agents.cs                  # SearchAgent, DatabaseAgent, Coordinator
│   │   └── McpMultiAgent.AgentCore.csproj
│   └── McpMultiAgent.Demo/            # Console demo
│       ├── Program.cs
│       └── McpMultiAgent.Demo.csproj
├── McpMultiAgent.sln                  # Solution file
├── notes.md                            # Development notes
└── README.md                           # This file
```

## Key Features

### 1. Internet Search with DuckDuckGo

The SearchAgent uses DuckDuckGo's free Instant Answer API to search the internet:

```csharp
var searchAgent = new SearchAgent(searchServer, chatClient);
var result = await searchAgent.ProcessQueryAsync("What is Microsoft Agent Framework?");
```

Returns AI-summarized results based on:
- Abstract text from Wikipedia/sources
- Instant answers
- Definitions
- Related topics

### 2. SQL Server Management

The DatabaseAgent handles natural language database requests:

```csharp
var dbAgent = new DatabaseAgent(sqlServer, chatClient);

// List databases
await dbAgent.ProcessRequestAsync("Show me all databases");

// Create database
await dbAgent.ProcessRequestAsync("Create a database called ProductionDB");

// Create table
await dbAgent.ProcessRequestAsync("Create table Users with columns Id, Name, Email");

// Query data
await dbAgent.ProcessRequestAsync("Select all from Users table");
```

### 3. Multi-Agent Coordination

The coordinator automatically routes requests:

```csharp
var coordinator = new MultiAgentCoordinator(searchAgent, dbAgent, chatClient);

// Routes to SearchAgent
await coordinator.ProcessRequestAsync("What is .NET 9?");

// Routes to DatabaseAgent
await coordinator.ProcessRequestAsync("List all tables");
```

## Technologies Used

- **[.NET 9](https://dotnet.microsoft.com/download/dotnet/9.0)** - Latest .NET runtime
- **[Microsoft.Extensions.AI](https://www.nuget.org/packages/Microsoft.Extensions.AI/)** - AI abstraction layer (preview)
- **[Microsoft.Extensions.AI.OpenAI](https://www.nuget.org/packages/Microsoft.Extensions.AI.OpenAI/)** - OpenAI integration
- **[ModelContextProtocol](https://www.nuget.org/packages/ModelContextProtocol/)** - MCP SDK (preview)
- **[Microsoft.Data.SqlClient](https://www.nuget.org/packages/Microsoft.Data.SqlClient/)** - SQL Server connectivity
- **ASP.NET Core Minimal APIs** - Lightweight web framework
- **OpenAPI** - API documentation

## Limitations & Future Enhancements

### Current Limitations

1. **Mock LLM Client**: Uses placeholder responses instead of real AI
2. **Simple Routing**: Heuristic-based agent selection (could use LLM for smarter routing)
3. **No Authentication**: Public endpoints (add JWT/API keys for production)
4. **Limited Error Handling**: Basic error responses
5. **MCP Integration**: MCP SDK included but not fully utilized for inter-process communication

### Future Enhancements

- [ ] Integrate real LLM provider (OpenAI/Azure OpenAI/Anthropic)
- [ ] Implement full MCP protocol for server communication
- [ ] Add authentication and authorization
- [ ] Implement agent memory/conversation history
- [ ] Add streaming responses
- [ ] Create web UI for agent interaction
- [ ] Add unit and integration tests
- [ ] Implement rate limiting
- [ ] Add comprehensive logging and monitoring
- [ ] Support additional data sources (PostgreSQL, MongoDB, etc.)
- [ ] Add function calling/tool use capabilities

## References

- [Microsoft Agent Framework Overview](https://learn.microsoft.com/en-us/agent-framework/overview/agent-framework-overview)
- [Model Context Protocol - C# SDK](https://github.com/modelcontextprotocol/csharp-sdk)
- [Microsoft.Extensions.AI Documentation](https://learn.microsoft.com/en-us/dotnet/ai/ai-extensions)
- [.NET 9 What's New](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-9/overview)
- [DuckDuckGo Instant Answer API](https://duckduckgo.com/api)

## License

This is a proof-of-concept project for research purposes.

## Contributing

This project is part of the [agent-research](https://github.com/arisng/agent-research) repository. See the main repository for contribution guidelines.

## Author

Created as an AI-generated research project exploring multi-agent systems with .NET 9 and the Model Context Protocol.
