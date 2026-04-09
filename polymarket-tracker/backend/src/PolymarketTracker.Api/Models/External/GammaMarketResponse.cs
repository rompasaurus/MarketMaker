using System.Text.Json.Serialization;

namespace PolymarketTracker.Api.Models.External;

public class GammaMarketResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("condition_id")]
    public string ConditionId { get; set; } = "";

    [JsonPropertyName("question")]
    public string Question { get; set; } = "";

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("end_date_iso")]
    public string? EndDateIso { get; set; }

    [JsonPropertyName("active")]
    public bool Active { get; set; }

    [JsonPropertyName("volume_num")]
    public decimal VolumeNum { get; set; }

    [JsonPropertyName("liquidity_num")]
    public decimal LiquidityNum { get; set; }

    [JsonPropertyName("image")]
    public string? Image { get; set; }

    [JsonPropertyName("clob_token_ids")]
    public string? ClobTokenIds { get; set; }
}
