using System.ComponentModel;
using System.Text;
using System.Text.Json;
using ModelContextProtocol.Server;
using UmbracoForumMcp.Models;

namespace UmbracoForumMcp.Tools;

/// <summary>
/// MCP tools for searching the Umbraco forum
/// </summary>
[McpServerToolType]
public class UmbracoForumTools
{
    private const string ForumBaseUrl = "https://forum.umbraco.com";
    private const string ForumSearchUrl = "https://forum.umbraco.com/search.json";
    private const int MaxResultsToReturn = 3;

    /// <summary>
    /// Searches the Umbraco forum for relevant posts and topics
    /// </summary>
    /// <param name="query">The search query (e.g., "404 custom API Umbraco 13")</param>
    /// <param name="httpClientFactory">Injected HTTP client factory</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Top 3 most relevant forum posts with links</returns>
    [McpServerTool(Name = "search_umbraco_forum")]
    [Description("Searches the Umbraco forum for relevant posts and topics. Use this when users have questions about Umbraco CMS, errors, or need help with Umbraco development.")]
    public static async Task<string> SearchUmbracoForum(
        [Description("The search query (e.g., '404 custom API Umbraco 13', 'content picker not working', 'deploy errors')")] 
        string query,
        IHttpClientFactory httpClientFactory,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return "Error: Search query cannot be empty.";
        }

        try
        {
            using var httpClient = httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            // Build the search URL
            var encodedQuery = Uri.EscapeDataString(query);
            var requestUrl = $"{ForumSearchUrl}?q={encodedQuery}";

            // Make the API call
            var response = await httpClient.GetAsync(requestUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var searchResult = JsonSerializer.Deserialize<ForumSearchResponse>(jsonContent);

            if (searchResult == null)
            {
                return "Error: Failed to parse forum search results.";
            }

            // Format the results
            return FormatSearchResults(searchResult, query);
        }
        catch (HttpRequestException ex)
        {
            return $"Error: Failed to connect to Umbraco forum. {ex.Message}";
        }
        catch (TaskCanceledException)
        {
            return "Error: Search request timed out.";
        }
        catch (Exception ex)
        {
            return $"Error: An unexpected error occurred: {ex.Message}";
        }
    }

