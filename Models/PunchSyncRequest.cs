using System;
using System.Text.Json.Serialization;

public class PunchSyncRequest
{
    [JsonPropertyName("personalId")]
    public string? PersonalId { get; set; }   // used by server sync

    [JsonPropertyName("photoData")]
    public string? ImageBase64 { get; set; }

    [JsonPropertyName("punchTime")]
    public DateTime PunchTime { get; set; } = DateTime.MinValue;
}
