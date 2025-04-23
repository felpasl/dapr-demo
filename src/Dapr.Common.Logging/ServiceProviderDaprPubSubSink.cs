using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Dapr.Client;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Core;
using Serilog.Events;

namespace Dapr.Common.Logging
{
    /// <summary>
    /// A Serilog sink that publishes log events to a Dapr PubSub component
    /// using DaprClient from dependency injection
    /// </summary>
    public class ServiceProviderDaprPubSubSink : ILogEventSink
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly string _pubsubName;
        private readonly string _topicName;
        private readonly IFormatProvider? _formatProvider;

        /// <summary>
        /// Creates a new instance of the ServiceProviderDaprPubSubSink
        /// </summary>
        /// <param name="serviceProvider">Service provider to resolve DaprClient from</param>
        /// <param name="pubsubName">The name of the pubsub component in Dapr</param>
        /// <param name="topicName">The topic to publish log events to</param>
        /// <param name="formatProvider">Optional format provider for rendering log events</param>
        public ServiceProviderDaprPubSubSink(
            IServiceProvider serviceProvider,
            string pubsubName,
            string topicName,
            IFormatProvider? formatProvider = null
        )
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _pubsubName = pubsubName ?? throw new ArgumentNullException(nameof(pubsubName));
            _topicName = topicName ?? throw new ArgumentNullException(nameof(topicName));
            _formatProvider = formatProvider;
        }

        /// <summary>
        /// Emits a log event to the Dapr PubSub component
        /// </summary>
        /// <param name="logEvent">The log event to emit</param>
        public void Emit(LogEvent logEvent)
        {
            // Fire and forget
            _ = PublishLogEventAsync(logEvent);
        }

        private async Task PublishLogEventAsync(LogEvent logEvent)
        {
            try
            {
                // Create a scope to resolve the DaprClient
                using var scope = _serviceProvider.CreateScope();
                var daprClient = scope.ServiceProvider.GetRequiredService<DaprClient>();

                // Extract properties as a dictionary
                var properties = logEvent.Properties.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.ToString("l", _formatProvider)
                );

                // Create the log event object to publish
                var publishEvent = new DaprLogEvent
                {
                    Timestamp = logEvent.Timestamp.UtcDateTime,
                    Level = logEvent.Level.ToString(),
                    Message = logEvent.RenderMessage(_formatProvider),
                    Exception = logEvent.Exception?.ToString(),
                    Properties = properties,
                };

                // Create metadata for the Dapr publish
                var metadata = new Dictionary<string, string>();

                // Add key properties as metadata for potential filtering
                if (properties.TryGetValue("CorrelationId", out var correlationId))
                {
                    metadata["correlationId"] = correlationId;
                }

                if (properties.TryGetValue("BusinessEvent", out var businessEvent))
                {
                    metadata["businessEvent"] = businessEvent;
                }

                // Add log level to metadata
                metadata["logLevel"] = logEvent.Level.ToString();

                // Publish the event
                await daprClient.PublishEventAsync(
                    _pubsubName,
                    _topicName,
                    publishEvent,
                    metadata
                );
            }
            catch (Exception ex)
            {
                // Can't use the logger here to avoid potential infinite loops
                Console.Error.WriteLine($"Error publishing log event to Dapr: {ex}");
            }
        }
    }
}