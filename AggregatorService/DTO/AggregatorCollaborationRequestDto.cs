using System.Text.Json.Serialization;

namespace AggregatorService.DTO;

public class AggregatorCollaborationRequestDto
{
    [JsonPropertyName("id")]
    public string RequestId { get; set; } = null!;
    public string TeamId { get; set; } = null!;
    public string? TeamName { get; set; }
    public string FromUserId { get; set; } = null!;
    public string FromUsername { get; set; } = null!;
    [JsonPropertyName("fromAvatarUrl")]
    public string? FromUserAvatarUrl { get; set; }
    public string? ToUserId { get; set; }
    public string Role { get; set; } = null!;
    public string? Message { get; set; }
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}