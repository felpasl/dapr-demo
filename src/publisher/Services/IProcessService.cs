using Publisher.Models;

namespace Publisher.Services;

public interface IProcessService
{
    Task<ProcessData> StartProcessAsync();
}
