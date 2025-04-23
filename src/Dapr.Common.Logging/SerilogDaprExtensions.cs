using System;
using Dapr.Client;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;

namespace Dapr.Common.Logging
{
    /// <summary>
    /// Extension methods for configuring Serilog to use the Dapr PubSub sink
    /// </summary>
    public static class SerilogDaprExtensions
    {
        /// <summary>
        /// Write Serilog events to a Dapr PubSub component
        /// </summary>
        /// <param name="loggerSinkConfiguration">The logger sink configuration</param>
        /// <param name="daprClient">The DaprClient used to publish events</param>
        /// <param name="pubsubName">The name of the pubsub component in Dapr</param>
        /// <param name="topicName">The topic to publish log events to</param>
        /// <param name="formatProvider">Optional format provider for rendering log events</param>
        /// <returns>Logger configuration, allowing configuration to continue</returns>
        public static LoggerConfiguration DaprPubSub(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            DaprClient daprClient,
            string pubsubName,
            string topicName,
            IFormatProvider? formatProvider = null
        )
        {
            if (loggerSinkConfiguration == null)
                throw new ArgumentNullException(nameof(loggerSinkConfiguration));
            if (daprClient == null)
                throw new ArgumentNullException(nameof(daprClient));
            if (string.IsNullOrEmpty(pubsubName))
                throw new ArgumentNullException(nameof(pubsubName));
            if (string.IsNullOrEmpty(topicName))
                throw new ArgumentNullException(nameof(topicName));

            return loggerSinkConfiguration.Sink(
                new DaprPubSubSink(daprClient, pubsubName, topicName, formatProvider)
            );
        }

        /// <summary>
        /// Write Serilog events to a Dapr PubSub component, resolving DaprClient from the IServiceProvider
        /// </summary>
        /// <param name="loggerSinkConfiguration">The logger sink configuration</param>
        /// <param name="serviceProvider">The IServiceProvider to resolve DaprClient from</param>
        /// <param name="pubsubName">The name of the pubsub component in Dapr</param>
        /// <param name="topicName">The topic to publish log events to</param>
        /// <param name="formatProvider">Optional format provider for rendering log events</param>
        /// <returns>Logger configuration, allowing configuration to continue</returns>
        public static LoggerConfiguration DaprPubSub(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            IServiceProvider serviceProvider,
            string pubsubName,
            string topicName,
            IFormatProvider? formatProvider = null
        )
        {
            if (loggerSinkConfiguration == null)
                throw new ArgumentNullException(nameof(loggerSinkConfiguration));
            if (serviceProvider == null)
                throw new ArgumentNullException(nameof(serviceProvider));
            if (string.IsNullOrEmpty(pubsubName))
                throw new ArgumentNullException(nameof(pubsubName));
            if (string.IsNullOrEmpty(topicName))
                throw new ArgumentNullException(nameof(topicName));

            return loggerSinkConfiguration.Sink(
                new ServiceProviderDaprPubSubSink(
                    serviceProvider,
                    pubsubName,
                    topicName,
                    formatProvider
                )
            );
        }
    }
}
