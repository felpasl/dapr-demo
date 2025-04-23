using Dapr.Client;
using OrderItemProcessing.Models;
using Dapr.Common.Logging;

namespace OrderItemProcessing.Services;

public class OrderItemService : IOrderItemService
{
    private readonly DaprClient daprClient;
    private readonly ILogger<OrderItemService> logger;
    private readonly BusinessEventLogger<OrderItemService> businessLogger;

    public OrderItemService(
        DaprClient daprClient, 
        ILogger<OrderItemService> logger,
        BusinessEventLogger<OrderItemService> businessLogger)
    {
        this.daprClient = daprClient;
        this.logger = logger;
        this.businessLogger = businessLogger;
    }

    public async Task ProcessWorkAsync(
        Models.OrderItem orderItem,
        Dictionary<string, string> metadata
    )
    {
        this.businessLogger.LogEvent(
            orderItem.Id.ToString(),
            "OrderItemProcessing",
            "Processing order item",
            orderItem
        );

        await Task.Delay(orderItem.Duration);

        orderItem.Status = "Completed";

        this.businessLogger.LogEvent(
            orderItem.Id.ToString(),
            "OrderItemCompleted",
            "Order item processing completed",
            orderItem
        );

        await this.daprClient.PublishEventAsync<Models.OrderItem>(
            "kafka-pubsub",
            "orderItemCompleted",
            orderItem,
            metadata
        );

        // Log the Dapr pub/sub event
        this.logger.LogDaprPubSubEvent(
            orderItem.Id.ToString(),
            "kafka-pubsub",
            "orderItemCompleted",
            "Publish",
            orderItem
        );

        if (orderItem.Index == orderItem.Total - 1)
        {
            var completedOrder = new OrderCompleted(orderItem.ProcessId, DateTime.Now, "Success");
            
            this.businessLogger.LogEvent(
                orderItem.ProcessId.ToString(),
                "OrderCompleted",
                "All order items processed, order completed",
                completedOrder
            );
            
            await this.daprClient.PublishEventAsync<OrderCompleted>(
                "kafka-pubsub",
                "orderCompleted",
                completedOrder,
                metadata
            );

            // Log the Dapr pub/sub event for order completion
            this.logger.LogDaprPubSubEvent(
                orderItem.ProcessId.ToString(),
                "kafka-pubsub",
                "orderCompleted",
                "Publish",
                completedOrder
            );
        }
    }
}
