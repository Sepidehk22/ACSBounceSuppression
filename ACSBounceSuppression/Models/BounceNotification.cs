using System.Text.Json.Serialization;

namespace ACSBounceSuppression.Models;

public class BounceNotification
{
    [JsonPropertyName("operationName")]
    public string OperationName { get; set; } = string.Empty;

    [JsonPropertyName("recipientAddress")]
    public string RecipientAddress { get; set; } = string.Empty;

    [JsonPropertyName("bounceReason")]
    public string BounceReason { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("messageId")]
    public string MessageId { get; set; } = string.Empty;
}
