using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ModelContextProtocol.Server;
using UmbracoForumMcp.Models;

namespace UmbracoForumMcp.Tools;

/// <summary>
/// Advanced context-aware search tools
/// </summary>
[McpServerToolType]
public class AdvancedSearchTools
{
    private const string ForumBaseUrl = "https://forum.umbraco.com";
    private const string ForumSearchUrl = "https://forum.umbraco.com/search.json";
    private static readonly string[] UmbracoVersions = { "v17", "v16", "v15", "v14", "v13", "v12", "v11", "v10", "v9", "v8" };

    /// <summary>
    /// Context-aware search that considers Umbraco version and user preferences
    /// </summary>
    [McpServerTool(Name = "smart_search_forum")]
    [Description("Intelligent search that ranks results based on your Umbraco version and context. Better than basic search for targeted results.")]
    public static async Task<string> SmartSearchForum(
        [Description("Your search query (e.g., 'API 404 error')")] 
        string query,
        IHttpClientFactory httpClientFactory,
        [Description("Your Umbraco version (e.g., 'v13', 'v14', 'v17') - helps prioritize relevant results")] 
        string? umbracoVersion = null,
        [Description("Comma-separated tags to prioritize (e.g., 'API,Routing,Controllers')")] 
        string? priorityTags = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Build enhanced query
            var enhancedQuery = query;
            if (!string.IsNullOrEmpty(umbracoVersion))
            {
                enhancedQuery = $"{query} {umbracoVersion}";
            }

            using var httpClient = httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            var encodedQuery = Uri.EscapeDataString(enhancedQuery);
            var requestUrl = $"{ForumSearchUrl}?q={encodedQuery}";

            var response = await httpClient.GetAsync(requestUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var searchResult = JsonSerializer.Deserialize<ForumSearchResponse>(jsonContent);

            if (searchResult == null)
            {
                return "Error: Failed to parse forum search results.";
            }

            // Parse priority tags
            var tags = priorityTags?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim().ToLower())
                .ToList() ?? new List<string>();

            // Rank results intelligently
            var rankedResults = RankResults(searchResult, umbracoVersion, tags, query);

            return FormatSmartResults(rankedResults, query, umbracoVersion);
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Multi-source search combining Forum, Docs, and GitHub
    /// </summary>
    [McpServerTool(Name = "search_all_sources")]
    [Description("Search across Umbraco forum, official docs, and GitHub repositories. Returns comprehensive results from all sources.")]
    public static async Task<string> SearchAllSources(
        [Description("Your search query")] 
        string query,
        IHttpClientFactory httpClientFactory,
        [Description("Your Umbraco version (optional)")] 
        string? umbracoVersion = null,
        CancellationToken cancellationToken = default)
    {
        var results = new AggregatedSearchResult();
        var tasks = new List<Task>();

        // Search forum
        tasks.Add(Task.Run(async () =>
        {
            try
            {
                var forumResults = await SearchForumInternal(query, umbracoVersion, httpClientFactory, cancellationToken);
                results.ForumResults = forumResults;
            }
            catch { }
        }, cancellationToken));

        // Search official docs
        tasks.Add(Task.Run(async () =>
        {
            try
            {
                var docsResults = await SearchUmbracoDocsInternal(query, umbracoVersion, httpClientFactory, cancellationToken);
                results.DocsResults = docsResults;
            }
            catch { }
        }, cancellationToken));

        // Search GitHub
        tasks.Add(Task.Run(async () =>
        {
            try
            {
                var githubResults = await SearchGitHubInternal(query, httpClientFactory, cancellationToken);
                results.GitHubResults = githubResults;
            }
            catch { }
        }, cancellationToken));

        await Task.WhenAll(tasks);

        return FormatAggregatedResults(results, query);
    }

    /// <summary>
    /// Monitor new forum posts for specific topics
    /// </summary>
    [McpServerTool(Name = "monitor_forum_topics")]
    [Description("Get recent forum posts about specific topics or versions. Use this to stay updated on new issues and solutions.")]
    public static async Task<string> MonitorForumTopics(
        [Description("Topic to monitor (e.g., 'API', 'v14', 'Deploy')")] 
        string topic,
        IHttpClientFactory httpClientFactory,
        [Description("Hours to look back (default 24)")] 
        int hoursBack = 24,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var httpClient = httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            var requestUrl = $"{ForumBaseUrl}/latest.json";
            var response = await httpClient.GetAsync(requestUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var latestData = JsonSerializer.Deserialize<ForumLatestResponse>(jsonContent);

            if (latestData?.TopicList?.Topics == null)
            {
                return "Error: Failed to fetch latest topics.";
            }

            var cutoffTime = DateTime.UtcNow.AddHours(-hoursBack);
            var relevantTopics = latestData.TopicList.Topics
                .Where(t => t.LastPostedAt >= cutoffTime)
                .Where(t => t.Title.Contains(topic, StringComparison.OrdinalIgnoreCase) ||
                           t.FancyTitle.Contains(topic, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(t => t.LastPostedAt)
                .ToList();

            return FormatMonitorResults(relevantTopics, topic, hoursBack);
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    private static List<RankedSearchResult> RankResults(
        ForumSearchResponse searchResult,
        string? targetVersion,
        List<string> priorityTags,
        string originalQuery)
    {
        var allResults = new List<RankedSearchResult>();
        var queryWords = originalQuery.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Process topics
        foreach (var topic in searchResult.Topics)
        {
            // Find the author from the first post (post_number = 1) of this topic
            var authorPost = searchResult.Posts?
                .FirstOrDefault(p => p.TopicId == topic.Id && p.PostNumber == 1);
            var authorName = authorPost != null && !string.IsNullOrEmpty(authorPost.Username) 
                ? authorPost.Username 
                : "Unknown";
            
            var result = new RankedSearchResult
            {
                Title = topic.FancyTitle,
                Url = $"{ForumBaseUrl}/t/{topic.Slug}/{topic.Id}",
                Preview = $"👤 Author: {authorName} · 👁 {topic.Views} views · 💬 {topic.ReplyCount} replies · ❤ {topic.LikeCount} likes",
                Date = topic.LastPostedAt,
                Score = topic.LikeCount + (topic.Views / 10) + (topic.ReplyCount * 2),
                Source = "Forum"
            };

            // Calculate relevance score
            var relevanceScore = 0;

            // Version matching (highest priority)
            var detectedVersion = DetectVersion(topic.Title + " " + topic.FancyTitle);
            result.DetectedVersion = detectedVersion;
            if (!string.IsNullOrEmpty(detectedVersion) && !string.IsNullOrEmpty(targetVersion))
            {
                if (detectedVersion.Equals(targetVersion, StringComparison.OrdinalIgnoreCase))
                {
                    relevanceScore += 100; // Huge boost for version match
                }
            }

            // Tag matching
            foreach (var tag in priorityTags)
            {
                if (topic.Title.Contains(tag, StringComparison.OrdinalIgnoreCase) ||
                    topic.FancyTitle.Contains(tag, StringComparison.OrdinalIgnoreCase))
                {
                    relevanceScore += 50;
                    result.MatchedTags.Add(tag);
                }
            }

            // Query word matching
            var titleLower = topic.FancyTitle.ToLower();
            foreach (var word in queryWords)
            {
                if (titleLower.Contains(word))
                {
                    relevanceScore += 20;
                }
            }

            // Recency bonus
            var daysSincePost = (DateTime.UtcNow - topic.LastPostedAt).TotalDays;
            if (daysSincePost < 30)
            {
                relevanceScore += (int)(30 - daysSincePost);
            }

            // Engagement score
            relevanceScore += result.Score / 10;

            result.RelevanceScore = relevanceScore;
            allResults.Add(result);
        }

        return allResults.OrderByDescending(r => r.RelevanceScore).ToList();
    }

    private static string? DetectVersion(string text)
    {
        foreach (var version in UmbracoVersions)
        {
            if (text.Contains(version, StringComparison.OrdinalIgnoreCase) ||
                text.Contains(version.Replace("v", "Umbraco "), StringComparison.OrdinalIgnoreCase))
            {
                return version;
            }
        }
        return null;
    }

    private static async Task<List<RankedSearchResult>> SearchForumInternal(
        string query,
        string? version,
        IHttpClientFactory httpClientFactory,
        CancellationToken cancellationToken)
    {
        using var httpClient = httpClientFactory.CreateClient();
        var enhancedQuery = !string.IsNullOrEmpty(version) ? $"{query} {version}" : query;
        var encodedQuery = Uri.EscapeDataString(enhancedQuery);
        var requestUrl = $"{ForumSearchUrl}?q={encodedQuery}";

        var response = await httpClient.GetAsync(requestUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var searchResult = JsonSerializer.Deserialize<ForumSearchResponse>(jsonContent);

        return searchResult?.Topics.Take(3).Select(t => new RankedSearchResult
        {
            Title = t.FancyTitle,
            Url = $"{ForumBaseUrl}/t/{t.Slug}/{t.Id}",
            Preview = $"{t.ReplyCount} replies · {t.Views} views",
            Date = t.LastPostedAt,
            Source = "Forum",
            DetectedVersion = DetectVersion(t.Title)
        }).ToList() ?? new List<RankedSearchResult>();
    }

    private static async Task<List<RankedSearchResult>> SearchUmbracoDocsInternal(
        string query,
        string? version,
        IHttpClientFactory httpClientFactory,
        CancellationToken cancellationToken)
    {
        // Search Umbraco official documentation
        var docsUrl = !string.IsNullOrEmpty(version)
            ? $"https://docs.umbraco.com/{version}/search?q={Uri.EscapeDataString(query)}"
            : $"https://docs.umbraco.com/search?q={Uri.EscapeDataString(query)}";

        var results = new List<RankedSearchResult>
        {
            new RankedSearchResult
            {
                Title = $"Official Docs: {query}",
                Url = docsUrl,
                Preview = "Search official Umbraco documentation for detailed guides",
                Date = DateTime.UtcNow,
                Source = "Docs"
            }
        };

        return results;
    }

    private static async Task<List<RankedSearchResult>> SearchGitHubInternal(
        string query,
        IHttpClientFactory httpClientFactory,
        CancellationToken cancellationToken)
    {
        var results = new List<RankedSearchResult>
        {
            new RankedSearchResult
            {
                Title = "Umbraco CMS Repository",
                Url = $"https://github.com/umbraco/Umbraco-CMS/search?q={Uri.EscapeDataString(query)}",
                Preview = "Search Umbraco CMS source code and issues",
                Date = DateTime.UtcNow,
                Source = "GitHub"
            },
            new RankedSearchResult
            {
                Title = "Umbraco Community Packages",
                Url = $"https://github.com/search?q=umbraco+{Uri.EscapeDataString(query)}",
                Preview = "Search community packages and examples",
                Date = DateTime.UtcNow,
                Source = "GitHub"
            }
        };

        return results;
    }

    private static string FormatSmartResults(List<RankedSearchResult> results, string query, string? version)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"🎯 **Smart Search Results for: \"{query}\"**");
        if (!string.IsNullOrEmpty(version))
        {
            sb.AppendLine($"🔖 Filtered for: **{version}**");
        }
        sb.AppendLine();

        var topResults = results.Take(5).ToList();

        if (!topResults.Any())
        {
            sb.AppendLine("❌ No results found.");
            return sb.ToString();
        }

        sb.AppendLine($"📊 Found {topResults.Count} highly relevant result(s):\n");

        for (int i = 0; i < topResults.Count; i++)
        {
            var result = topResults[i];
            var versionBadge = !string.IsNullOrEmpty(result.DetectedVersion) 
                ? $" `{result.DetectedVersion}`" 
                : "";
            var tagBadge = result.MatchedTags.Any() 
                ? $" 🏷️ {string.Join(", ", result.MatchedTags)}" 
                : "";

            sb.AppendLine($"**{i + 1}. {result.Title}**{versionBadge}{tagBadge}");
            sb.AppendLine($"   🔗 {result.Url}");
            sb.AppendLine($"   {result.Preview}");
            sb.AppendLine($"   📅 {result.Date:MMM dd, yyyy} · Relevance: {result.RelevanceScore}");
            sb.AppendLine();
        }

        sb.AppendLine("---");
        sb.AppendLine("💡 Results ranked by version match, tags, recency, and engagement.");

        return sb.ToString();
    }

    private static string FormatAggregatedResults(AggregatedSearchResult results, string query)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"🌐 **Multi-Source Search: \"{query}\"**");
        sb.AppendLine("Results from Forum, Docs, and GitHub\n");
        sb.AppendLine("═══════════════════════════════════════════════════════════\n");

        if (results.ForumResults.Any())
        {
            sb.AppendLine("📱 **FORUM DISCUSSIONS:**\n");
            foreach (var result in results.ForumResults)
            {
                sb.AppendLine($"• **{result.Title}**");
                sb.AppendLine($"  🔗 {result.Url}");
                sb.AppendLine($"  {result.Preview}\n");
            }
        }

        if (results.DocsResults.Any())
        {
            sb.AppendLine("📚 **OFFICIAL DOCUMENTATION:**\n");
            foreach (var result in results.DocsResults)
            {
                sb.AppendLine($"• **{result.Title}**");
                sb.AppendLine($"  🔗 {result.Url}");
                sb.AppendLine($"  {result.Preview}\n");
            }
        }

        if (results.GitHubResults.Any())
        {
            sb.AppendLine("💻 **GITHUB RESOURCES:**\n");
            foreach (var result in results.GitHubResults)
            {
                sb.AppendLine($"• **{result.Title}**");
                sb.AppendLine($"  🔗 {result.Url}");
                sb.AppendLine($"  {result.Preview}\n");
            }
        }

        sb.AppendLine("═══════════════════════════════════════════════════════════");
        sb.AppendLine("💡 TIP: Check forum for community solutions, docs for official guides, GitHub for code examples.");

        return sb.ToString();
    }

    private static string FormatMonitorResults(List<ForumTopic> topics, string topic, int hours)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"🔔 **Forum Monitor: \"{topic}\"**");
        sb.AppendLine($"📅 Last {hours} hours\n");

        if (!topics.Any())
        {
            sb.AppendLine($"✅ No new posts about \"{topic}\" in the last {hours} hours.");
            sb.AppendLine("\n💡 This is good - means no major new issues!");
            return sb.ToString();
        }

        sb.AppendLine($"⚠️ Found {topics.Count} new post(s):\n");

        foreach (var t in topics)
        {
            var hoursAgo = (int)(DateTime.UtcNow - t.LastPostedAt).TotalHours;
            sb.AppendLine($"**{t.FancyTitle}**");
            sb.AppendLine($"   🔗 {ForumBaseUrl}/t/{t.Slug}/{t.Id}");
            sb.AppendLine($"   🕐 {hoursAgo}h ago · 💬 {t.ReplyCount} replies · 👁 {t.Views} views");
            sb.AppendLine();
        }

        sb.AppendLine("---");
        sb.AppendLine("💡 Stay updated with these recent discussions!");

        return sb.ToString();
    }
}
