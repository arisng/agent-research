# MCP Multi-Agent POC with .NET 9 - Development Notes

## Project Goal
Build a proof-of-concept .NET 9 application demonstrating Microsoft Agent Framework with MCP (Model Context Protocol) server integration for multi-agent systems. Include sample MCP servers for internet search (DuckDuckGo) and MS SQL Server management.

## Initial Planning

### Architecture Design
- ASP.NET Core 9.0 minimal web API
- Microsoft Agent Framework for agent orchestration
- MCP C# SDK for Model Context Protocol server integration
- Two sample MCP servers:
  1. Internet Search (DuckDuckGo free API)
  2. MS SQL Server management (create DB, create table, query operations)
- Cloud LLM integration (Azure OpenAI or OpenAI)
- Docker container support via `dotnet publish --os linux --arch x64 /t:PublishContainer`
- No authentication required for POC

### Project Structure
```
mcp-multi-agent-poc-net9/
├── src/
│   ├── McpMultiAgent.WebApi/          # Main web API project
│   ├── McpMultiAgent.SearchServer/    # DuckDuckGo search MCP server
│   ├── McpMultiAgent.SqlServer/       # MS SQL management MCP server
│   └── McpMultiAgent.AgentCore/       # Shared agent framework code
├── tests/
│   └── McpMultiAgent.Tests/           # Integration tests
├── notes.md                            # This file
├── README.md                           # Final documentation
└── McpMultiAgent.sln                  # Solution file
```

### Development Steps
1. Create .NET 9 solution and project structure
2. Add Microsoft Agent Framework packages
3. Add MCP C# SDK
4. Implement DuckDuckGo search MCP server
5. Implement MS SQL management MCP server
6. Create agent framework integration
7. Build multi-agent coordinator
8. Add minimal web API endpoints
9. Configure LLM providers
10. Add Docker publishing support
11. Create demos and tests
12. Document findings

## Session Log

### Session Start
Starting implementation of MCP multi-agent POC with .NET 9...

