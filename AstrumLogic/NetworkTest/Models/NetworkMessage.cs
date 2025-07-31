using System.Text.Json.Serialization;

namespace NetworkTest.Models
{
    public class NetworkMessage
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
        
        [JsonPropertyName("data")]
        public object? Data { get; set; }
        
        [JsonPropertyName("error")]
        public string? Error { get; set; }
        
        [JsonPropertyName("success")]
        public bool Success { get; set; } = true;
        
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public NetworkMessage()
        {
        }

        public NetworkMessage(string type, object? data = null)
        {
            Type = type;
            Data = data;
        }

        public static NetworkMessage CreateSuccess(string type, object? data = null)
        {
            return new NetworkMessage(type, data) { Success = true };
        }

        public static NetworkMessage CreateError(string type, string error)
        {
            return new NetworkMessage(type) { Success = false, Error = error };
        }
    }

    public class LoginRequest
    {
        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }
    }



    public class CreateRoomRequest
    {
        [JsonPropertyName("roomName")]
        public string RoomName { get; set; } = string.Empty;
        
        [JsonPropertyName("maxPlayers")]
        public int MaxPlayers { get; set; } = 4;
    }

    public class JoinRoomRequest
    {
        [JsonPropertyName("roomId")]
        public string RoomId { get; set; } = string.Empty;
    }

    public class LeaveRoomRequest
    {
        [JsonPropertyName("roomId")]
        public string RoomId { get; set; } = string.Empty;
    }

    public class RoomInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string CreatorName { get; set; } = string.Empty;
        public int CurrentPlayers { get; set; }
        public int MaxPlayers { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string> PlayerNames { get; set; } = new();
    }

    public class UserInfo
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public DateTime LastLoginAt { get; set; }
        public string? CurrentRoomId { get; set; }
    }
} 