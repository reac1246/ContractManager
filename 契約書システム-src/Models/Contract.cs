using System;
using System.Text.Json.Serialization;

namespace ContractManager.Models
{
    public class Contract
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }

        [JsonPropertyName("createdBy")]
        public string CreatedBy { get; set; } = string.Empty;

        [JsonPropertyName("contentHash")]
        public string ContentHash { get; set; } = string.Empty;

        // 重要契約フラグ
        [JsonPropertyName("isImportantContract")]
        public bool IsImportantContract { get; set; } = false;

        // 事前説明用HTML
        [JsonPropertyName("explanationHtml")]
        public string ExplanationHtml { get; set; } = string.Empty;
    }
}
