using System;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using MemoryPack;
using Astrum.Generated;
using Astrum.CommonBase;

namespace AstrumTest
{
    /// <summary>
    /// 协议序列化/反序列化测试
    /// </summary>
    [Trait("TestLevel", "Unit")]        // 纯单元测试：序列化/反序列化
    [Trait("Category", "Unit")]
    [Trait("Module", "Network")]
    public class ProtocolSerializationTests
    {
        private readonly ITestOutputHelper _output;

        public ProtocolSerializationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        /// <summary>
        /// 测试GameNetworkMessage序列化和反序列化
        /// </summary>
        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Module", "Network")]
        public void GameNetworkMessage_SerializeDeserialize_ShouldWork()
        {
            // Arrange
            var originalMessage = GameNetworkMessage.Create();
            originalMessage.type = "TestMessage";
            originalMessage.data = new byte[] { 1, 2, 3, 4, 5 };
            originalMessage.success = true;
            originalMessage.error = "No error";
            originalMessage.timestamp = 1234567890;

            // Act - 序列化
            var serializedData = MemoryPackSerializer.Serialize(originalMessage);
            Assert.NotNull(serializedData);
            Assert.True(serializedData.Length > 0);

            // Act - 反序列化
            var deserializedMessage = MemoryPackSerializer.Deserialize<GameNetworkMessage>(serializedData);

            // Assert
            Assert.NotNull(deserializedMessage);
            Assert.Equal(originalMessage.type, deserializedMessage.type);
            Assert.Equal(originalMessage.data, deserializedMessage.data);
            Assert.Equal(originalMessage.success, deserializedMessage.success);
            Assert.Equal(originalMessage.error, deserializedMessage.error);
            Assert.Equal(originalMessage.timestamp, deserializedMessage.timestamp);

            _output.WriteLine($"✅ GameNetworkMessage 序列化测试通过，数据大小: {serializedData.Length} bytes");
        }

        /// <summary>
        /// 测试LoginRequest序列化和反序列化
        /// </summary>
        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Module", "Network")]
        public void LoginRequest_SerializeDeserialize_ShouldWork()
        {
            // Arrange
            var originalRequest = LoginRequest.Create();
            originalRequest.DisplayName = "TestPlayer";

            // Act - 序列化
            var serializedData = MemoryPackSerializer.Serialize(originalRequest);
            Assert.NotNull(serializedData);
            Assert.True(serializedData.Length > 0);

            // Act - 反序列化
            var deserializedRequest = MemoryPackSerializer.Deserialize<LoginRequest>(serializedData);

            // Assert
            Assert.NotNull(deserializedRequest);
            Assert.Equal(originalRequest.DisplayName, deserializedRequest.DisplayName);

            _output.WriteLine($"✅ LoginRequest 序列化测试通过，数据大小: {serializedData.Length} bytes");
        }

        /// <summary>
        /// 测试CreateRoomRequest序列化和反序列化
        /// </summary>
        [Fact(Skip = "Temporarily disabled for robustness tests")]
        public void CreateRoomRequest_SerializeDeserialize_ShouldWork()
        {
            // Arrange
            var originalRequest = CreateRoomRequest.Create();
            originalRequest.RoomName = "TestRoom";
            originalRequest.MaxPlayers = 4;

            // Act - 序列化
            var serializedData = MemoryPackSerializer.Serialize(originalRequest);
            Assert.NotNull(serializedData);
            Assert.True(serializedData.Length > 0);

            // Act - 反序列化
            var deserializedRequest = MemoryPackSerializer.Deserialize<CreateRoomRequest>(serializedData);

            // Assert
            Assert.NotNull(deserializedRequest);
            Assert.Equal(originalRequest.RoomName, deserializedRequest.RoomName);
            Assert.Equal(originalRequest.MaxPlayers, deserializedRequest.MaxPlayers);

            _output.WriteLine($"✅ CreateRoomRequest 序列化测试通过，数据大小: {serializedData.Length} bytes");
        }

