using System.Text.Json.Serialization;

namespace UmbracoForumMcp.Models;

/// <summary>
/// Response from the Umbraco forum search API
/// </summary>
public class ForumSearchResponse
{
    [JsonPropertyName("posts")]
    public List<ForumPost> Posts { get; set; } = new();

    [JsonPropertyName("topics")]
    public List<ForumTopic> Topics { get; set; } = new();

    [JsonPropertyName("users")]
    public List<ForumUser> Users { get; set; } = new();

    [JsonPropertyName("categories")]
    public List<ForumCategory> Categories { get; set; } = new();

    [JsonPropertyName("tags")]
    public List<ForumTag> Tags { get; set; } = new();

    [JsonPropertyName("grouped_search_result")]
    public GroupedSearchResult? GroupedSearchResult { get; set; }
}

public class ForumPost
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("avatar_template")]
    public string AvatarTemplate { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("like_count")]
    public int LikeCount { get; set; }

    [JsonPropertyName("blurb")]
    public string Blurb { get; set; } = string.Empty;

    [JsonPropertyName("post_number")]
    public int PostNumber { get; set; }

    [JsonPropertyName("topic_id")]
    public int TopicId { get; set; }
}

public class ForumTopic
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("fancy_title")]
    public string FancyTitle { get; set; } = string.Empty;

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("posts_count")]
    public int PostsCount { get; set; }

    [JsonPropertyName("reply_count")]
    public int ReplyCount { get; set; }

    [JsonPropertyName("highest_post_number")]
    public int HighestPostNumber { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("last_posted_at")]
    public DateTime LastPostedAt { get; set; }

    [JsonPropertyName("views")]
    public int Views { get; set; }

    [JsonPropertyName("like_count")]
    public int LikeCount { get; set; }

    [JsonPropertyName("category_id")]
    public int CategoryId { get; set; }
}

public class ForumUser
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("avatar_template")]
    public string AvatarTemplate { get; set; } = string.Empty;
}

public class ForumCategory
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;
}

public class ForumTag
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("topic_count")]
    public int TopicCount { get; set; }
}

public class GroupedSearchResult
{
    [JsonPropertyName("term")]
    public string Term { get; set; } = string.Empty;

    [JsonPropertyName("type_filter")]
    public string? TypeFilter { get; set; }
}

// Topic Detail Models
public class ForumTopicDetail
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("posts_count")]
    public int PostsCount { get; set; }

    [JsonPropertyName("views")]
    public int Views { get; set; }

    [JsonPropertyName("like_count")]
    public int LikeCount { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("post_stream")]
    public PostStream? PostStream { get; set; }
}

public class PostStream
{
    [JsonPropertyName("posts")]
    public List<PostDetail> Posts { get; set; } = new();
}

public class PostDetail
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("cooked")]
    public string Cooked { get; set; } = string.Empty;

    [JsonPropertyName("post_number")]
    public int PostNumber { get; set; }

    [JsonPropertyName("accepted_answer")]
    public bool AcceptedAnswer { get; set; }
}

// Latest Topics Models
public class ForumLatestResponse
{
    [JsonPropertyName("topic_list")]
    public TopicList? TopicList { get; set; }
}

public class TopicList
{
    [JsonPropertyName("topics")]
    public List<ForumTopic> Topics { get; set; } = new();
}

// Categories Models
public class ForumCategoriesResponse
{
    [JsonPropertyName("category_list")]
    public CategoryList? CategoryList { get; set; }
}

public class CategoryList
{
    [JsonPropertyName("categories")]
    public List<ForumCategoryDetail> Categories { get; set; } = new();
}

public class ForumCategoryDetail
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("topic_count")]
    public int TopicCount { get; set; }

    [JsonPropertyName("position")]
    public int Position { get; set; }
}
