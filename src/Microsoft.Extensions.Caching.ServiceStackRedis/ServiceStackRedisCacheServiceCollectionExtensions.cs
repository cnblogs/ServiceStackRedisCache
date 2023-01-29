using System;
using System.ComponentModel;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.ServiceStackRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using ServiceStack.Redis;
using static System.Collections.Specialized.BitVector32;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class RedisCacheServiceCollectionExtensions
    {
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddDistributedServiceStackRedisCache(this IServiceCollection services, Action<ServiceStackRedisCacheOptions> setupAction)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }

            return services.AddDistributedServiceStackRedisCache(
                services => setupAction(services));
        }

        public static IServiceCollection AddDistributedServiceStackRedisCache(this IServiceCollection services, string configName)
        {
            return services.AddDistributedServiceStackRedisCache(
                services => services.AddOptions<ServiceStackRedisCacheOptions>().BindConfiguration(configName));
        }

        public static IServiceCollection AddDistributedServiceStackRedisCache(this IServiceCollection services, IConfigurationSection section)
        {
            return services.AddDistributedServiceStackRedisCache(
                services => services.Configure<ServiceStackRedisCacheOptions>(section));
        }

        public static IServiceCollection AddDistributedServiceStackRedisCache(this IServiceCollection services, Action<IServiceCollection> configure)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddOptions();
            configure?.Invoke(services);

            services.TryAddSingleton<IRedisClientsManager>(provider =>
            {
                var options = provider.GetRequiredService<IOptions<ServiceStackRedisCacheOptions>>().Value;
                var host = $"{options.Password}@{options.Host}:{options.Port}";
                return new RedisManagerPool(host);
            });

            services.TryAddSingleton<IDistributedCache, ServiceStackRedisCache>();

            return services;
        }
    }
}
