# Umbraco Forum MCP Server

A [Model Context Protocol (MCP)](https://modelcontextprotocol.io/) server that provides AI assistants with comprehensive access to the [Umbraco CMS Community Forum](https://forum.umbraco.com). This server enables AI assistant like  Cursor to search, monitor, and interact with the Umbraco forum directly when helping users with Umbraco-related questions.

## Overview

This MCP server wraps the Umbraco forum API and provides intelligent search capabilities, code extraction, version-aware filtering, and multi-source integration. It allows AI assistants to quickly find relevant solutions, extract code snippets, and help users navigate the Umbraco community forum more effectively.

## Features

- **Smart Search**: Version-aware search with intelligent ranking based on Umbraco version, tags, recency, and engagement
- **Author Information**: Shows author names in all search results for better context
- **Code Extraction**: Automatically extracts code snippets from forum posts with syntax highlighting
- **Multi-Source Search**: Search across Umbraco forum, official documentation, and GitHub repositories simultaneously
- **Topic Monitoring**: Monitor forum for specific topics or versions within configurable time windows
- **Post Drafting**: Generate well-formatted forum posts when solutions aren't found
- **Comprehensive Coverage**: Access to all Umbraco forum features including categories, latest topics, and detailed discussions




## Prerequisites

- .NET 10 SDK or later
- An MCP-compatible client (Cursor, etc.)

## Configuration



### For Cursor

Add this to your Cursor MCP settings (`.cursor/mcp.json` or Settings → MCP):

```json
{
  "servers": {
    "UForum-MCP": {
      "type": "stdio",
      "command": "dnx",
      "args": ["UForum-MCP@1.0.5", "--yes"]
    }
  }
}
```



## Available Tools

This MCP server provides comprehensive access to the Umbraco forum with the following tools:

### search_umbraco_forum

Searches the Umbraco community forum for relevant posts and topics.

**Parameters:**
- `query` (string, required): The search query (e.g., "404 custom API Umbraco 13")

**Returns:**
- Top 3 most relevant forum posts with author information
- Post titles, URLs, and previews
- View counts, reply counts, and like counts
- Direct links to forum discussions

**Example Usage:**
```
User: "I'm getting a 404 error on my custom API endpoint in Umbraco 13"
AI: [Calls search_umbraco_forum with query "404 custom API Umbraco 13"]
    Returns: Top 3 relevant forum posts with solutions
```

### smart_search_forum

Intelligent search that ranks results based on Umbraco version and context. This tool provides better results by understanding version compatibility and topic relevance.

**Parameters:**
- `query` (string, required): The search query
- `umbracoVersion` (string, optional): Your Umbraco version (e.g., "v13", "v14")
- `priorityTags` (string, optional): Comma-separated tags to prioritize (e.g., "API,Routing,Controllers")

**Returns:**
- Highly relevant results ranked by version match, tags, recency, and engagement
- Relevance scores for each result
- Version badges for matched versions
- Tag indicators for matched tags

**Example Usage:**
```
User: "How do I set up custom routing in Umbraco 13?"
AI: [Calls smart_search_forum with query "custom routing", umbracoVersion "v13", priorityTags "Routing,API"]
    Returns: Version-specific results with routing-related discussions
```

### get_forum_topic

Gets detailed information about a specific forum topic by ID, including all posts, code snippets, and accepted solutions.

**Parameters:**
- `topicId` (integer, required): The topic ID from the forum URL (e.g., from https://forum.umbraco.com/t/topic-name/12345, use 12345)

**Returns:**
- Full topic details with all posts
- Code snippets automatically extracted from discussions
- Accepted solutions highlighted
- Author information for each post
- Formatted code blocks ready to copy

**Example Usage:**
```
User: "Get details about topic 6018"
AI: [Calls get_forum_topic with topicId 6018]
    Returns: Complete discussion with extracted code snippets and solutions
```

### get_latest_topics

Gets the latest topics from the Umbraco forum to see recent community activity.

**Parameters:**
- `limit` (integer, optional): Number of topics to return (default 5, max 10)

**Returns:**
- List of latest forum topics
- View counts, reply counts, and like counts
- Last activity timestamps
- Direct links to discussions

### get_forum_categories

Gets all available categories in the Umbraco forum to discover forum sections and focus searches.

**Parameters:**
- None

**Returns:**
- List of all forum categories
- Category descriptions
- Topic counts per category
- Links to category pages

### search_all_sources

Searches across Umbraco forum, official documentation, and GitHub repositories simultaneously for comprehensive results.

**Parameters:**
- `query` (string, required): The search query
- `umbracoVersion` (string, optional): Your Umbraco version

**Returns:**
- Comprehensive results from all sources
- Forum discussions
- Official documentation links
- GitHub repository links and code examples

**Example Usage:**
```
User: "How do I set up Content Delivery API?"
AI: [Calls search_all_sources with query "Content Delivery API setup"]
    Returns: Forum posts, official docs, and GitHub examples all in one response
```

### monitor_forum_topics

Get recent forum posts about specific topics or versions within a configurable time window.

**Parameters:**
- `topic` (string, required): Topic to monitor (e.g., "API", "v14", "Deploy")
- `hoursBack` (integer, optional): Hours to look back (default 24)

**Returns:**
- Recent posts matching the topic
- Post timestamps
- Links to discussions
- Summary of activity

**Example Usage:**
```
User: "Show me new v14 posts from the last 48 hours"
AI: [Calls monitor_forum_topics with topic "v14", hoursBack 48]
    Returns: All v14-related posts from the last 48 hours
```

### draft_forum_post

Creates a well-formatted draft forum post when no solution is found. This helps users create effective forum posts to get help.

**Parameters:**
- `issue` (string, required): The issue/error description
- `umbracoVersion` (string, required): Your Umbraco version
- `errorMessage` (string, required): Error message if available
- `attemptedSolutions` (string, optional): What you've already tried
- `codeSnippet` (string, optional): Relevant code snippet
- `tags` (string, required): Category/tags (comma separated, e.g., "API,Routing")

**Returns:**
- Well-formatted markdown forum post
- Optimized title
- Suggested category and tags
- Complete post structure ready to copy and paste

**Example Usage:**
```
User: "I have an issue with API routing, help me draft a forum post"
AI: [Calls draft_forum_post with issue details]
    Returns: Complete forum post draft with all sections filled in
```

### should_create_post

Analyzes if an issue should be posted to the forum based on existing search results.

**Parameters:**
- `issue` (string, required): Your issue description
- `resultsFound` (integer, required): Number of relevant results found (0-10)
- `resultsHelpful` (string, required): Are existing results helpful? (yes/no/somewhat)

**Returns:**
- Recommendation on whether to create a post
- Confidence level
- Reasoning for the recommendation
- Suggested next steps

### optimize_post_title

Optimizes your forum post title for better visibility and responses.

**Parameters:**
- `draftTitle` (string, required): Your draft title
- `umbracoVersion` (string, required): Your Umbraco version

**Returns:**
- Optimized title
- Analysis of the original title
- Suggestions for improvement
- Title checklist


## How It Works

1. **MCP Server Setup**: Uses the [official Microsoft MCP C# SDK](https://github.com/modelcontextprotocol/csharp-sdk) to implement the MCP protocol
2. **Stdio Transport**: Communicates with AI clients via standard input/output using JSON-RPC
3. **Tool Discovery**: AI assistants automatically discover all available tools through MCP protocol
4. **API Integration**: Calls Umbraco forum API endpoints:
5. **Result Formatting**: Returns formatted results with rich metadata including author information, engagement metrics, and extracted code snippets
6. **Intelligent Ranking**: Applies multi-factor ranking algorithm considering version match, tag relevance, recency, and engagement scores

## Code Extraction

The server automatically extracts code snippets from forum posts:

- Detects code blocks in HTML content
- Extracts from both questions and answers
- Identifies accepted solutions
- Preserves language syntax (C#, JavaScript, JSON, Razor, etc.)
- Formats code blocks for easy copying
- Shows author of each snippet
- Highlights solution code separately from other examples

## Intelligent Ranking System

Results are ranked using a multi-factor algorithm:

- **Version Match**: +100 points for matching Umbraco version
- **Tag Match**: +50 points per matching priority tag
- **Query Words**: +20 points per matching query word in title
- **Recency**: Up to +30 points based on how recent the post is
- **Engagement**: Variable points based on views, likes, and replies

The system automatically detects Umbraco versions (v8 through v14) in topic titles and content.

## Example Interactions

### Example 1: Finding a Solution

```
You: "I'm getting a null reference exception when accessing document type properties in Umbraco 13"

AI: Let me search the Umbraco forum for similar issues...
    [Calls smart_search_forum with query "null reference exception document type properties", umbracoVersion "v13"]
    [Returns top 3 relevant posts with solutions]
    
    Found 3 relevant discussions. The most relevant is about ModelsBuilder configuration...
    [Provides links and code snippets from accepted solutions]
```

### Example 2: Learning a New Feature

```
You: "How do I set up the Content Delivery API in Umbraco 13?"

AI: I'll search across the forum, docs, and GitHub for Content Delivery API setup guides...
    [Calls search_all_sources with query "Content Delivery API setup", umbracoVersion "v13"]
    [Returns comprehensive results from all sources]
    
    Found setup guides in the forum, official documentation, and GitHub examples...
    [Provides links to all resources]
```

### Example 3: Monitoring Updates

```
You: "Show me new posts about v14 in the last week"

AI: Monitoring forum for v14 posts from the last 168 hours...
    [Calls monitor_forum_topics with topic "v14", hoursBack 168]
    [Returns recent v14-related discussions]
    
    Found 12 new posts about v14. Topics include upgrade guides, new features, and known issues...
    [Lists all recent posts with links]
```

### Example 4: Getting Detailed Help

```
You: "Get details about forum topic 6018"

AI: Fetching detailed information about topic 6018...
    [Calls get_forum_topic with topicId 6018]
    [Returns complete discussion with extracted code]
    
    Topic: "Management API create document with notification handler attached"
    [Shows full discussion, all replies, and extracted code snippets ready to copy]
```



## Tips for Best Results

When asking AI assistants to search the forum:

- Include specific version numbers (e.g., "Umbraco 13", "v14")
- Include error messages or error codes
- Use technical terms (e.g., "ModelsBuilder", "Content Delivery API", "Notification Handlers")
- Be specific about what you're trying to accomplish
- Use `smart_search_forum` for version-specific queries
- Use `search_all_sources` when you want comprehensive coverage
- Use `get_forum_topic` to get code snippets from discussions

## Troubleshooting

### Server not starting

- Ensure .NET 10 SDK is installed: `dotnet --version`
- Check for errors in stderr output
- Verify the project path in your MCP config
- Ensure all NuGet packages are restored: `dotnet restore`


### AI assistant not finding the tool

- Restart your AI client (Claude Desktop, Cursor, etc.)
- Verify the MCP config file syntax (valid JSON)
- Check the AI client's MCP logs for errors
- Ensure the MCP server is running and accessible
- Verify the command path in your MCP config is correct



## Resources

- [Model Context Protocol Documentation](https://modelcontextprotocol.io/)
- [Official MCP C# SDK](https://github.com/modelcontextprotocol/csharp-sdk)
- [Umbraco Forum](https://forum.umbraco.com/)
- [Umbraco Documentation](https://docs.umbraco.com/)
- [Umbraco GitHub](https://github.com/umbraco/Umbraco-CMS)




## Support

For issues, questions, or feature requests:
contact me bishal@usome.com



## Acknowledgments

- Built with the [Model Context Protocol C# SDK](https://github.com/modelcontextprotocol/csharp-sdk)
- Integrates with the [Umbraco Community Forum](https://forum.umbraco.com/)
- Inspired by the Umbraco community's need for better AI-assisted development tools
