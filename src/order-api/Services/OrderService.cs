using Dapr.Client;
using OrderApi.Models;
using Dapr.Common.Logging;

namespace OrderApi.Services;

public class OrderService : IOrderService
{
    private readonly ILogger<OrderService> logger;
    private readonly BusinessEventLogger<OrderService> businessLogger;
    private readonly DaprClient daprClient;

    public OrderService(
        DaprClient daprClient, 
        ILogger<OrderService> logger,
        BusinessEventLogger<OrderService> businessLogger)
    {
        this.daprClient = daprClient;
        this.logger = logger;
        this.businessLogger = businessLogger;
    }

    public async Task<Order> StartProcessAsync(Order order, Dictionary<string, string> metadata)
    {
        await this.daprClient.PublishEventAsync<Order>("kafka-pubsub", "newOrder", order, metadata);

        this.businessLogger.LogEvent(
            order.Id.ToString(), 
            "NewOrder", 
            "New Order received", 
            order
        );

        // Additionally log the Dapr pub/sub event
        this.logger.LogDaprPubSubEvent(
            order.Id.ToString(),
            "kafka-pubsub",
            "newOrder",
            "Publish",
            order
        );

        return order;
    }
}
