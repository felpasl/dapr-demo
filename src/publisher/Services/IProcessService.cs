using Publisher.Models;

namespace Publisher.Services;

public interface IProcessService
{
    Task<ProcessData> StartProcessAsync(Dictionary<string, string> metadata);
}
