using System.Text.Json;

namespace McpMultiAgent.SearchServer;

/// <summary>
/// MCP server providing internet search functionality using DuckDuckGo's free API
/// </summary>
public class DuckDuckGoSearchServer
{
    private readonly HttpClient _httpClient;

    public DuckDuckGoSearchServer(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "MCP-Search-Agent/1.0");
    }

    /// <summary>
    /// Search DuckDuckGo for instant answers
    /// </summary>
    public async Task<string> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Query cannot be empty", nameof(query));

        // DuckDuckGo Instant Answer API
        var url = $"https://api.duckduckgo.com/?q={Uri.EscapeDataString(query)}&format=json&no_html=1&skip_disambig=1";

        try
        {
            var response = await _httpClient.GetStringAsync(url, cancellationToken);
            var result = JsonSerializer.Deserialize<DuckDuckGoResponse>(response);

            if (result == null)
                return "No results found";

            var output = new System.Text.StringBuilder();
            
            if (!string.IsNullOrEmpty(result.AbstractText))
            {
                output.AppendLine($"Abstract: {result.AbstractText}");
                if (!string.IsNullOrEmpty(result.AbstractSource))
                    output.AppendLine($"Source: {result.AbstractSource}");
                if (!string.IsNullOrEmpty(result.AbstractURL))
                    output.AppendLine($"URL: {result.AbstractURL}");
            }

            if (!string.IsNullOrEmpty(result.Answer))
            {
                output.AppendLine($"Answer: {result.Answer}");
            }

            if (!string.IsNullOrEmpty(result.Definition))
            {
                output.AppendLine($"Definition: {result.Definition}");
                if (!string.IsNullOrEmpty(result.DefinitionSource))
                    output.AppendLine($"Source: {result.DefinitionSource}");
            }

            if (result.RelatedTopics?.Any() == true)
            {
                output.AppendLine("\nRelated Topics:");
                foreach (var topic in result.RelatedTopics.Take(5))
                {
                    if (!string.IsNullOrEmpty(topic.Text))
                        output.AppendLine($"- {topic.Text}");
                    if (!string.IsNullOrEmpty(topic.FirstURL))
                        output.AppendLine($"  URL: {topic.FirstURL}");
                }
            }

            return output.Length > 0 ? output.ToString() : "No relevant results found";
        }
        catch (Exception ex)
        {
            return $"Search failed: {ex.Message}";
        }
    }

    private class DuckDuckGoResponse
    {
        public string? AbstractText { get; set; }
        public string? AbstractSource { get; set; }
        public string? AbstractURL { get; set; }
        public string? Answer { get; set; }
        public string? Definition { get; set; }
        public string? DefinitionSource { get; set; }
        public string? DefinitionURL { get; set; }
        public List<RelatedTopic>? RelatedTopics { get; set; }
    }

    private class RelatedTopic
    {
        public string? Text { get; set; }
        public string? FirstURL { get; set; }
    }
}
