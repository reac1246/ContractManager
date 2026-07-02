using System;
using System.Text.Json.Serialization;

namespace ContractManager.Models
{
    public enum ContractStatus
    {
        Pending,      // 未対応
        Agreed,       // 同意済み
        Rejected,     // 却下
        Disputed,     // 異議申し立て中
        Terminated    // 契約打ち切り
    }

    public class ContractAssignment
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("contractId")]
        public string ContractId { get; set; } = string.Empty;

        [JsonPropertyName("userId")]
        public string UserId { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public ContractStatus Status { get; set; } = ContractStatus.Pending;

        [JsonPropertyName("assignedAt")]
        public DateTime AssignedAt { get; set; } = DateTime.Now;

        [JsonPropertyName("respondedAt")]
        public DateTime? RespondedAt { get; set; }

        // 同意時の入力情報
        [JsonPropertyName("signatureName")]
        public string SignatureName { get; set; } = string.Empty;

        [JsonPropertyName("signatureBirthDate")]
        public DateTime? SignatureBirthDate { get; set; }

        [JsonPropertyName("signatureImage")]
        public string SignatureImage { get; set; } = string.Empty; // Base64 PNG

        // 異議申し立て内容
        [JsonPropertyName("disputeReason")]
        public string DisputeReason { get; set; } = string.Empty;

        [JsonPropertyName("terminationReason")]
        public string TerminationReason { get; set; } = string.Empty;

        [JsonPropertyName("agreedContentHash")]
        public string AgreedContentHash { get; set; } = string.Empty;

        // 代理同意フラグ（署名なし・消費者は後から拒否/解約可能）
        [JsonPropertyName("isProxyAgreement")]
        public bool IsProxyAgreement { get; set; } = false;

        // 音声録音データ（重要契約用）
        [JsonPropertyName("audioRecordingBase64")]
        public string AudioRecordingBase64 { get; set; } = string.Empty;

        [JsonPropertyName("audioHash")]
        public string AudioHash { get; set; } = string.Empty;

        // 本人確認書類（重要契約用・AES/ECC暗号化済み）
        [JsonPropertyName("idDocumentEncryptedBase64")]
        public string IdDocumentEncryptedBase64 { get; set; } = string.Empty;

        [JsonPropertyName("idDocumentEncryptedAesKey")]
        public string IdDocumentEncryptedAesKey { get; set; } = string.Empty;
    }
}