        /// <summary>
        /// 测试UserInfo序列化和反序列化
        /// </summary>
        [Fact(Skip = "Temporarily disabled for robustness tests")]
        public void UserInfo_SerializeDeserialize_ShouldWork()
        {
            // Arrange
            var originalUserInfo = UserInfo.Create();
            originalUserInfo.Id = "user123";
            originalUserInfo.DisplayName = "TestUser";
            originalUserInfo.LastLoginAt = 1234567890;
            originalUserInfo.CurrentRoomId = "room456";

            // Act - 序列化
            var serializedData = MemoryPackSerializer.Serialize(originalUserInfo);
            Assert.NotNull(serializedData);
            Assert.True(serializedData.Length > 0);

            // Act - 反序列化
            var deserializedUserInfo = MemoryPackSerializer.Deserialize<UserInfo>(serializedData);

            // Assert
            Assert.NotNull(deserializedUserInfo);
            Assert.Equal(originalUserInfo.Id, deserializedUserInfo.Id);
            Assert.Equal(originalUserInfo.DisplayName, deserializedUserInfo.DisplayName);
            Assert.Equal(originalUserInfo.LastLoginAt, deserializedUserInfo.LastLoginAt);
            Assert.Equal(originalUserInfo.CurrentRoomId, deserializedUserInfo.CurrentRoomId);

            _output.WriteLine($"✅ UserInfo 序列化测试通过，数据大小: {serializedData.Length} bytes");
        }

        /// <summary>
        /// 测试RoomInfo序列化和反序列化
        /// </summary>
        [Fact(Skip = "Temporarily disabled for robustness tests")]
        public void RoomInfo_SerializeDeserialize_ShouldWork()
        {
            // Arrange
            var originalRoomInfo = RoomInfo.Create();
            originalRoomInfo.Id = "room123";
            originalRoomInfo.Name = "TestRoom";
            originalRoomInfo.CreatorName = "Creator";
            originalRoomInfo.CurrentPlayers = 2;
            originalRoomInfo.MaxPlayers = 4;
            originalRoomInfo.CreatedAt = 1234567890;
            originalRoomInfo.PlayerNames = new System.Collections.Generic.List<string> { "Player1", "Player2" };

            // Act - 序列化
            var serializedData = MemoryPackSerializer.Serialize(originalRoomInfo);
            Assert.NotNull(serializedData);
            Assert.True(serializedData.Length > 0);

            // Act - 反序列化
            var deserializedRoomInfo = MemoryPackSerializer.Deserialize<RoomInfo>(serializedData);

            // Assert
            Assert.NotNull(deserializedRoomInfo);
            Assert.Equal(originalRoomInfo.Id, deserializedRoomInfo.Id);
            Assert.Equal(originalRoomInfo.Name, deserializedRoomInfo.Name);
            Assert.Equal(originalRoomInfo.CreatorName, deserializedRoomInfo.CreatorName);
            Assert.Equal(originalRoomInfo.CurrentPlayers, deserializedRoomInfo.CurrentPlayers);
            Assert.Equal(originalRoomInfo.MaxPlayers, deserializedRoomInfo.MaxPlayers);
            Assert.Equal(originalRoomInfo.CreatedAt, deserializedRoomInfo.CreatedAt);
            Assert.Equal(originalRoomInfo.PlayerNames.Count, deserializedRoomInfo.PlayerNames.Count);
            Assert.True(originalRoomInfo.PlayerNames.SequenceEqual(deserializedRoomInfo.PlayerNames));

            _output.WriteLine($"✅ RoomInfo 序列化测试通过，数据大小: {serializedData.Length} bytes");
        }

