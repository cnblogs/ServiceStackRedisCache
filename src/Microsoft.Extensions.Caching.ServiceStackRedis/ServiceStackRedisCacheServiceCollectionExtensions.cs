using System;
using System.ComponentModel;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.ServiceStackRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using ServiceStack.Redis;

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

            services.AddOptions();
            services.Configure(setupAction);
            services.TryAddSingleton<IDistributedCache, ServiceStackRedisCache>();

            return services;
        }

#if NET6_0_OR_GREATER
        public static IServiceCollection AddDistributedServiceStackRedisCache(this IServiceCollection services, string configName)
        {
            var config = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
            return services.AddDistributedServiceStackRedisCache(config.GetSection(configName));
        }
#endif

        public static IServiceCollection AddDistributedServiceStackRedisCache(this IServiceCollection services, IConfigurationSection section)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (section == null)
            {
                throw new ArgumentNullException(nameof(section));
            }

            services.Configure<ServiceStackRedisCacheOptions>(section);

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
