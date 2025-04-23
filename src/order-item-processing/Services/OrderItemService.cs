using Dapr.Client;
using Dapr.Common.Logging;
using OrderItemProcessing.Models;

namespace OrderItemProcessing.Services;

public class OrderItemService : IOrderItemService
{
    private readonly DaprClient daprClient;
    private readonly ILogger<OrderItemService> logger;

    public OrderItemService(DaprClient daprClient, ILogger<OrderItemService> logger)
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

            this.logger.LogEvent("Order item processing completed", orderItem);

            await this.daprClient.PublishEventAsync<Models.OrderItem>(
                "kafka-pubsub",
                "orderItemCompleted",
                orderItem,
                metadata
            );

            if (orderItem.Index == orderItem.Total - 1)
            {
                var completedOrder = new OrderCompleted(
                    orderItem.ProcessId,
                    DateTime.Now,
                    "Success"
                );

                this.logger.LogEvent("All order items processed, order completed", completedOrder);

                await this.daprClient.PublishEventAsync<OrderCompleted>(
                    "kafka-pubsub",
                    "orderCompleted",
                    completedOrder,
                    metadata
                );
            }
        }
    }
}
