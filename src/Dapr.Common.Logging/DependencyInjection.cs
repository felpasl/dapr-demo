using Microsoft.Extensions.DependencyInjection;

namespace Dapr.Common.Logging
{
    /// <summary>
    /// Extension methods for setting up Dapr logging services in an Microsoft.Extensions.DependencyInjection.IServiceCollection.
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Adds Dapr logging services to the specified Microsoft.Extensions.DependencyInjection.IServiceCollection.
        /// </summary>
        /// <param name="services">The Microsoft.Extensions.DependencyInjection.IServiceCollection to add services to.</param>
        /// <returns>The Microsoft.Extensions.DependencyInjection.IServiceCollection so that additional calls can be chained.</returns>
        public static IServiceCollection AddDaprLogging(this IServiceCollection services)
        {
            // No additional services needed for the simplified logging approach
            // The LoggingExtensions class provides extension methods directly on ILogger
            return services;
        }
        
        // Note: AddBusinessEventLogger has been removed as we're now using LoggingExtensions directly
    }
}