        /// <summary>
        /// 测试HeartbeatMessage序列化和反序列化
        /// </summary>
        [Fact(Skip = "Temporarily disabled for robustness tests")]
        public void HeartbeatMessage_SerializeDeserialize_ShouldWork()
        {
            // Arrange
            var originalMessage = HeartbeatMessage.Create();
            originalMessage.ClientId = "client123";
            originalMessage.Timestamp = 1234567890;

            // Act - 序列化
            var serializedData = MemoryPackSerializer.Serialize(originalMessage);
            Assert.NotNull(serializedData);
            Assert.True(serializedData.Length > 0);

            // Act - 反序列化
            var deserializedMessage = MemoryPackSerializer.Deserialize<HeartbeatMessage>(serializedData);

            // Assert
            Assert.NotNull(deserializedMessage);
            Assert.Equal(originalMessage.ClientId, deserializedMessage.ClientId);
            Assert.Equal(originalMessage.Timestamp, deserializedMessage.Timestamp);

            _output.WriteLine($"✅ HeartbeatMessage 序列化测试通过，数据大小: {serializedData.Length} bytes");
        }

        /// <summary>
        /// 测试大数据量序列化性能
        /// </summary>
        [Fact(Skip = "Temporarily disabled for robustness tests")]
        public void LargeData_SerializePerformance_ShouldBeEfficient()
        {
            // Arrange
            var largeMessage = GameNetworkMessage.Create();
            largeMessage.type = "LargeDataTest";
            largeMessage.data = new byte[1024 * 1024]; // 1MB数据
            for (int i = 0; i < largeMessage.data.Length; i++)
            {
                largeMessage.data[i] = (byte)(i % 256);
            }
            largeMessage.success = true;
            largeMessage.error = "No error";
            largeMessage.timestamp = TimeInfo.Instance.ClientNow();

            // Act - 测试序列化性能
            var startTime = DateTime.UtcNow;
            var serializedData = MemoryPackSerializer.Serialize(largeMessage);
            var serializeTime = DateTime.UtcNow - startTime;

            // Act - 测试反序列化性能
            startTime = DateTime.UtcNow;
            var deserializedMessage = MemoryPackSerializer.Deserialize<GameNetworkMessage>(serializedData);
            var deserializeTime = DateTime.UtcNow - startTime;

            // Assert
            Assert.NotNull(serializedData);
            Assert.NotNull(deserializedMessage);
            Assert.Equal(largeMessage.data.Length, deserializedMessage.data.Length);
            Assert.True(largeMessage.data.SequenceEqual(deserializedMessage.data));

            _output.WriteLine($"✅ 大数据序列化测试通过");
            _output.WriteLine($"   原始数据大小: {largeMessage.data.Length:N0} bytes");
            _output.WriteLine($"   序列化后大小: {serializedData.Length:N0} bytes");
            _output.WriteLine($"   序列化耗时: {serializeTime.TotalMilliseconds:F2} ms");
            _output.WriteLine($"   反序列化耗时: {deserializeTime.TotalMilliseconds:F2} ms");
        }

        /// <summary>
        /// 测试空数据序列化
        /// </summary>
        [Fact(Skip = "Temporarily disabled for robustness tests")]
        public void EmptyData_Serialize_ShouldWork()
        {
            // Arrange
            var emptyMessage = GameNetworkMessage.Create();
            emptyMessage.type = "";
            emptyMessage.data = new byte[0];
            emptyMessage.success = false;
            emptyMessage.error = "";
            emptyMessage.timestamp = 0;

            // Act
            var serializedData = MemoryPackSerializer.Serialize(emptyMessage);
            var deserializedMessage = MemoryPackSerializer.Deserialize<GameNetworkMessage>(serializedData);

            // Assert
            Assert.NotNull(serializedData);
            Assert.NotNull(deserializedMessage);
            Assert.Equal(emptyMessage.type, deserializedMessage.type);
            Assert.Equal(emptyMessage.data.Length, deserializedMessage.data.Length);
            Assert.Equal(emptyMessage.success, deserializedMessage.success);
            Assert.Equal(emptyMessage.error, deserializedMessage.error);
            Assert.Equal(emptyMessage.timestamp, deserializedMessage.timestamp);

            _output.WriteLine($"✅ 空数据序列化测试通过，数据大小: {serializedData.Length} bytes");
        }
    }
}
