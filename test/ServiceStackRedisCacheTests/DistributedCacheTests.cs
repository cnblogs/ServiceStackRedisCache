using System.Formats.Tar;
using Microsoft.Extensions.Caching.Distributed;
using ServiceStack.Redis;
using Xunit.Priority;
using PriorityAttribute = Xunit.Priority.PriorityAttribute;

namespace ServiceStackRedisCacheTests;

[TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Assembly)]
[Collection(nameof(DistributedCacheCollection))]
public class DistributedCacheTests
{
    private const string _value = "Coding changes the world";
    private readonly IDistributedCache _cache;
    private readonly IRedisClientsManager _redisClientManager;

    public DistributedCacheTests(DistributedCacheFixture fixture)
    {
        _cache = fixture.DistributedCache;
        _redisClientManager = fixture.RedisClientManager;
    }

    [Fact]
    public async Task Cache_with_absolute_expiration()
    {
        var key = nameof(Cache_with_absolute_expiration) + "_" + Guid.NewGuid();
        var keyAsync = nameof(Cache_with_absolute_expiration) + "_async_" + Guid.NewGuid();

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(1)
        };

        _cache.SetString(key, _value, options);
        await _cache.SetStringAsync(keyAsync, _value, options);
        Assert.Equal(_value, _cache.GetString(key));
        Assert.Equal(_value, await _cache.GetStringAsync(keyAsync));

        await Task.Delay(1010);

        Assert.Null(_cache.GetString(key));
        Assert.Null(await _cache.GetStringAsync(keyAsync));
    }

    [Fact]
    public async Task Cache_with_absolute_expiration_relative_to_now()
    {
        var key = nameof(Cache_with_absolute_expiration_relative_to_now) + "_" + Guid.NewGuid();
        var keyAsync = nameof(Cache_with_absolute_expiration_relative_to_now) + "_async_" + Guid.NewGuid();

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(1)
        };

        _cache.SetString(key, _value, options);
        await _cache.SetStringAsync(keyAsync, _value, options);
        Assert.Equal(_value, _cache.GetString(key));
        Assert.Equal(_value, await _cache.GetStringAsync(keyAsync));

        await Task.Delay(1010);

        Assert.Null(_cache.GetString(key));
        Assert.Null(await _cache.GetStringAsync(keyAsync));
    }

    [Fact]
    public async Task Cache_with_sliding_expiration()
    {
        var key = nameof(Cache_with_sliding_expiration) + "_" + Guid.NewGuid();
        var keyAsync = nameof(Cache_with_sliding_expiration) + "_async_" + Guid.NewGuid();

        var options = new DistributedCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromSeconds(2)
        };

        _cache.SetString(key, _value, options);
        await _cache.SetStringAsync(keyAsync, _value, options);

        await Task.Delay(500);
        Assert.Equal(_value, _cache.GetString(key));
        Assert.Equal(_value, await _cache.GetStringAsync(keyAsync));
        Assert.True(GetTtl(key) > TimeSpan.FromMilliseconds(1900));
        Assert.True(await GetTtlAsync(keyAsync) > TimeSpan.FromMilliseconds(1900));

        await Task.Delay(1000);
        Assert.True(GetTtl(key) <= TimeSpan.FromMilliseconds(1000));
        Assert.True(await GetTtlAsync(keyAsync) <= TimeSpan.FromMilliseconds(1000));

        _cache.Refresh(key);
        await _cache.RefreshAsync(keyAsync);
        Assert.True(GetTtl(key) > TimeSpan.FromMilliseconds(1900));
        Assert.True(await GetTtlAsync(keyAsync) > TimeSpan.FromMilliseconds(1900));

        await Task.Delay(2010);
        Assert.Null(_cache.GetString(key));
        Assert.Null(await _cache.GetStringAsync(keyAsync));
    }

    private TimeSpan? GetTtl(string key)
    {
        using var client = _redisClientManager.GetClient();
        return client.GetTimeToLive(key);
    }

    private async Task<TimeSpan?> GetTtlAsync(string key)
    {
        await using var client = await _redisClientManager.GetClientAsync();
        return await client.GetTimeToLiveAsync(key);
    }
}