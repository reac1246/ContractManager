using System;
using System.Text.Json.Serialization;

namespace ContractManager.Models
{
    public class User
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("passwordHash")]
        public string PasswordHash { get; set; } = string.Empty;

        [JsonPropertyName("salt")]
        public string Salt { get; set; } = string.Empty;

        [JsonPropertyName("isAdmin")]
        public bool IsAdmin { get; set; } = false;

        [JsonPropertyName("nameEnglish")]
        public string NameEnglish { get; set; } = string.Empty;

        [JsonPropertyName("birthDate")]
        public DateTime? BirthDate { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
