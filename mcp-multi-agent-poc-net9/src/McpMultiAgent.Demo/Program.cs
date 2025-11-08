using McpMultiAgent.AgentCore;
using McpMultiAgent.SearchServer;
using McpMultiAgent.SqlServer;

namespace McpMultiAgent.Demo;

/// <summary>
/// Demo application to test MCP multi-agent functionality
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== MCP Multi-Agent POC Demo ===\n");

        // Initialize MCP servers
        var searchServer = new DuckDuckGoSearchServer();
        var sqlServer = new SqlServerManagementServer(
            "Server=localhost;Database=TestDB;Integrated Security=true;TrustServerCertificate=true;");

        // Initialize chat client (mock for demo)
        var chatClient = new MockChatClient();

        // Initialize agents
        var searchAgent = new SearchAgent(searchServer, chatClient);
        var databaseAgent = new DatabaseAgent(sqlServer, chatClient);
        var coordinator = new MultiAgentCoordinator(searchAgent, databaseAgent, chatClient);

        Console.WriteLine("Demo 1: Search Agent - DuckDuckGo Internet Search");
        Console.WriteLine("================================================\n");
        
        try
        {
            var searchResult = await searchAgent.ProcessQueryAsync("What is .NET 9?");
            Console.WriteLine($"Result: {searchResult}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}\n");
        }

        Console.WriteLine("Demo 2: Database Agent - List Operations");
        Console.WriteLine("==========================================\n");
        
        try
        {
            // Note: This will fail without a real SQL Server connection
            var dbResult = await databaseAgent.ProcessRequestAsync("List databases");
            Console.WriteLine($"Result: {dbResult}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Note: SQL Server operations require a running SQL Server instance");
            Console.WriteLine($"Error: {ex.Message}\n");
        }

        Console.WriteLine("Demo 3: Multi-Agent Coordinator");
        Console.WriteLine("=================================\n");
        
        try
        {
            var coordResult = await coordinator.ProcessRequestAsync("Search for information about Microsoft Agent Framework");
            Console.WriteLine($"Result: {coordResult}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}\n");
        }

        Console.WriteLine("=== Demo Complete ===");
    }
}
