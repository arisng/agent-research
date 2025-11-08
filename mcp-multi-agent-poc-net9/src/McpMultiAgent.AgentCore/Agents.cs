using Microsoft.Extensions.AI;
using System.Text;
using McpMultiAgent.SearchServer;
using McpMultiAgent.SqlServer;

namespace McpMultiAgent.AgentCore;

/// <summary>
/// Simple mock chat client for demonstration purposes
/// In production, replace with actual OpenAI or Azure OpenAI client
/// </summary>
public class MockChatClient : IChatClient
{
    public ChatClientMetadata Metadata => new("mock-client");

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var lastMessage = chatMessages.LastOrDefault()?.Text ?? "";
        var mockResponse = $"Mock response to: {lastMessage.Substring(0, Math.Min(50, lastMessage.Length))}...";
        
        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, mockResponse));
        return Task.FromResult(response);
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        yield break;
    }

    public object? GetService(Type serviceType, object? key = null) => null;

    public void Dispose() { }
}

/// <summary>
/// Agent that can perform internet searches using DuckDuckGo
/// </summary>
public class SearchAgent
{
    private readonly DuckDuckGoSearchServer _searchServer;
    private readonly IChatClient _chatClient;

    public SearchAgent(DuckDuckGoSearchServer searchServer, IChatClient chatClient)
    {
        _searchServer = searchServer ?? throw new ArgumentNullException(nameof(searchServer));
        _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
    }

    public async Task<string> ProcessQueryAsync(string userQuery, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userQuery))
            throw new ArgumentException("Query cannot be empty", nameof(userQuery));

        // Perform the search
        var searchResults = await _searchServer.SearchAsync(userQuery, cancellationToken);

        // Use LLM to summarize and format the results
        var prompt = $@"Based on these search results, answer the question:

Question: {userQuery}

Results: {searchResults.Substring(0, Math.Min(500, searchResults.Length))}";

        var messages = new List<ChatMessage> { new(ChatRole.User, prompt) };
        var response = await _chatClient.GetResponseAsync(messages, null, cancellationToken);
        
        return response.Messages.LastOrDefault()?.Text ?? searchResults;
    }
}

/// <summary>
/// Agent that can manage SQL Server databases
/// </summary>
public class DatabaseAgent
{
    private readonly SqlServerManagementServer _sqlServer;
    private readonly IChatClient _chatClient;

    public DatabaseAgent(SqlServerManagementServer sqlServer, IChatClient chatClient)
    {
        _sqlServer = sqlServer ?? throw new ArgumentNullException(nameof(sqlServer));
        _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
    }

    public async Task<string> ProcessRequestAsync(string userRequest, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userRequest))
            throw new ArgumentException("Request cannot be empty", nameof(userRequest));

        var lowerRequest = userRequest.ToLowerInvariant();
        string result;

        if (lowerRequest.Contains("list database") || lowerRequest.Contains("show database"))
        {
            result = await _sqlServer.ListDatabasesAsync(cancellationToken);
        }
        else if (lowerRequest.Contains("list table") || lowerRequest.Contains("show table"))
        {
            result = await _sqlServer.ListTablesAsync(cancellationToken);
        }
        else if (lowerRequest.Contains("create database"))
        {
            var extractPrompt = $"Extract database name from: {userRequest}";
            var messages = new List<ChatMessage> { new(ChatRole.User, extractPrompt) };
            var response = await _chatClient.GetResponseAsync(messages, null, cancellationToken);
            var dbName = (response.Messages.LastOrDefault()?.Text ?? "TestDB").Trim().Split(' ').Last();
            
            result = await _sqlServer.CreateDatabaseAsync(dbName, cancellationToken);
        }
        else if (lowerRequest.Contains("create table"))
        {
            var extractPrompt = $"Extract table name and columns from: {userRequest}";
            var messages = new List<ChatMessage> { new(ChatRole.User, extractPrompt) };
            var response = await _chatClient.GetResponseAsync(messages, null, cancellationToken);
            var parts = (response.Messages.LastOrDefault()?.Text ?? "TestTable|Id INT").Split('|');
            var tableName = parts.Length > 0 ? parts[0].Trim() : "TestTable";
            var columns = parts.Length > 1 ? parts[1].Trim() : "Id INT PRIMARY KEY";
            
            result = await _sqlServer.CreateTableAsync(tableName, columns, cancellationToken);
        }
        else if (lowerRequest.Contains("select ") || lowerRequest.Contains("query"))
        {
            var extractPrompt = $"Extract SQL query from: {userRequest}";
            var messages = new List<ChatMessage> { new(ChatRole.User, extractPrompt) };
            var response = await _chatClient.GetResponseAsync(messages, null, cancellationToken);
            var query = response.Messages.LastOrDefault()?.Text ?? "SELECT 1";
            
            result = await _sqlServer.QueryAsync(query, cancellationToken);
        }
        else
        {
            result = "Supported operations:\n- List databases\n- List tables\n- Create database\n- Create table\n- Run SELECT query";
        }

        return result;
    }
}

/// <summary>
/// Coordinator that orchestrates multiple agents to handle complex queries
/// </summary>
public class MultiAgentCoordinator
{
    private readonly SearchAgent _searchAgent;
    private readonly DatabaseAgent _databaseAgent;
    private readonly IChatClient _chatClient;

    public MultiAgentCoordinator(SearchAgent searchAgent, DatabaseAgent databaseAgent, IChatClient chatClient)
    {
        _searchAgent = searchAgent ?? throw new ArgumentNullException(nameof(searchAgent));
        _databaseAgent = databaseAgent ?? throw new ArgumentNullException(nameof(databaseAgent));
        _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
    }

    public async Task<string> ProcessRequestAsync(string userRequest, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userRequest))
            throw new ArgumentException("Request cannot be empty", nameof(userRequest));

        // Use simple heuristics for routing
        var lower = userRequest.ToLowerInvariant();
        var isDatabase = lower.Contains("database") || lower.Contains("table") || lower.Contains("sql") || lower.Contains("query");
        var isSearch = lower.Contains("search") || lower.Contains("find") || lower.Contains("what is") || lower.Contains("who is");

        var results = new StringBuilder();

        if (isDatabase && !isSearch)
        {
            var dbResult = await _databaseAgent.ProcessRequestAsync(userRequest, cancellationToken);
            results.AppendLine(dbResult);
        }
        else if (isSearch && !isDatabase)
        {
            var searchResult = await _searchAgent.ProcessQueryAsync(userRequest, cancellationToken);
            results.AppendLine(searchResult);
        }
        else
        {
            // Default to search for general queries
            var searchResult = await _searchAgent.ProcessQueryAsync(userRequest, cancellationToken);
            results.AppendLine(searchResult);
        }

        return results.ToString();
    }
}
