using OrderProcessing.Models;

namespace OrderProcessing.Services
{
    public interface IOrderProcessingService
    {
        Task ProcessNewWorkAsync(Order order, Dictionary<string, string> metadata);
    }
}
