using OrderApi.Models;

namespace OrderApi.Services;

public interface IOrderService
{
    Task<Order> StartProcessAsync(Order data, Dictionary<string, string> metadata);
}
