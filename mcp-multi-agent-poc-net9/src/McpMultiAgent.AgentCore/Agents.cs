using Microsoft.Extensions.AI;
using System.Text;
using McpMultiAgent.SearchServer;
using McpMultiAgent.SqlServer;

namespace McpMultiAgent.AgentCore;

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
        var prompt = $@"You are a helpful assistant. Based on the search results provided, 
answer the user's question concisely and accurately. If the search results don't contain relevant information, 
say so clearly.

User Question: {userQuery}

Search Results:
{searchResults}";

        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, prompt)
        };

        var response = await _chatClient.CompleteAsync(messages, options: null, cancellationToken);
        
        return response.Message.Text ?? "Unable to generate a response";
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

        // Simple pattern matching for common operations
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
            // Extract database name using LLM
            var extractPrompt = $@"Extract the database name from this request. Return only the database name, nothing else.

Request: {userRequest}";

            var messages = new List<ChatMessage> { new(ChatRole.User, extractPrompt) };
            var response = await _chatClient.CompleteAsync(messages, options: null, cancellationToken);
            var dbName = (response.Message.Text ?? "TestDB").Trim();
            
            result = await _sqlServer.CreateDatabaseAsync(dbName, cancellationToken);
        }
        else if (lowerRequest.Contains("create table"))
        {
            // Use LLM to extract table structure
            var extractPrompt = $@"Extract the table name and column definitions from this request. 
Return in format: TABLE_NAME|COLUMN_DEFINITIONS
For example: Users|Id INT PRIMARY KEY, Name NVARCHAR(100), Email NVARCHAR(255)

Request: {userRequest}";

            var messages = new List<ChatMessage> { new(ChatRole.User, extractPrompt) };
            var response = await _chatClient.CompleteAsync(messages, options: null, cancellationToken);
            var parts = (response.Message.Text ?? "TestTable|Id INT").Split('|');
            var tableName = parts.Length > 0 ? parts[0].Trim() : "TestTable";
            var columns = parts.Length > 1 ? parts[1].Trim() : "Id INT PRIMARY KEY";
            
            result = await _sqlServer.CreateTableAsync(tableName, columns, cancellationToken);
        }
        else if (lowerRequest.Contains("select ") || lowerRequest.Contains("query"))
        {
            // Extract query using LLM
            var extractPrompt = $@"Extract or create the SQL SELECT query from this request. Return only the SQL query, nothing else.

Request: {userRequest}";

            var messages = new List<ChatMessage> { new(ChatRole.User, extractPrompt) };
            var response = await _chatClient.CompleteAsync(messages, options: null, cancellationToken);
            var query = response.Message.Text ?? "SELECT 1";
            
            result = await _sqlServer.QueryAsync(query, cancellationToken);
        }
        else
        {
            result = "I can help you with:\n- Listing databases\n- Listing tables\n- Creating databases\n- Creating tables\n- Running SELECT queries\n\nPlease rephrase your request.";
        }

        // Format the response using LLM
        var formatPrompt = $@"Present this database operation result to the user in a clear and friendly way.

User Request: {userRequest}

Operation Result:
{result}";
        
        var formatMessages = new List<ChatMessage> { new(ChatRole.User, formatPrompt) };
        var formatResponse = await _chatClient.CompleteAsync(formatMessages, cancellationToken);
        
        return formatResponse.Message.Text ?? result;
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

        // Use LLM to determine which agent(s) to use
        var routingPrompt = $@"Analyze this user request and determine which agent should handle it.
Respond with only one word: search, database, or both

- search: For internet searches, finding information online, answering general knowledge questions
- database: For SQL Server operations like creating databases, tables, or querying data
- both: If the task requires both searching for information AND database operations

User Request: {userRequest}";

        var routingMessages = new List<ChatMessage> { new(ChatRole.User, routingPrompt) };
        var routingResponse = await _chatClient.CompleteAsync(routingMessages, cancellationToken);
        var routeText = (routingResponse.Message.Text ?? "search").Trim().ToLowerInvariant();

        var results = new StringBuilder();

        if (routeText == "search")
        {
            var searchResult = await _searchAgent.ProcessQueryAsync(userRequest, cancellationToken);
            results.AppendLine(searchResult);
        }
        else if (routeText == "database")
        {
            var dbResult = await _databaseAgent.ProcessRequestAsync(userRequest, cancellationToken);
            results.AppendLine(dbResult);
        }
        else
        {
            // Execute both agents for "both" or unknown routes
            var searchTask = _searchAgent.ProcessQueryAsync(userRequest, cancellationToken);
            var dbTask = _databaseAgent.ProcessRequestAsync(userRequest, cancellationToken);
            await Task.WhenAll(searchTask, dbTask);
            
            results.AppendLine("Search Results:");
            results.AppendLine(searchTask.Result);
            results.AppendLine();
            results.AppendLine("Database Results:");
            results.AppendLine(dbTask.Result);
        }

        return results.ToString();
    }
}
