using Xunit;
using Moq;
using StackExchange.Redis;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

public class RateLimitServiceTests
{
    private readonly RateLimitService _rateLimitService;
    private readonly Mock<IConnectionMultiplexer> _mockRedis;
    private readonly Mock<IDatabase> _mockDb;
    private readonly IConfiguration _configuration;

    public RateLimitServiceTests()
    {
        _mockRedis = new Mock<IConnectionMultiplexer>();
        _mockDb = new Mock<IDatabase>();
        _mockRedis.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_mockDb.Object);

        var inMemorySettings = new Dictionary<string, string> {
            {"RateLimit:Status:Limit", "2"},
            {"RateLimit:Status:WindowInSeconds", "60"}
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _rateLimitService = new RateLimitService(_mockRedis.Object, _configuration);
    }

    [Fact]
    public void IsValidNotificationType_ValidType_ReturnsTrue()
    {
        var result = _rateLimitService.IsValidNotificationType("Status");
        Assert.True(result);
    }

    [Fact]
    public void IsValidNotificationType_InvalidType_ReturnsFalse()
    {
        var result = _rateLimitService.IsValidNotificationType("InvalidType");
        Assert.False(result);
    }

    [Fact]
    public async Task GetRateLimitInfo_FirstRequest_Allowed()
    {
        // Arrange
        string recipient = "user@example.com";
        string type = "Status";
        _mockDb.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisValue)0);
        _mockDb.Setup(x => x.KeyTimeToLiveAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync((TimeSpan?)TimeSpan.FromSeconds(60));

        // Act
        var rateLimitInfo = await _rateLimitService.GetRateLimitInfo(recipient, type);

        // Assert
        Assert.True(rateLimitInfo.Allowed);
        Assert.Equal(2, rateLimitInfo.Limit);
        Assert.Equal(1, rateLimitInfo.Remaining);
    }

    [Fact]
    public async Task GetRateLimitInfo_LimitExceeded_NotAllowed()
    {
        // Arrange
        string recipient = "user@example.com";
        string type = "Status";
        _mockDb.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisValue)2);
        _mockDb.Setup(x => x.KeyTimeToLiveAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync((TimeSpan?)TimeSpan.FromSeconds(30));

        // Act
        var rateLimitInfo = await _rateLimitService.GetRateLimitInfo(recipient, type);

        // Assert
        Assert.False(rateLimitInfo.Allowed);
        Assert.Equal(2, rateLimitInfo.Limit);
        Assert.Equal(0, rateLimitInfo.Remaining);
    }
}
