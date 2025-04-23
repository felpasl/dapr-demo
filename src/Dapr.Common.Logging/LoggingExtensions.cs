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
        /// <summary>
        /// Creates a scoped logger with business context information including correlation ID and business event
        /// </summary>
        public static IDisposable? WithBusinessContext(
            this ILogger logger,
            string correlationId,
            string businessEvent,
            string? operationName = null
        )
        {
            var state = new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId,
                ["BusinessEvent"] = businessEvent,
            };

            // Add operation name if provided
            if (!string.IsNullOrEmpty(operationName))
            {
                state["OperationName"] = operationName;
            }

            // Add distributed tracing context if available
            var activity = Activity.Current;
            if (activity != null)
            {
                state["TraceId"] = activity.TraceId.ToString();
                state["SpanId"] = activity.SpanId.ToString();
            }

            return logger.BeginScope(state);
        }

        /// <summary>
        /// Logs a business event with correlation ID and structured data
        /// </summary>
        public static void LogBusinessEvent(
            this ILogger logger,
            string correlationId,
            string businessEvent,
            string message,
            object? data = null,
            LogLevel logLevel = LogLevel.Information
        )
        {
            using (logger.WithBusinessContext(correlationId, businessEvent))
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
}
