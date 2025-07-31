using System.Text.Json.Serialization;
using System.Net.Sockets;

namespace AstrumServer.Models
{
    public class User
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime LastLoginAt { get; set; }
        public bool IsOnline { get; set; }
        public string? CurrentRoomId { get; set; }
        
        [JsonIgnore]
        public TcpClient? Client { get; set; }
        
        [JsonIgnore]
        public string? ClientId { get; set; }

        public User()
        {
            Id = Guid.NewGuid().ToString();
            DisplayName = $"Player_{Id[..8]}";
            CreatedAt = DateTime.Now;
            LastLoginAt = DateTime.Now;
        }

        public User(string displayName) : this()
        {
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                DisplayName = displayName;
            }
        }
    }
} 