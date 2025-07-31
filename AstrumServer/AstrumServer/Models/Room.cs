using System.Text.Json.Serialization;

namespace AstrumServer.Models
{
    public class Room
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string CreatorId { get; set; } = string.Empty;
        public int MaxPlayers { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public List<string> PlayerIds { get; set; } = new();
        
        [JsonIgnore]
        public List<User> Players => PlayerIds.Select(id => AstrumServer.Managers.UserManager.Instance.GetUser(id)).Where(u => u != null).ToList();

        public Room()
        {
            Id = Guid.NewGuid().ToString();
            CreatedAt = DateTime.Now;
            IsActive = true;
        }

        public Room(string name, string creatorId, int maxPlayers) : this()
        {
            Name = name;
            CreatorId = creatorId;
            MaxPlayers = maxPlayers;
            PlayerIds.Add(creatorId);
        }

        public bool CanJoin()
        {
            return IsActive && PlayerIds.Count < MaxPlayers;
        }

        public bool AddPlayer(string userId)
        {
            if (!CanJoin() || PlayerIds.Contains(userId))
                return false;
            
            PlayerIds.Add(userId);
            return true;
        }

        public bool RemovePlayer(string userId)
        {
            return PlayerIds.Remove(userId);
        }
    }
} 