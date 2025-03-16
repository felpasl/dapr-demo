using Dapr.Client;
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

        await daprClient.PublishEventAsync<Order>(
            "kafka-pubsub",
            "newOrder",
            order,
            metadata
        );

        this.logger.LogInformation("New Order recieved: {@process}", order);

        return order;
    }
}
