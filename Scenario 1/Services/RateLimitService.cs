using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

public interface IRateLimitService
{
    Task<RateLimitInfo> GetRateLimitInfo(string recipient, string type);
    bool IsValidNotificationType(string type);
}

public class RateLimitService : IRateLimitService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly Dictionary<string, (int Limit, int WindowInSeconds)> _rateLimits;

    public RateLimitService(IConnectionMultiplexer redis, IConfiguration configuration)
    {
        _redis = redis;

        // Load rate limits from appsettings.json
        _rateLimits = configuration.GetSection("RateLimit").GetChildren()
            .ToDictionary(x => x.Key.ToLower(),
                          x => (int.Parse(x["Limit"]), int.Parse(x["WindowInSeconds"])));
    }

    public bool IsValidNotificationType(string type)
    {
        return _rateLimits.ContainsKey(type.ToLower());
    }

    public async Task<RateLimitInfo> GetRateLimitInfo(string recipient, string type)
    {
        var db = _redis.GetDatabase();
        var key = $"{recipient}:{type}";
        var rateLimit = _rateLimits[type.ToLower()];

        // Try to get the current request count
        int currentCount = (int)await db.StringGetAsync(key);
        TimeSpan? timeToReset = await db.KeyTimeToLiveAsync(key);

        if (timeToReset == null)
        {
            // If no TTL is set, it's the first request in the window, so set the TTL
            await db.StringSetAsync(key, 1, TimeSpan.FromSeconds(rateLimit.WindowInSeconds));
            currentCount = 1;
            timeToReset = TimeSpan.FromSeconds(rateLimit.WindowInSeconds);
        }
        else
        {
            // Otherwise, increment the count for the key
            await db.StringIncrementAsync(key);
            currentCount++;
        }

        int remaining = Math.Max(rateLimit.Limit - currentCount, 0);
        int resetTimeInSeconds = (int)(timeToReset?.TotalSeconds ?? rateLimit.WindowInSeconds);

        return new RateLimitInfo
        {
            Allowed = currentCount <= rateLimit.Limit,
            Limit = rateLimit.Limit,
            Remaining = remaining,
            Reset = resetTimeInSeconds
        };
    }
}

public class RateLimitInfo
{
    public bool Allowed { get; set; }
    public int Limit { get; set; }
    public int Remaining { get; set; }
    public int Reset { get; set; }
}