    private static string FormatSearchResults(ForumSearchResponse results, string query)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"🔍 Search results for: \"{query}\"\n");

        // Prioritize topics (full threads) over individual posts
        // Only show posts if no topics found
        var hasTopics = results.Topics != null && results.Topics.Any();
        
        if (hasTopics)
        {
            // Show topics only (complete threads)
            var topTopics = results.Topics
                .OrderByDescending(t => t.LikeCount + (t.Views / 10) + (t.ReplyCount * 2))
                .Take(MaxResultsToReturn)
                .ToList();

            if (!topTopics.Any())
            {
                sb.AppendLine("❌ No results found. Try different search terms.");
                sb.AppendLine("\n💡 Tips:");
                sb.AppendLine("   - Use specific version numbers (e.g., 'Umbraco 13')");
                sb.AppendLine("   - Include error messages or error codes");
                sb.AppendLine("   - Use technical terms (e.g., 'ModelsBuilder', 'Content Delivery API')");
                return sb.ToString();
            }

            sb.AppendLine($"📊 Found {topTopics.Count} relevant discussion(s):\n");

            for (int i = 0; i < topTopics.Count; i++)
            {
                var topic = topTopics[i];
                var url = $"https://forum.umbraco.com/t/{topic.Slug}/{topic.Id}";
                
                // Find the author from the first post (post_number = 1) of this topic
                var authorPost = results.Posts?
                    .FirstOrDefault(p => p.TopicId == topic.Id && p.PostNumber == 1);
                var authorName = authorPost != null && !string.IsNullOrEmpty(authorPost.Username) 
                    ? authorPost.Username 
                    : "Unknown";
                
                sb.AppendLine($"**{i + 1}. {topic.FancyTitle}**");
                sb.AppendLine($"   🔗 {url}");
                sb.AppendLine($"   👤 Author: {authorName}");
                sb.AppendLine($"   👁 {topic.Views} views · 💬 {topic.ReplyCount} replies · ❤ {topic.LikeCount} likes");
                sb.AppendLine($"   📅 Last activity: {topic.LastPostedAt:MMM dd, yyyy}");
                sb.AppendLine();
            }
        }
        else if (results.Posts != null && results.Posts.Any())
        {
            // Fallback: show individual posts if no topics found
            var topPosts = results.Posts
                .OrderByDescending(p => p.LikeCount * 3)
                .Take(MaxResultsToReturn)
                .ToList();

            sb.AppendLine($"📊 Found {topPosts.Count} relevant post(s):\n");

            for (int i = 0; i < topPosts.Count; i++)
            {
                var post = topPosts[i];
                var url = $"https://forum.umbraco.com/t/{post.TopicId}";
                var preview = post.Blurb.Length > 150 
                    ? post.Blurb.Substring(0, 150) + "..." 
                    : post.Blurb;
                
                sb.AppendLine($"**{i + 1}. {post.Name}**");
                sb.AppendLine($"   🔗 {url}");
                sb.AppendLine($"   By {post.Username} · ❤ {post.LikeCount} likes");
                sb.AppendLine($"   {preview}");
                sb.AppendLine($"   📅 {post.CreatedAt:MMM dd, yyyy}");
                sb.AppendLine();
            }
        }
        else
        {
            sb.AppendLine("❌ No results found. Try different search terms.");
            sb.AppendLine("\n💡 Tips:");
            sb.AppendLine("   - Use specific version numbers (e.g., 'Umbraco 13')");
            sb.AppendLine("   - Include error messages or error codes");
            sb.AppendLine("   - Use technical terms (e.g., 'ModelsBuilder', 'Content Delivery API')");
            return sb.ToString();
        }

        sb.AppendLine("---");
        sb.AppendLine("💡 Click the links above to view full discussions on the Umbraco forum.");

        return sb.ToString();
    }

    /// <summary>
    /// Gets detailed information about a specific forum topic
    /// </summary>
    /// <param name="topicId">The topic ID from the forum URL</param>
    /// <param name="httpClientFactory">Injected HTTP client factory</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detailed topic information including all posts</returns>
    [McpServerTool(Name = "get_forum_topic")]
    [Description("Gets detailed information about a specific Umbraco forum topic by ID. Use this to read full discussions and all replies.")]
    public static async Task<string> GetForumTopic(
        [Description("The topic ID (e.g., from URL https://forum.umbraco.com/t/topic-name/12345, use 12345)")] 
        int topicId,
        IHttpClientFactory httpClientFactory,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var httpClient = httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            var requestUrl = $"{ForumBaseUrl}/t/{topicId}.json";
            var response = await httpClient.GetAsync(requestUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var topic = JsonSerializer.Deserialize<ForumTopicDetail>(jsonContent);

            if (topic == null)
            {
                return "Error: Failed to parse topic details.";
            }

            return FormatTopicDetails(topic);
        }
        catch (HttpRequestException ex)
        {
            return $"Error: Failed to fetch topic {topicId}. {ex.Message}";
        }
        catch (TaskCanceledException)
        {
            return "Error: Request timed out.";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Gets the latest topics from the Umbraco forum
    /// </summary>
    /// <param name="httpClientFactory">Injected HTTP client factory</param>
    /// <param name="limit">Number of topics to return (default 5, max 10)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of latest forum topics</returns>
    [McpServerTool(Name = "get_latest_topics")]
    [Description("Gets the latest topics from the Umbraco forum. Use this to see recent discussions and trending questions.")]
    public static async Task<string> GetLatestTopics(
        IHttpClientFactory httpClientFactory,
        [Description("Number of topics to return (default 5, max 10)")] 
        int limit = 5,
        CancellationToken cancellationToken = default)
    {
        if (limit < 1) limit = 5;
        if (limit > 10) limit = 10;

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
                return "Error: Failed to parse latest topics.";
            }

            return FormatLatestTopics(latestData.TopicList.Topics.Take(limit).ToList());
        }
        catch (HttpRequestException ex)
        {
            return $"Error: Failed to fetch latest topics. {ex.Message}";
        }
        catch (TaskCanceledException)
        {
            return "Error: Request timed out.";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Gets categories available in the Umbraco forum
    /// </summary>
    /// <param name="httpClientFactory">Injected HTTP client factory</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of forum categories</returns>
    [McpServerTool(Name = "get_forum_categories")]
    [Description("Gets all available categories in the Umbraco forum. Use this to discover forum sections and focus searches.")]
    public static async Task<string> GetForumCategories(
        IHttpClientFactory httpClientFactory,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var httpClient = httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            var requestUrl = $"{ForumBaseUrl}/categories.json";
            var response = await httpClient.GetAsync(requestUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var categoriesData = JsonSerializer.Deserialize<ForumCategoriesResponse>(jsonContent);

            if (categoriesData?.CategoryList?.Categories == null)
            {
                return "Error: Failed to parse categories.";
            }

            return FormatCategories(categoriesData.CategoryList.Categories);
        }
        catch (HttpRequestException ex)
        {
            return $"Error: Failed to fetch categories. {ex.Message}";
        }
        catch (TaskCanceledException)
        {
            return "Error: Request timed out.";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    private static string FormatTopicDetails(ForumTopicDetail topic)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"📖 **{topic.Title}**\n");
        sb.AppendLine($"🔗 {ForumBaseUrl}/t/{topic.Slug}/{topic.Id}");
        sb.AppendLine($"👁 {topic.Views} views · 💬 {topic.PostsCount} posts · ❤ {topic.LikeCount} likes");
        sb.AppendLine($"📅 Created: {topic.CreatedAt:MMM dd, yyyy}\n");
        
        var allCodeSnippets = new List<(string username, string code, string language, bool isAcceptedSolution)>();
        
        if (topic.PostStream?.Posts != null && topic.PostStream.Posts.Any())
        {
            sb.AppendLine("**Original Post:**");
            var firstPost = topic.PostStream.Posts.First();
            
            // Extract code from original post
            var (cleanContent, codeSnippets) = ExtractCodeBlocks(firstPost.Cooked ?? "");
            
            var content = cleanContent.Length > 500 
                ? cleanContent.Substring(0, 500) + "..." 
                : cleanContent;
            sb.AppendLine($"By: {firstPost.Username}");
            sb.AppendLine($"{content}\n");

            // Track code from original post
            foreach (var snippet in codeSnippets)
            {
                allCodeSnippets.Add((firstPost.Username, snippet.code, snippet.language, false));
            }

            if (topic.PostStream.Posts.Count > 1)
            {
                sb.AppendLine($"**{topic.PostStream.Posts.Count - 1} Replies:**");
                
                // Check for accepted solutions first
                var acceptedSolution = topic.PostStream.Posts.Skip(1).FirstOrDefault(p => p.AcceptedAnswer == true);
                var postsToShow = acceptedSolution != null 
                    ? new[] { acceptedSolution }.Concat(topic.PostStream.Posts.Skip(1).Where(p => p.Id != acceptedSolution.Id).Take(2))
                    : topic.PostStream.Posts.Skip(1).Take(3);

                foreach (var post in postsToShow)
                {
                    var (replyCleanContent, replyCodeSnippets) = ExtractCodeBlocks(post.Cooked ?? "");
                    var replyContent = replyCleanContent.Length > 200 
                        ? replyCleanContent.Substring(0, 200) + "..." 
                        : replyCleanContent;
                    
                    var solutionBadge = post.AcceptedAnswer == true ? "✅ ACCEPTED SOLUTION - " : "";
                    sb.AppendLine($"\n- **{solutionBadge}{post.Username}** ({post.CreatedAt:MMM dd, yyyy}):");
                    sb.AppendLine($"  {replyContent}");

                    // Track code from replies
                    foreach (var snippet in replyCodeSnippets)
                    {
                        allCodeSnippets.Add((post.Username, snippet.code, snippet.language, post.AcceptedAnswer == true));
                    }
                }

                if (topic.PostStream.Posts.Count > 4)
                {
                    sb.AppendLine($"\n... and {topic.PostStream.Posts.Count - 4} more replies.");
                }
            }
        }

        // Display all code snippets found
        if (allCodeSnippets.Any())
        {
            sb.AppendLine("\n");
            sb.AppendLine("═══════════════════════════════════════════════════════════");
            sb.AppendLine("💻 **CODE SNIPPETS FOUND IN DISCUSSION:**");
            sb.AppendLine("═══════════════════════════════════════════════════════════\n");

            var solutionSnippets = allCodeSnippets.Where(s => s.isAcceptedSolution).ToList();
            var otherSnippets = allCodeSnippets.Where(s => !s.isAcceptedSolution).ToList();

            if (solutionSnippets.Any())
            {
                sb.AppendLine("✅ **SOLUTION CODE (from accepted answer):**\n");
                for (int i = 0; i < solutionSnippets.Count; i++)
                {
                    var snippet = solutionSnippets[i];
                    sb.AppendLine($"**Snippet {i + 1}** (by {snippet.username}):");
                    sb.AppendLine($"```{snippet.language}");
                    sb.AppendLine(snippet.code);
                    sb.AppendLine("```\n");
                }
            }

            if (otherSnippets.Any())
            {
                sb.AppendLine("📝 **Other code examples from discussion:**\n");
                for (int i = 0; i < otherSnippets.Count; i++)
                {
                    var snippet = otherSnippets[i];
                    sb.AppendLine($"**Snippet {i + 1}** (by {snippet.username}):");
                    sb.AppendLine($"```{snippet.language}");
                    sb.AppendLine(snippet.code);
                    sb.AppendLine("```\n");
                }
            }

            sb.AppendLine("═══════════════════════════════════════════════════════════");
            sb.AppendLine("💡 **TIP:** You can copy these code snippets directly to fix your issue!");
            sb.AppendLine("═══════════════════════════════════════════════════════════\n");
        }

        sb.AppendLine("---");
        sb.AppendLine($"💡 View full discussion at: {ForumBaseUrl}/t/{topic.Slug}/{topic.Id}");

        return sb.ToString();
    }

    private static (string cleanContent, List<(string code, string language)> codeSnippets) ExtractCodeBlocks(string html)
    {
        var codeSnippets = new List<(string code, string language)>();
        var cleanContent = html;

        // Extract code blocks with <pre><code> tags
        var codeBlockPattern = @"<pre><code class=""lang-(\w+)"">([^<]+)</code></pre>";
        var matches = System.Text.RegularExpressions.Regex.Matches(html, codeBlockPattern, System.Text.RegularExpressions.RegexOptions.Singleline);
        
        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var language = match.Groups[1].Value;
            var code = System.Net.WebUtility.HtmlDecode(match.Groups[2].Value).Trim();
            codeSnippets.Add((code, language));
            
            // Remove code block from clean content
            cleanContent = cleanContent.Replace(match.Value, "[CODE BLOCK EXTRACTED]");
        }

        // Also check for simple code tags without language
        var simpleCodePattern = @"<pre><code>([^<]+)</code></pre>";
        var simpleMatches = System.Text.RegularExpressions.Regex.Matches(cleanContent, simpleCodePattern, System.Text.RegularExpressions.RegexOptions.Singleline);
        
        foreach (System.Text.RegularExpressions.Match match in simpleMatches)
        {
            var code = System.Net.WebUtility.HtmlDecode(match.Groups[1].Value).Trim();
            if (code.Length > 20) // Only include substantial code blocks
            {
                codeSnippets.Add((code, "text"));
                cleanContent = cleanContent.Replace(match.Value, "[CODE BLOCK EXTRACTED]");
            }
        }

        // Strip remaining HTML tags from clean content
        cleanContent = System.Text.RegularExpressions.Regex.Replace(cleanContent, @"<[^>]+>", "");
        cleanContent = System.Net.WebUtility.HtmlDecode(cleanContent).Trim();

        return (cleanContent, codeSnippets);
    }

    private static string FormatLatestTopics(List<ForumTopic> topics)
    {
        var sb = new StringBuilder();
        sb.AppendLine("📰 **Latest Topics from Umbraco Forum**\n");

        for (int i = 0; i < topics.Count; i++)
        {
            var topic = topics[i];
            sb.AppendLine($"**{i + 1}. {topic.FancyTitle}**");
            sb.AppendLine($"   🔗 {ForumBaseUrl}/t/{topic.Slug}/{topic.Id}");
            sb.AppendLine($"   👁 {topic.Views} views · 💬 {topic.ReplyCount} replies · ❤ {topic.LikeCount} likes");
            sb.AppendLine($"   📅 {topic.LastPostedAt:MMM dd, yyyy HH:mm}");
            sb.AppendLine();
        }

        sb.AppendLine("---");
        sb.AppendLine("💡 These are the most recent discussions in the Umbraco community.");

        return sb.ToString();
    }

    private static string FormatCategories(List<ForumCategoryDetail> categories)
    {
        var sb = new StringBuilder();
        sb.AppendLine("📂 **Umbraco Forum Categories**\n");

        var visibleCategories = categories
            .Where(c => !string.IsNullOrEmpty(c.Name))
            .OrderBy(c => c.Position)
            .ToList();

        foreach (var category in visibleCategories)
        {
            sb.AppendLine($"**{category.Name}**");
            if (!string.IsNullOrEmpty(category.Description))
            {
                var desc = category.Description.Length > 100 
                    ? category.Description.Substring(0, 100) + "..." 
                    : category.Description;
                sb.AppendLine($"   {desc}");
            }
            sb.AppendLine($"   📊 {category.TopicCount} topics");
            sb.AppendLine($"   🔗 {ForumBaseUrl}/c/{category.Slug}/{category.Id}");
            sb.AppendLine();
        }

        sb.AppendLine("---");
        sb.AppendLine("💡 Use these categories to focus your forum searches.");

        return sb.ToString();
    }
}
