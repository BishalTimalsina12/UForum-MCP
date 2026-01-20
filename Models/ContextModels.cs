using System.Text.Json.Serialization;

namespace UmbracoForumMcp.Models;

/// <summary>
/// User context for personalized search results
/// </summary>
public class UserContext
{
    [JsonPropertyName("umbraco_version")]
    public string? UmbracoVersion { get; set; }

    [JsonPropertyName("past_queries")]
    public List<string> PastQueries { get; set; } = new();

    [JsonPropertyName("preferred_tags")]
    public List<string> PreferredTags { get; set; } = new();

    [JsonPropertyName("common_issues")]
    public List<string> CommonIssues { get; set; } = new();
}

/// <summary>
/// Enhanced search result with relevance scoring
/// </summary>
public class RankedSearchResult
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Preview { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int Score { get; set; }
    public int RelevanceScore { get; set; }
    public List<string> MatchedTags { get; set; } = new();
    public string? DetectedVersion { get; set; }
    public string Source { get; set; } = "Forum"; // Forum, Docs, GitHub
}

/// <summary>
/// Multi-source aggregated result
/// </summary>
public class AggregatedSearchResult
{
    public List<RankedSearchResult> ForumResults { get; set; } = new();
    public List<RankedSearchResult> DocsResults { get; set; } = new();
    public List<RankedSearchResult> GitHubResults { get; set; } = new();
    public string Summary { get; set; } = string.Empty;
}
