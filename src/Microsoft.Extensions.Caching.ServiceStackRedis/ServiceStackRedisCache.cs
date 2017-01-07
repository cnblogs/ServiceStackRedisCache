using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using ServiceStack.Redis;
using ServiceStack.Redis.Generic;
using System.IO;

namespace Microsoft.Extensions.Caching.ServiceStackRedis
{
    public class ServiceStackRedisCache : IDistributedCache, IDisposable
    {
        private readonly IRedisNativeClient _cache;
        private readonly ServiceStackRedisCacheOptions _options;

        public ServiceStackRedisCache(IOptions<ServiceStackRedisCacheOptions> optionsAccessor)
        {
            if (optionsAccessor == null)
            {
                throw new ArgumentNullException(nameof(optionsAccessor));
            }

            _options = optionsAccessor.Value;

            var host = $"{_options.Password}@{_options.Host}:{_options.Port}";
            var manager = new RedisManagerPool(host);
            var client = manager.GetClient() as IRedisNativeClient;
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }
            _cache = client;
        }

        public byte[] Get(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (_cache.Exists(key) == 1)
            {
                return _cache.Get(key);
            }

            return null;
        }

        public async Task<byte[]> GetAsync(string key)
        {
            return Get(key);
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

            var expireInSeconds = GetExpireInSeconds(options);
            if (expireInSeconds > 0)
            {
                _cache.SetEx(key, expireInSeconds, value);
                _cache.SetEx(GetExpirationKey(key), expireInSeconds, Encoding.UTF8.GetBytes(expireInSeconds.ToString()));
            }
            else
            {
                _cache.Set(key, value);
            }
        }

        public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            Set(key, value, options);
        }

        public void Refresh(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (_cache.Exists(key) == 1)
            {
                var value = _cache.Get(key);
                if (value != null)
                {
                    var expirationValue = _cache.Get(GetExpirationKey(key));
                    if (expirationValue != null)
                    {
                        _cache.Expire(key, int.Parse(Encoding.UTF8.GetString(expirationValue)));
                    }
                }
            }
        }

        public async Task RefreshAsync(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            Refresh(key);
        }

        public void Remove(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            _cache.Del(key);
        }

        public async Task RemoveAsync(string key)
        {
            Remove(key);
        }

        public void Dispose()
        {
            if (_cache != null)
            {
                _cache.Dispose();
            }
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