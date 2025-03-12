using Consumer.Models;

namespace Consumer.Services
{
    public interface IProcessService
    {
        Task ProcessNewWorkAsync(ProcessData process, Dictionary<string, string> metadata);
    }
}
