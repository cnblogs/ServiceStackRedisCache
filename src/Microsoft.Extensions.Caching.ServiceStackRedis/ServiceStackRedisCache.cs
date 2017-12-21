using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using ServiceStack.Redis;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Caching.ServiceStackRedis
{
    public class ServiceStackRedisCache : IDistributedCache
    {
        private readonly IRedisClientsManager _redisManager;
        private readonly ServiceStackRedisCacheOptions _options;

        public ServiceStackRedisCache(IOptions<ServiceStackRedisCacheOptions> optionsAccessor)
        {
            if (optionsAccessor == null)
            {
                throw new ArgumentNullException(nameof(optionsAccessor));
            }

            _options = optionsAccessor.Value;

            var host = $"{_options.Password}@{_options.Host}:{_options.Port}";
            RedisConfig.VerifyMasterConnections = false;
            _redisManager = new RedisManagerPool(host);
        }

        public byte[] Get(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            using (var client = _redisManager.GetClient() as IRedisNativeClient)
            {
                if (client.Exists(key) == 1)
                {
                    return client.Get(key);
                }
            }
            return null;
        }

        public Task<byte[]> GetAsync(string key, CancellationToken token = default(CancellationToken))
        {
            return Task.FromResult(Get(key));
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

            using (var client = _redisManager.GetClient() as IRedisNativeClient)
            {
                var expireInSeconds = GetExpireInSeconds(options);
                if (expireInSeconds > 0)
                {
                    client.SetEx(key, expireInSeconds, value);
                    client.SetEx(GetExpirationKey(key), expireInSeconds, Encoding.UTF8.GetBytes(expireInSeconds.ToString()));
                }
                else
                {
                    client.Set(key, value);
                }
            }
        }

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default(CancellationToken))
        {
            return Task.Run(() => Set(key, value, options));
        }

        public void Refresh(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            using (var client = _redisManager.GetClient() as IRedisNativeClient)
            {
                if (client.Exists(key) == 1)
                {
                    var value = client.Get(key);
                    if (value != null)
                    {
                        var expirationValue = client.Get(GetExpirationKey(key));
                        if (expirationValue != null)
                        {
                            client.Expire(key, int.Parse(Encoding.UTF8.GetString(expirationValue)));
                        }
                    }
                }
            }
        }

        public Task RefreshAsync(string key, CancellationToken token = default(CancellationToken))
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return Task.Run(() => Refresh(key));
        }

        public void Remove(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            using (var client = _redisManager.GetClient() as IRedisNativeClient)
            {
                client.Del(key);
            }
        }

        public Task RemoveAsync(string key, CancellationToken token = default(CancellationToken))
        {
            return Task.Run(() => Remove(key));
        }

        private int GetExpireInSeconds(DistributedCacheEntryOptions options)
        {
            if (options.SlidingExpiration.HasValue)
            {
                return (int)options.SlidingExpiration.Value.TotalSeconds;
            }
            else if (options.AbsoluteExpiration.HasValue)
            {
                return (int)options.AbsoluteExpirationRelativeToNow.Value.TotalSeconds;
            }
            else
            {
                return 0;
            }
        }

        private string GetExpirationKey(string key)
        {
            return key + $"-{nameof(DistributedCacheEntryOptions)}";
        }
    }
}