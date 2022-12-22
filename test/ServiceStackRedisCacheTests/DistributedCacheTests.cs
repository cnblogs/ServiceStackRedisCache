using Microsoft.Extensions.Caching.Distributed;
using Xunit.Priority;
using PriorityAttribute = Xunit.Priority.PriorityAttribute;

namespace ServiceStackRedisCacheTests;

[TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Assembly)]
[Collection(nameof(DistributedCacheCollection))]
public class DistributedCacheTests
{
    private readonly string _key = "distributed_cache";
    private readonly string _keyAsync = "distributed_cache_async";
    private const string _value = "Coding changes the world";
    private readonly IDistributedCache _distributedCache;

    public DistributedCacheTests(DistributedCacheFixture fixture)
    {
        _distributedCache = fixture.DistributedCache;
        _key += fixture.KeyPostfix;
        _keyAsync += fixture.KeyPostfix;
    }

    [Fact, Priority(1)]
    public async Task Sets_a_value_with_the_given_key()
    {
        _distributedCache.SetString(_key, _value);
        await _distributedCache.SetStringAsync(_keyAsync, _value);
    }

    [Fact, Priority(2)]
    public async Task Gets_a_value_with_the_given_key()
    {
        Assert.Equal(_value, _distributedCache.GetString(_key));
        Assert.Equal(_value, await _distributedCache.GetStringAsync(_keyAsync));
    }

    [Fact, Priority(3)]
    public async Task Refreshes_a_value_in_the_cache_based_on_its_key()
    {
        _distributedCache.Refresh(_key);
        await _distributedCache.RefreshAsync(_keyAsync);
    }

    [Fact, Priority(4)]
    public async Task Removes_the_value_with_the_given_key()
    {
        _distributedCache.Remove(_key);
        await _distributedCache.RemoveAsync(_keyAsync);
        Assert.Null(_distributedCache.Get(_key));
        Assert.Null(await _distributedCache.GetAsync(_keyAsync));
    }
}