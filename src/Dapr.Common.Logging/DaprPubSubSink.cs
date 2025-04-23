using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Dapr.Client;
using Serilog.Core;
using Serilog.Events;

namespace Dapr.Common.Logging
{
    /// <summary>
    /// A Serilog sink that publishes log events to a Dapr PubSub component
    /// </summary>
    public class DaprPubSubSink : ILogEventSink
    {
        private readonly DaprClient daprClient;
        private readonly string pubsubName;
        private readonly string topicName;
        private readonly IFormatProvider? formatProvider;

        /// <summary>
        /// Creates a new instance of the DaprPubSubSink
        /// </summary>
        /// <param name="daprClient">Dapr client used to publish messages</param>
        /// <param name="pubsubName">The name of the pubsub component in Dapr</param>
        /// <param name="topicName">The topic to publish log events to</param>
        /// <param name="formatProvider">Optional format provider for rendering log events</param>
        public DaprPubSubSink(
            DaprClient daprClient,
            string pubsubName,
            string topicName,
            IFormatProvider? formatProvider = null
        )
        {
            this.daprClient = daprClient;
            this.pubsubName = pubsubName;
            this.topicName = topicName;
            this.formatProvider = formatProvider;
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
                // Extract properties as a dictionary
                var properties = logEvent.Properties.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.ToString("l", this.formatProvider)
                );

                // Create the log event object to publish
                var publishEvent = new DaprLogEvent
                {
                    Timestamp = logEvent.Timestamp.UtcDateTime,
                    Level = logEvent.Level.ToString(),
                    Message = logEvent.RenderMessage(this.formatProvider),
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
                await this.daprClient.PublishEventAsync(
                    this.pubsubName,
                    this.topicName,
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

    /// <summary>
    /// Represents a structured log event that can be published to Dapr
    /// </summary>
    public class DaprLogEvent
    {
        /// <summary>
        /// The timestamp when the event occurred
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// The log level
        /// </summary>
        public string? Level { get; set; }

        /// <summary>
        /// The log message
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// The exception details, if any
        /// </summary>
        public string? Exception { get; set; }

        /// <summary>
        /// The properties associated with the log event
        /// </summary>
        public Dictionary<string, string>? Properties { get; set; }
    }
}
