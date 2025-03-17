using OrderProcessing.Models;

namespace OrderProcessing.Services;

public interface IOrderService
{
    Task NewOrderAsync(Order order, Dictionary<string, string> metadata);
}
