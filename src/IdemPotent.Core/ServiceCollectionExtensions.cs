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
        /// <summary>Adds idempotency services and optionally configures their behavior.</summary>
        public static IServiceCollection AddIdempotency(this IServiceCollection services, Action<IdempotencyOptions>? configure = null)
        {
            var options = new IdempotencyOptions();
            configure?.Invoke(options);
            options.Validate();

            services.AddSingleton(options);

            return services;
        }

        /// <summary>Adds the idempotency middleware to the application pipeline.</summary>
        public static IApplicationBuilder UseIdempotency(this IApplicationBuilder app)
        {
            return app.UseMiddleware<IdempotencyMiddleware>();
        }
    }
}
