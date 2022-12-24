using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using ServiceStack.Redis;

namespace Microsoft.Extensions.Caching.ServiceStackRedis
{
    public class ServiceStackRedisCache : IDistributedCache
    {
        private readonly IRedisClientsManager _redisClientsManager;
        private readonly ServiceStackRedisCacheOptions _options;

        public ServiceStackRedisCache(IRedisClientsManager redisClientsManager)
        {
            RedisConfig.VerifyMasterConnections = false;
            _redisClientsManager = redisClientsManager;
        }

        public byte[] Get(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            using var client = _redisClientsManager.GetClient();
            if (!client.ContainsKey(key))
            {
                return null;
            }

            var values = client.GetValuesFromHash(key, nameof(CacheEntry.Value), nameof(CacheEntry.SlidingExpiration));

            if (TimeSpan.TryParse(values[1], out var sldExp))
            {
                Refresh(key, sldExp);
            }

            return Encoding.UTF8.GetBytes(values[0]);
        }

        public async Task<byte[]> GetAsync(string key, CancellationToken token = default)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            await using var client = await _redisClientsManager.GetClientAsync();
            if (!await client.ContainsKeyAsync(key))
            {
                return null;
            }

            var values = await client.GetValuesFromHashAsync(key, nameof(CacheEntry.Value), nameof(CacheEntry.SlidingExpiration));

            if (TimeSpan.TryParse(values[1], out var slbExp))
            {
                await RefreshAsync(key, slbExp);
            }

            return Encoding.UTF8.GetBytes(values[0]);
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            using var client = _redisClientsManager.GetClient();
            client.SetEntryInHash(key, nameof(CacheEntry.Value), Encoding.UTF8.GetString(value));
            SetExpiration(client, key, options);
        }

        public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default(CancellationToken))
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            await using var client = await _redisClientsManager.GetClientAsync();
            await client.SetEntryInHashAsync(key, nameof(CacheEntry.Value), Encoding.UTF8.GetString(value));
            await SetExpirationAsync(client, key, options);
        }

        public void Refresh(string key)
        {
            Refresh(key, null);
        }

        public void Refresh(string key, TimeSpan? sldExp)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            using var client = _redisClientsManager.GetClient();
            var ttl = client.GetTimeToLive(key);
            if (ttl.HasValue)
            {
                if (!sldExp.HasValue)
                {
                    var sldExpStr = client.GetValueFromHash(key, nameof(CacheEntry.SlidingExpiration));
                    if (TimeSpan.TryParse(sldExpStr, out var cachedSldExp))
                    {
                        sldExp = cachedSldExp;
                    }
                }

                if (sldExp.HasValue && ttl < sldExp)
                {
                    client.ExpireEntryIn(key, sldExp.Value);
                }
            }
        }

        public async Task RefreshAsync(string key, CancellationToken token)
        {
            await RefreshAsync(key, null);
        }

        public async Task RefreshAsync(string key, TimeSpan? sldExp)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            await using var client = await _redisClientsManager.GetClientAsync();
            var ttl = await client.GetTimeToLiveAsync(key);
            if (ttl.HasValue)
            {
                if (!sldExp.HasValue)
                {
                    var sldExpStr = await client.GetValueFromHashAsync(key, nameof(CacheEntry.SlidingExpiration));
                    if (TimeSpan.TryParse(sldExpStr, out var cachedSldExp))
                    {
                        sldExp = cachedSldExp;
                    }
                }

                if (sldExp.HasValue && ttl < sldExp)
                {
                    await client.ExpireEntryInAsync(key, sldExp.Value);
                }
            }
        }

        public void Remove(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            using var client = _redisClientsManager.GetClient();
            client.Remove(key);
        }

        public async Task RemoveAsync(string key, CancellationToken token = default)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            await using var client = await _redisClientsManager.GetClientAsync();
            await client.RemoveAsync(key);
        }

        private void SetExpiration(IRedisClient client, string key, DistributedCacheEntryOptions options)
        {
            if (options.SlidingExpiration.HasValue)
            {
                var sldExp = options.SlidingExpiration.Value;
                client.SetEntryInHash(key, nameof(CacheEntry.SlidingExpiration), sldExp.ToString());
                client.ExpireEntryIn(key, sldExp);
            }
            else if (options.AbsoluteExpirationRelativeToNow.HasValue)
            {
                client.ExpireEntryAt(key, DateTime.Now + options.AbsoluteExpirationRelativeToNow.Value);
            }
            else if (options.AbsoluteExpiration.HasValue)
            {
                client.ExpireEntryAt(key, options.AbsoluteExpiration.Value.DateTime);
            }
        }

        private async Task SetExpirationAsync(IRedisClientAsync client, string key, DistributedCacheEntryOptions options)
        {
            if (options.SlidingExpiration.HasValue)
            {
                var sldExp = options.SlidingExpiration.Value;
                await client.SetEntryInHashAsync(key, nameof(CacheEntry.SlidingExpiration), sldExp.ToString());
                await client.ExpireEntryInAsync(key, sldExp);
            }
            else if (options.AbsoluteExpirationRelativeToNow.HasValue)
            {
                await client.ExpireEntryAtAsync(key, DateTime.Now + options.AbsoluteExpirationRelativeToNow.Value);
            }
            else if (options.AbsoluteExpiration.HasValue)
            {
                await client.ExpireEntryAtAsync(key, options.AbsoluteExpiration.Value.DateTime);
            }
        }
    }
}