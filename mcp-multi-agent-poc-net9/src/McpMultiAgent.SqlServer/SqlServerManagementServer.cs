using Microsoft.Data.SqlClient;
using System.Data;

namespace McpMultiAgent.SqlServer;

/// <summary>
/// MCP server providing MS SQL Server management functionality
/// </summary>
public class SqlServerManagementServer
{
    private readonly string _connectionString;
    private readonly string _masterConnectionString;

    public SqlServerManagementServer(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be empty", nameof(connectionString));

        _connectionString = connectionString;
        
        // Create master connection string for database creation
        var builder = new SqlConnectionStringBuilder(connectionString);
        var originalDb = builder.InitialCatalog;
        builder.InitialCatalog = "master";
        _masterConnectionString = builder.ConnectionString;
    }

    /// <summary>
    /// Create a new database
    /// </summary>
    public async Task<string> CreateDatabaseAsync(string databaseName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(databaseName))
            throw new ArgumentException("Database name cannot be empty", nameof(databaseName));

        // Validate database name to prevent SQL injection
        if (!IsValidIdentifier(databaseName))
            throw new ArgumentException("Invalid database name", nameof(databaseName));

        try
        {
            await using var connection = new SqlConnection(_masterConnectionString);
            await connection.OpenAsync(cancellationToken);

            // Check if database already exists
            var checkCmd = new SqlCommand(
                "SELECT COUNT(*) FROM sys.databases WHERE name = @dbName",
                connection);
            checkCmd.Parameters.AddWithValue("@dbName", databaseName);
            
            var exists = (int)await checkCmd.ExecuteScalarAsync(cancellationToken) > 0;
            if (exists)
                return $"Database '{databaseName}' already exists";

            // Create database
            var createCmd = new SqlCommand(
                $"CREATE DATABASE [{databaseName}]",
                connection);
            await createCmd.ExecuteNonQueryAsync(cancellationToken);

            return $"Database '{databaseName}' created successfully";
        }
        catch (Exception ex)
        {
            return $"Failed to create database: {ex.Message}";
        }
    }

    /// <summary>
    /// Create a new table in the database
    /// </summary>
    public async Task<string> CreateTableAsync(string tableName, string columnDefinitions, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("Table name cannot be empty", nameof(tableName));
        
        if (string.IsNullOrWhiteSpace(columnDefinitions))
            throw new ArgumentException("Column definitions cannot be empty", nameof(columnDefinitions));

        if (!IsValidIdentifier(tableName))
            throw new ArgumentException("Invalid table name", nameof(tableName));

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            // Check if table already exists
            var checkCmd = new SqlCommand(
                "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @tableName",
                connection);
            checkCmd.Parameters.AddWithValue("@tableName", tableName);
            
            var exists = (int)await checkCmd.ExecuteScalarAsync(cancellationToken) > 0;
            if (exists)
                return $"Table '{tableName}' already exists";

            // Create table
            var createCmd = new SqlCommand(
                $"CREATE TABLE [{tableName}] ({columnDefinitions})",
                connection);
            await createCmd.ExecuteNonQueryAsync(cancellationToken);

            return $"Table '{tableName}' created successfully";
        }
        catch (Exception ex)
        {
            return $"Failed to create table: {ex.Message}";
        }
    }

    /// <summary>
    /// Execute a SELECT query and return results
    /// </summary>
    public async Task<string> QueryAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Query cannot be empty", nameof(query));

        // Basic validation - only allow SELECT queries
        var trimmedQuery = query.TrimStart().ToUpperInvariant();
        if (!trimmedQuery.StartsWith("SELECT"))
            throw new ArgumentException("Only SELECT queries are allowed", nameof(query));

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(query, connection);
            command.CommandTimeout = 30; // 30 seconds timeout
            
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            
            var output = new System.Text.StringBuilder();
            
            // Get column names
            var columnCount = reader.FieldCount;
            var columnNames = new List<string>();
            for (int i = 0; i < columnCount; i++)
            {
                columnNames.Add(reader.GetName(i));
            }
            
            output.AppendLine(string.Join(" | ", columnNames));
            output.AppendLine(new string('-', columnNames.Sum(c => c.Length) + (columnCount - 1) * 3));

            // Read rows (limit to 100 rows for safety)
            int rowCount = 0;
            while (await reader.ReadAsync(cancellationToken) && rowCount < 100)
            {
                var values = new List<string>();
                for (int i = 0; i < columnCount; i++)
                {
                    var value = reader.IsDBNull(i) ? "NULL" : reader.GetValue(i).ToString();
                    values.Add(value ?? "NULL");
                }
                output.AppendLine(string.Join(" | ", values));
                rowCount++;
            }

            if (rowCount == 0)
                return "Query returned no results";

            output.AppendLine($"\nTotal rows: {rowCount}{(rowCount >= 100 ? " (limited to 100)" : "")}");
            
            return output.ToString();
        }
        catch (Exception ex)
        {
            return $"Query failed: {ex.Message}";
        }
    }

    /// <summary>
    /// List all databases on the server
    /// </summary>
    public async Task<string> ListDatabasesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new SqlConnection(_masterConnectionString);
            await connection.OpenAsync(cancellationToken);

            var command = new SqlCommand(
                "SELECT name FROM sys.databases WHERE database_id > 4 ORDER BY name",
                connection);
            
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            
            var databases = new List<string>();
            while (await reader.ReadAsync(cancellationToken))
            {
                databases.Add(reader.GetString(0));
            }

            if (databases.Count == 0)
                return "No user databases found";

            return $"User Databases:\n- {string.Join("\n- ", databases)}";
        }
        catch (Exception ex)
        {
            return $"Failed to list databases: {ex.Message}";
        }
    }

    /// <summary>
    /// List all tables in the current database
    /// </summary>
    public async Task<string> ListTablesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var command = new SqlCommand(
                "SELECT TABLE_SCHEMA, TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_SCHEMA, TABLE_NAME",
                connection);
            
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            
            var tables = new List<string>();
            while (await reader.ReadAsync(cancellationToken))
            {
                tables.Add($"{reader.GetString(0)}.{reader.GetString(1)}");
            }

            if (tables.Count == 0)
                return "No tables found in database";

            return $"Tables:\n- {string.Join("\n- ", tables)}";
        }
        catch (Exception ex)
        {
            return $"Failed to list tables: {ex.Message}";
        }
    }

    private static bool IsValidIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            return false;

        // Basic validation: alphanumeric and underscore only, starts with letter or underscore
        return identifier.All(c => char.IsLetterOrDigit(c) || c == '_') &&
               (char.IsLetter(identifier[0]) || identifier[0] == '_');
    }
}
