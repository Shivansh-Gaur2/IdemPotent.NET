using IdemPotent.Core;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdemPotent.Redis
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection UseRedisStore(this IServiceCollection services, string connectionString)
        {
            services.AddSingleton<IConnectionMultiplexer>(
                ConnectionMultiplexer.Connect(connectionString));

            services.AddScoped<IIdempotencyStore, RedisIdempotencyStore>();

            return services;
        }
    }
}
