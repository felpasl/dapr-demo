using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Dapr.Common.Logging
{
    /// <summary>
    /// Extension methods for structured logging with business context
    /// </summary>
    public static class LoggingExtensions
    {
        // Common scope key names for standardization
        private const string CorrelationIdKey = "CorrelationId";
        private const string BusinessEventKey = "BusinessEvent";
        private const string TraceIdKey = "TraceId";
        private const string SpanIdKey = "SpanId";
        
        /// <summary>
        /// Creates a logging scope for business events
        /// </summary>
        /// <param name="logger">The logger to create scope for</param>
        /// <param name="correlationId">A unique identifier for correlating related log entries</param>
        /// <param name="businessEvent">Optional business event name to include in the scope</param>
        /// <param name="additionalProperties">Optional additional properties to include in all logs within this scope</param>
        /// <returns>A disposable scope</returns>
        public static IDisposable BeginScope(
            this ILogger logger,
            string correlationId,
            string businessEvent,
            Dictionary<string, object>? additionalProperties = null
        )
        {
            // Create the base scope dictionary
            var scopeState = new Dictionary<string, object> { [CorrelationIdKey] = correlationId };

            scopeState[BusinessEventKey] = businessEvent;

            // Add any distributed tracing context if available
            var activity = Activity.Current;
            if (activity != null)
            {
                scopeState[TraceIdKey] = activity.TraceId.ToString();
                scopeState[SpanIdKey] = activity.SpanId.ToString();
            }

            // Add any additional properties
            if (additionalProperties != null)
            {
                foreach (var prop in additionalProperties)
                {
                    scopeState[prop.Key] = prop.Value;
                }
            }
            // Create the scope
            return logger.BeginScope(scopeState);
        }

        /// <summary>
        /// Logs a business event with structured data
        /// </summary>
        /// <param name="logger">The logger to use</param>
        /// <param name="message">The log message</param>
        /// <param name="data">Optional structured data to include in the log</param>
        /// <param name="logLevel">The log level (defaults to Information)</param>
        public static void LogEvent(
            this ILogger logger,
            string message,
            object? data = null,
            LogLevel logLevel = LogLevel.Information
        )
        {
            if (data != null)
            {
                switch (logLevel)
                {
                    case LogLevel.Debug:
                        logger.LogDebug(message + ": {@Data}", data);
                        break;
                    case LogLevel.Information:
                        logger.LogInformation(message + ": {@Data}", data);
                        break;
                    case LogLevel.Warning:
                        logger.LogWarning(message + ": {@Data}", data);
                        break;
                    case LogLevel.Error:
                        logger.LogError(message + ": {@Data}", data);
                        break;
                    default:
                        logger.LogInformation(message + ": {@Data}", data);
                        break;
                }
            }
            else
            {
                switch (logLevel)
                {
                    case LogLevel.Debug:
                        logger.LogDebug(message);
                        break;
                    case LogLevel.Information:
                        logger.LogInformation(message);
                        break;
                    case LogLevel.Warning:
                        logger.LogWarning(message);
                        break;
                    case LogLevel.Error:
                        logger.LogError(message);
                        break;
                    default:
                        logger.LogInformation(message);
                        break;
                }
            }
        }
    }
}
