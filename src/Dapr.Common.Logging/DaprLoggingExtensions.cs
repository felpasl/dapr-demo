using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Dapr.Common.Logging
{
    /// <summary>
    /// Extension methods for Dapr-specific logging scenarios
    /// </summary>
    public static class DaprLoggingExtensions
    {
        /// <summary>
        /// Logs a Dapr service invocation with full contextual information
        /// </summary>
        public static void LogDaprServiceInvocation<T>(
            this ILogger<T> logger,
            string correlationId,
            string appId,
            string methodName,
            object? request = null,
            object? response = null
        )
        {
            using (
                logger.WithBusinessContext(
                    correlationId,
                    "ServiceInvocation",
                    $"{appId}.{methodName}"
                )
            )
            {
                logger.LogInformation(
                    "Dapr service invocation: AppId={AppId}, Method={Method}, Request={@Request}, Response={@Response}",
                    appId,
                    methodName,
                    request,
                    response
                );
            }
        }

        /// <summary>
        /// Logs a Dapr pub/sub event with full contextual information
        /// </summary>
        public static void LogDaprPubSubEvent<T>(
            this ILogger<T> logger,
            string correlationId,
            string pubsubName,
            string topic,
            string eventType,
            object? payload = null
        )
        {
            logger.LogInformation(
                "Dapr pub/sub event: PubSub={PubSub}, Topic={Topic}, EventType={EventType}, Payload={@Payload}",
                pubsubName,
                topic,
                eventType,
                payload
            );
        }

        /// <summary>
        /// Logs a Dapr state operation with full contextual information
        /// </summary>
        public static void LogDaprStateOperation<T>(
            this ILogger<T> logger,
            string correlationId,
            string storeName,
            string operation,
            string key,
            object? value = null
        )
        {
            using (
                logger.WithBusinessContext(
                    correlationId,
                    "StateOperation",
                    $"{storeName}.{operation}"
                )
            )
            {
                logger.LogInformation(
                    "Dapr state operation: Store={Store}, Operation={Operation}, Key={Key}, Value={@Value}",
                    storeName,
                    operation,
                    key,
                    value
                );
            }
        }
    }
}
