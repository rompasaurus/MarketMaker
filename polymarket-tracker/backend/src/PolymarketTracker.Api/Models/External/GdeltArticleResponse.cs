using System.Text.Json.Serialization;

namespace PolymarketTracker.Api.Models.External;

public class GdeltDocResponse
{
    [JsonPropertyName("articles")]
    public List<GdeltArticle>? Articles { get; set; }
}

public class GdeltArticle
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

    [JsonPropertyName("domain")]
    public string? Domain { get; set; }

    [JsonPropertyName("sourcecountry")]
    public string? SourceCountry { get; set; }

    [JsonPropertyName("tone")]
    public string? Tone { get; set; }

    [JsonPropertyName("socialimage")]
    public string? SocialImage { get; set; }

    [JsonPropertyName("seendate")]
    public string? SeenDate { get; set; }
}
