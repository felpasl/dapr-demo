using Consumer.Models;

namespace Consumer.Services
{
    public interface IConsumerService
    {
        Task ProcessNewWorkAsync(ProcessData process, Dictionary<string, string> metadata);
    }
}
