using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdemPotent.Core
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddIdempotency(this IServiceCollection services, Action<IdempotencyOptions>? configure = null)
        {
            var options = new IdempotencyOptions();
            configure?.Invoke(options);

            services.AddSingleton(options);

            return services;
        }

        public static IApplicationBuilder UseIdempotency(this IApplicationBuilder app)
        {
            return app.UseMiddleware<IdempotencyMiddleware>();
        }
    }
}
