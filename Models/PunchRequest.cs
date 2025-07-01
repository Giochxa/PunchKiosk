using System;
using System.Text.Json.Serialization;

public class PunchRequest
{
    [JsonPropertyName("employeeId")]
    public string? UniqueId { get; set; }  // Nullable to avoid validation errors

    [JsonPropertyName("photoData")]
    public string? ImageBase64 { get; set; }  // Nullable to avoid validation errors

    [JsonPropertyName("punchTime")]
    public DateTime PunchTime { get; set; } = DateTime.MinValue;
}
