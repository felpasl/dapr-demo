using Dapr.Client;
using Dapr.Common.Logging;
using OrderApi.Models;

namespace OrderApi.Services;

public class OrderService : IOrderService
{
    private readonly ILogger<OrderService> logger;
    private readonly DaprClient daprClient;

    public OrderService(DaprClient daprClient, ILogger<OrderService> logger)
    {
        this.daprClient = daprClient;
        this.logger = logger;
    }

    public async Task<Order> StartProcessAsync(Order order, Dictionary<string, string> metadata)
    {
        using (this.logger.BeginScope(order.Id.ToString(), "NewOrder"))
        {
            await this.daprClient.PublishEventAsync<Order>(
                "kafka-pubsub",
                "newOrder",
                order,
                metadata
            );

            this.logger.LogEvent("New Order received", order);

            return order;
        }
    }
}
