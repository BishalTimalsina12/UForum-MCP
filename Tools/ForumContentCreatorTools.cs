using System.ComponentModel;
using System.Text;
using ModelContextProtocol.Server;

namespace UmbracoForumMcp.Tools;

/// <summary>
/// Tools for creating forum content when solutions aren't found
/// </summary>
[McpServerToolType]
public class ForumContentCreatorTools
{
    /// <summary>
    /// Analyzes an issue and drafts a forum post when no solution is found
    /// </summary>
    [McpServerTool(Name = "draft_forum_post")]
    [Description("Creates a well-formatted draft forum post when no solution is found. Use this after searching returns no results.")]
    public static string DraftForumPost(
        [Description("The issue/error you're experiencing")] 
        string issue,
        [Description("Your Umbraco version (e.g., 'v13', 'v14', 'v17')")] 
        string umbracoVersion,
        [Description("Error message if available")] 
        string? errorMessage,
        [Description("What you've already tried (optional)")] 
        string? attemptedSolutions,
        [Description("Relevant code snippet (optional)")] 
        string? codeSnippet,
        [Description("Category/tags (e.g., 'API,Routing' - comma separated)")] 
        string? tags)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("📝 **DRAFT FORUM POST**");
        sb.AppendLine("Ready to post at: https://forum.umbraco.com/");
        sb.AppendLine();
        sb.AppendLine("═══════════════════════════════════════════════════════════");
        sb.AppendLine();

        // Generate title
        var title = GenerateTitle(issue, umbracoVersion, errorMessage);
        sb.AppendLine($"**TITLE:**");
        sb.AppendLine($"{title}");
        sb.AppendLine();

        // Generate category suggestion
        var category = SuggestCategory(issue, tags);
        sb.AppendLine($"**SUGGESTED CATEGORY:**");
        sb.AppendLine($"{category}");
        sb.AppendLine();

        // Generate tags
        var suggestedTags = GenerateTags(issue, umbracoVersion, tags);
        sb.AppendLine($"**SUGGESTED TAGS:**");
        sb.AppendLine($"{string.Join(", ", suggestedTags)}");
        sb.AppendLine();

        sb.AppendLine("═══════════════════════════════════════════════════════════");
        sb.AppendLine();

        // Generate post body
        sb.AppendLine($"**POST CONTENT:**");
        sb.AppendLine();
        sb.AppendLine("```markdown");
        sb.AppendLine(GeneratePostBody(issue, umbracoVersion, errorMessage, attemptedSolutions, codeSnippet));
        sb.AppendLine("```");
        sb.AppendLine();

        sb.AppendLine("═══════════════════════════════════════════════════════════");
        sb.AppendLine();
        sb.AppendLine("💡 **TIPS FOR BETTER RESPONSES:**");
        sb.AppendLine("   ✅ Include all relevant details above");
        sb.AppendLine("   ✅ Add screenshots if applicable");
        sb.AppendLine("   ✅ Specify your environment (.NET version, hosting)");
        sb.AppendLine("   ✅ Check for typos before posting");
        sb.AppendLine();
        sb.AppendLine("🔗 **READY TO POST:**");
        sb.AppendLine("   1. Copy the content above");
        sb.AppendLine("   2. Go to https://forum.umbraco.com/");
        sb.AppendLine("   3. Click 'New Topic'");
        sb.AppendLine("   4. Paste and review");
        sb.AppendLine("   5. Add any screenshots");
        sb.AppendLine("   6. Post!");
        sb.AppendLine();
        sb.AppendLine("═══════════════════════════════════════════════════════════");
        
