using OrderItemProcessing.Models;

namespace OrderItemProcessing.Services;

public interface IOrderItemService
{
    Task ProcessWorkAsync(Models.OrderItem work, Dictionary<string, string> metadata);
}
