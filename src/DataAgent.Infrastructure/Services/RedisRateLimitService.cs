using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using DataAgent.Application.Interfaces;

namespace DataAgent.Infrastructure.Services;

public class RedisRateLimitService : IRateLimitService
{
    private readonly IConnectionMultiplexer? _redis;
    private readonly ILogger<RedisRateLimitService> _logger;

    public RedisRateLimitService(IConfiguration config, ILogger<RedisRateLimitService> logger)
    {
        _logger = logger;
        var connectionString = config["Redis:ConnectionString"] ?? Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING");
        
        try 
        {
            if (!string.IsNullOrEmpty(connectionString))
                _redis = ConnectionMultiplexer.Connect(connectionString);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Could not connect to Redis: {ex.Message}. Rate limiting will fallback to allowing all.");
        }
    }

    public async Task<bool> IsValidAsync(string key, int limit, int periodInMinutes)
    {
        if (_redis == null) return true; // Fallback if no redis

        try
        {
            var db = _redis.GetDatabase();
            var redisKey = $"ratelimit:{key}";
            
            var count = await db.StringIncrementAsync(redisKey);
            if (count == 1)
            {
                await db.KeyExpireAsync(redisKey, TimeSpan.FromMinutes(periodInMinutes));
            }

            return count <= limit;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Redis rate limit error: {ex.Message}");
            return true; // fail open
        }
    }
}
