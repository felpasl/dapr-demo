using Microsoft.Extensions.Logging;

namespace Dapr.Common.Logging
{
    /// <summary>
    /// Base class for logging business events with consistent format and context
    /// </summary>
    public class BusinessEventLogger<T>
    {
        private readonly ILogger<T> logger;

        public BusinessEventLogger(ILogger<T> logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Logs a business event with correlation ID and structured data
        /// </summary>
        public void LogEvent(
            string correlationId, 
            string businessEvent, 
            string message, 
            object? data = null, 
            LogLevel logLevel = LogLevel.Information)
        {
            this.logger.LogBusinessEvent(correlationId, businessEvent, message, data, logLevel);
        }

        /// <summary>
        /// Creates a scoped logger with business context information
        /// </summary>
        public IDisposable? BeginBusinessScope(string correlationId, string businessEvent, string? operation = null)
        {
            return this.logger.WithBusinessContext(correlationId, businessEvent, operation);
        }
    }
}
