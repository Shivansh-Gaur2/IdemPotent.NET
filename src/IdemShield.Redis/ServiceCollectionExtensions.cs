using IdemShield.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdemShield.Redis
{
    /// <summary>Registers the Redis provider for IdemShield.</summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>Uses Redis as the application's idempotency store.</summary>
        /// <param name="services">The service collection.</param>
        /// <param name="connectionString">The Redis configuration string.</param>
        /// <returns>The same service collection for chaining.</returns>
        public static IServiceCollection UseRedisStore(this IServiceCollection services, string connectionString)
        {
            services.AddSingleton<IConnectionMultiplexer>(
                _ => ConnectionMultiplexer.Connect(connectionString));

            services.AddScoped<IIdempotencyStore, RedisIdempotencyStore>();

            return services;
        }
    }
}
