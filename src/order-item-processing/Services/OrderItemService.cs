using Dapr.Client;
using Dapr.Common.Logging;
using OrderItemProcessing.Models;

namespace OrderItemProcessing.Services;

public class OrderItemService : IOrderItemService
{
    private readonly DaprClient daprClient;
    private readonly ILogger<OrderItemService> logger;

    public OrderItemService(
        DaprClient daprClient,
        ILogger<OrderItemService> logger
    )
    {
        this.daprClient = daprClient;
        this.logger = logger;
    }

    public async Task ProcessWorkAsync(
        Models.OrderItem orderItem,
        Dictionary<string, string> metadata
    )
    {
        using (this.logger.BeginScope(orderItem.ProcessId.ToString(), "OrderItemProcessing"))
        {
            this.logger.LogEvent("Processing order item", orderItem);

            await Task.Delay(orderItem.Duration);

            orderItem.Status = "Completed";

            using (this.logger.BeginScope(orderItem.ProcessId.ToString(), "OrderItemCompleted"))
            {
                this.logger.LogEvent("Order item processing completed", orderItem);

                await this.daprClient.PublishEventAsync<Models.OrderItem>(
                    "kafka-pubsub",
                    "orderItemCompleted",
                    orderItem,
                    metadata
                );

                // Log the Dapr pub/sub event
                this.logger.LogDaprPubSubEvent(
                    orderItem.ProcessId.ToString(),
                    "kafka-pubsub",
                    "orderItemCompleted",
                    "Publish",
                    orderItem
                );

                if (orderItem.Index == orderItem.Total - 1)
                {
                    var completedOrder = new OrderCompleted(orderItem.ProcessId, DateTime.Now, "Success");

                    using (this.logger.BeginScope(orderItem.ProcessId.ToString(), "OrderCompleted"))
                    {
                        this.logger.LogEvent("All order items processed, order completed", completedOrder);

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
        }
    }
}
