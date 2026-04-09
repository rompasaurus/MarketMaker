using System.Text.Json.Serialization;

namespace PolymarketTracker.Api.Models.External;

public class PolymarketMidpointResponse
{
    [JsonPropertyName("mid")]
    public string Mid { get; set; } = "0";
}

public class PolymarketPriceHistoryPoint
{
    [JsonPropertyName("t")]
    public long Timestamp { get; set; }

    [JsonPropertyName("p")]
    public decimal Price { get; set; }
}

public class PolymarketTradeMessage
{
    [JsonPropertyName("asset_id")]
    public string AssetId { get; set; } = "";

    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("side")]
    public string Side { get; set; } = "";

    [JsonPropertyName("price")]
    public string Price { get; set; } = "0";

    [JsonPropertyName("size")]
    public string Size { get; set; } = "0";

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = "";

    [JsonPropertyName("outcome")]
    public string Outcome { get; set; } = "";
}

public class PolymarketWebSocketMessage
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("asset_id")]
    public string AssetId { get; set; } = "";

    [JsonPropertyName("trades")]
    public List<PolymarketTradeMessage>? Trades { get; set; }
}
