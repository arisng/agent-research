using McpMultiAgent.AgentCore;
using McpMultiAgent.SearchServer;
using McpMultiAgent.SqlServer;
using Microsoft.Extensions.AI;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddOpenApi();

// Register MCP servers
builder.Services.AddSingleton(sp => new DuckDuckGoSearchServer());
builder.Services.AddSingleton(sp => new SqlServerManagementServer(
    builder.Configuration.GetConnectionString("SqlServer") ?? 
    "Server=localhost;Database=TestDB;Integrated Security=true;TrustServerCertificate=true;"));

// Register chat client (mock for POC - replace with OpenAI client in production)
builder.Services.AddSingleton<IChatClient>(sp => new MockChatClient());

// Register agents
builder.Services.AddSingleton<SearchAgent>();
builder.Services.AddSingleton<DatabaseAgent>();
builder.Services.AddSingleton<MultiAgentCoordinator>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Agent API endpoints
app.MapPost("/api/search", async (SearchRequest request, SearchAgent agent, CancellationToken ct) =>
{
    try
    {
        var result = await agent.ProcessQueryAsync(request.Query, ct);
        return Results.Ok(new { result });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("Search")
.WithDescription("Search the internet using DuckDuckGo and get AI-summarized results");

app.MapPost("/api/database", async (DatabaseRequest request, DatabaseAgent agent, CancellationToken ct) =>
{
    try
    {
        var result = await agent.ProcessRequestAsync(request.Request, ct);
        return Results.Ok(new { result });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("Database")
.WithDescription("Execute SQL Server operations");

app.MapPost("/api/agent", async (AgentRequest request, MultiAgentCoordinator coordinator, CancellationToken ct) =>
{
    try
    {
        var result = await coordinator.ProcessRequestAsync(request.Request, ct);
        return Results.Ok(new { result });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("Agent")
.WithDescription("Process requests using multi-agent coordinator");

app.MapGet("/api/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    service = "MCP Multi-Agent POC"
}))
.WithName("Health");

app.Run();

// Request models
record SearchRequest(string Query);
record DatabaseRequest(string Request);
record AgentRequest(string Request);

