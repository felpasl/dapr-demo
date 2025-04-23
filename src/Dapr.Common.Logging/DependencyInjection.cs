using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dapr.Common.Logging
{
    /// <summary>
    /// Extensions for registering logging services with dependency injection
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Adds the BusinessEventLogger to the service collection
        /// </summary>
        public static IServiceCollection AddBusinessEventLogger(this IServiceCollection services)
        {
            // Register BusinessEventLogger for each type that requests it via DI
            services.AddTransient(typeof(BusinessEventLogger<>));
            return services;
        }
    }
}