        return sb.ToString();
    }

    /// <summary>
    /// Suggests whether to create a forum post based on search results
    /// </summary>
    [McpServerTool(Name = "should_create_post")]
    [Description("Analyzes if your issue should be posted to the forum. Use this after unsuccessful searches.")]
    public static string ShouldCreatePost(
        [Description("Your issue description")] 
        string issue,
        [Description("Number of relevant results found (0-10)")] 
        int resultsFound,
        [Description("Are existing results helpful? (yes/no/somewhat)")] 
        string resultsHelpful)
    {
        var sb = new StringBuilder();
        sb.AppendLine("🤔 **SHOULD YOU CREATE A FORUM POST?**");
        sb.AppendLine();

        var shouldPost = false;
        var confidence = "";
        var reasoning = new List<string>();

        // Analysis logic
        if (resultsFound == 0)
        {
            shouldPost = true;
            confidence = "HIGH";
            reasoning.Add("✅ No existing discussions found - this is a new issue");
            reasoning.Add("✅ Community will benefit from your question");
            reasoning.Add("✅ You might be the first to encounter this");
        }
        else if (resultsFound > 0 && resultsHelpful.ToLower() == "no")
        {
            shouldPost = true;
            confidence = "HIGH";
            reasoning.Add("✅ Existing results don't solve your specific case");
            reasoning.Add("✅ Your variation might help others");
            reasoning.Add("✅ Include what you tried from other posts");
        }
        else if (resultsFound > 0 && resultsHelpful.ToLower() == "somewhat")
        {
            shouldPost = false;
            confidence = "MEDIUM";
            reasoning.Add("⚠️ Some relevant results exist");
            reasoning.Add("⚠️ Try implementing existing solutions first");
            reasoning.Add("💡 If those don't work, post with details on what you tried");
        }
        else
        {
            shouldPost = false;
            confidence = "LOW";
            reasoning.Add("❌ Helpful results found");
            reasoning.Add("💡 Try implementing existing solutions");
            reasoning.Add("💡 Comment on existing threads if you need clarification");
        }

        sb.AppendLine($"**RECOMMENDATION:** {(shouldPost ? "YES, CREATE A POST" : "TRY EXISTING SOLUTIONS FIRST")}");
        sb.AppendLine($"**CONFIDENCE:** {confidence}");
        sb.AppendLine();
        sb.AppendLine("**REASONING:**");
        foreach (var reason in reasoning)
        {
            sb.AppendLine($"   {reason}");
        }
        sb.AppendLine();

        if (shouldPost)
        {
            sb.AppendLine("🚀 **NEXT STEPS:**");
            sb.AppendLine("   1. Use `draft_forum_post` to create a well-formatted post");
            sb.AppendLine("   2. Include all relevant details (version, error, code)");
            sb.AppendLine("   3. Post to the forum");
            sb.AppendLine("   4. Monitor for responses");
        }
        else
        {
            sb.AppendLine("💡 **SUGGESTED ACTIONS:**");
            sb.AppendLine("   1. Try solutions from existing posts");
            sb.AppendLine("   2. Check official documentation");
            sb.AppendLine("   3. If still stuck after trying, then create a post");
            sb.AppendLine("   4. Reference what you tried in your post");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Generates a suggested post title based on checklist best practices
    /// </summary>
    [McpServerTool(Name = "optimize_post_title")]
    [Description("Optimizes your forum post title for better visibility and responses. Creates clear, searchable titles.")]
    public static string OptimizePostTitle(
        [Description("Your draft title")] 
        string draftTitle,
        [Description("Your Umbraco version")] 
        string umbracoVersion)
    {
        var sb = new StringBuilder();
        sb.AppendLine("📊 **POST TITLE OPTIMIZATION**");
        sb.AppendLine();
        sb.AppendLine($"**ORIGINAL TITLE:**");
        sb.AppendLine($"\"{draftTitle}\"");
        sb.AppendLine();

        // Analyze current title
        var issues = new List<string>();
        var improvements = new List<string>();

        if (!draftTitle.Contains(umbracoVersion, StringComparison.OrdinalIgnoreCase))
        {
            issues.Add("❌ Missing Umbraco version");
            improvements.Add("Add version for better targeting");
        }

        if (draftTitle.Length < 20)
        {
            issues.Add("⚠️ Title might be too short");
            improvements.Add("Add more context");
        }

        if (draftTitle.Length > 100)
        {
            issues.Add("⚠️ Title might be too long");
            improvements.Add("Make it more concise");
        }

        if (draftTitle.ToLower().StartsWith("help") || draftTitle.ToLower().StartsWith("please"))
        {
            issues.Add("⚠️ Starts with generic word");
            improvements.Add("Start with the actual issue");
        }

        var hasKeywords = draftTitle.Contains("404") || 
                         draftTitle.Contains("error") || 
                         draftTitle.Contains("API") ||
                         draftTitle.Contains("issue") ||
                         draftTitle.Contains("problem");

        if (!hasKeywords)
        {
            issues.Add("⚠️ Missing specific keywords");
            improvements.Add("Include error type or component");
        }

        // Generate optimized versions
        var optimizedTitle = OptimizeTitleString(draftTitle, umbracoVersion);

        sb.AppendLine("**ANALYSIS:**");
        if (issues.Any())
        {
            foreach (var issue in issues)
            {
                sb.AppendLine($"   {issue}");
            }
        }
        else
        {
            sb.AppendLine("   ✅ Title looks good!");
        }
        sb.AppendLine();

        if (improvements.Any())
        {
            sb.AppendLine("**SUGGESTED IMPROVEMENTS:**");
            foreach (var improvement in improvements)
            {
                sb.AppendLine($"   💡 {improvement}");
            }
            sb.AppendLine();
        }

        sb.AppendLine("**OPTIMIZED TITLE:**");
        sb.AppendLine($"\"{optimizedTitle}\"");
        sb.AppendLine();

        sb.AppendLine("**TITLE CHECKLIST:**");
        sb.AppendLine($"   {(optimizedTitle.Length >= 20 && optimizedTitle.Length <= 100 ? "✅" : "❌")} Length: {optimizedTitle.Length} chars (ideal: 40-80)");
        sb.AppendLine($"   {(optimizedTitle.Contains(umbracoVersion, StringComparison.OrdinalIgnoreCase) ? "✅" : "❌")} Includes version");
        sb.AppendLine($"   {(hasKeywords ? "✅" : "❌")} Contains specific keywords");
        sb.AppendLine($"   {(!optimizedTitle.ToLower().StartsWith("help") ? "✅" : "❌")} Starts with issue description");

        return sb.ToString();
    }

    private static string GenerateTitle(string issue, string version, string? errorMessage)
    {
        var title = new StringBuilder();

        // Extract key error code if present
        var errorCode = ExtractErrorCode(errorMessage);
        if (!string.IsNullOrEmpty(errorCode))
        {
            title.Append($"{errorCode} - ");
        }

        // Add concise issue description
        var shortIssue = issue.Length > 60 ? issue.Substring(0, 57) + "..." : issue;
        title.Append(shortIssue);

        // Add version
        if (!title.ToString().Contains(version, StringComparison.OrdinalIgnoreCase))
        {
            title.Append($" ({version})");
        }

        return title.ToString();
    }

    private static string SuggestCategory(string issue, string? tags)
    {
        var issueLower = issue.ToLower();
        var tagsLower = tags?.ToLower() ?? "";

        if (issueLower.Contains("api") || tagsLower.Contains("api"))
            return "💻 Developing Websites (API/Backend Development)";
        
        if (issueLower.Contains("deploy") || tagsLower.Contains("deploy"))
            return "🚀 Umbraco Cloud / Deploy";
        
        if (issueLower.Contains("forms") || tagsLower.Contains("forms"))
            return "📝 Umbraco Forms";
        
        if (issueLower.Contains("commerce") || tagsLower.Contains("commerce"))
            return "🛒 Umbraco Commerce";
        
        if (issueLower.Contains("upgrade") || issueLower.Contains("migration"))
            return "⬆️ Upgrading Umbraco";
        
        return "💻 Developing Websites (General)";
    }

    private static List<string> GenerateTags(string issue, string version, string? userTags)
    {
        var tags = new HashSet<string>();
        
        // Always add version
        tags.Add(version.Replace("v", "umbraco-"));

        // Add user-provided tags
        if (!string.IsNullOrEmpty(userTags))
        {
            foreach (var tag in userTags.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                tags.Add(tag.Trim().ToLower());
            }
        }

        // Auto-detect common tags
        var issueLower = issue.ToLower();
        if (issueLower.Contains("api")) tags.Add("api");
        if (issueLower.Contains("404")) tags.Add("routing");
        if (issueLower.Contains("500")) tags.Add("server-error");
        if (issueLower.Contains("model")) tags.Add("modelsbuilder");
        if (issueLower.Contains("deploy")) tags.Add("deployment");
        if (issueLower.Contains("content")) tags.Add("content");
        if (issueLower.Contains("media")) tags.Add("media");

        return tags.Take(5).ToList(); // Forum usually limits to 5 tags
    }

    private static string GeneratePostBody(
        string issue,
        string version,
        string? errorMessage,
        string? attemptedSolutions,
        string? codeSnippet)
    {
        var sb = new StringBuilder();

        sb.AppendLine("## Issue Description");
        sb.AppendLine();
        sb.AppendLine(issue);
        sb.AppendLine();

        sb.AppendLine("## Environment");
        sb.AppendLine();
        sb.AppendLine($"- **Umbraco Version:** {version}");
        sb.AppendLine($"- **.NET Version:** [Please specify, e.g., .NET 8]");
        sb.AppendLine($"- **Hosting:** [e.g., Umbraco Cloud, Azure, IIS, Self-hosted]");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(errorMessage))
        {
            sb.AppendLine("## Error Message");
            sb.AppendLine();
            sb.AppendLine("```");
            sb.AppendLine(errorMessage);
            sb.AppendLine("```");
            sb.AppendLine();
        }

        if (!string.IsNullOrEmpty(codeSnippet))
        {
            sb.AppendLine("## Relevant Code");
            sb.AppendLine();
            sb.AppendLine("```csharp");
            sb.AppendLine(codeSnippet);
            sb.AppendLine("```");
            sb.AppendLine();
        }

        if (!string.IsNullOrEmpty(attemptedSolutions))
        {
            sb.AppendLine("## What I've Tried");
            sb.AppendLine();
            sb.AppendLine(attemptedSolutions);
            sb.AppendLine();
        }

        sb.AppendLine("## Expected Behavior");
        sb.AppendLine();
        sb.AppendLine("[Describe what you expected to happen]");
        sb.AppendLine();

        sb.AppendLine("## Actual Behavior");
        sb.AppendLine();
        sb.AppendLine("[Describe what actually happens]");
        sb.AppendLine();

        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("Any help would be greatly appreciated! Thank you! 🙏");

        return sb.ToString();
    }

    private static string? ExtractErrorCode(string? errorMessage)
    {
        if (string.IsNullOrEmpty(errorMessage))
            return null;

        if (errorMessage.Contains("404")) return "404";
        if (errorMessage.Contains("500")) return "500";
        if (errorMessage.Contains("401")) return "401";
        if (errorMessage.Contains("403")) return "403";

        return null;
    }

    private static string OptimizeTitleString(string title, string version)
    {
        var optimized = title.Trim();

        // Remove generic starts
        if (optimized.ToLower().StartsWith("help:"))
            optimized = optimized.Substring(5).Trim();
        if (optimized.ToLower().StartsWith("help"))
            optimized = optimized.Substring(4).Trim();
        if (optimized.ToLower().StartsWith("please"))
            optimized = optimized.Substring(6).Trim();

        // Add version if missing
        if (!optimized.Contains(version, StringComparison.OrdinalIgnoreCase))
        {
            optimized = $"{optimized} - {version}";
        }

        // Capitalize first letter
        if (optimized.Length > 0)
        {
            optimized = char.ToUpper(optimized[0]) + optimized.Substring(1);
        }

        // Trim to reasonable length
        if (optimized.Length > 100)
        {
            optimized = optimized.Substring(0, 97) + "...";
        }

        return optimized;
    }
}
