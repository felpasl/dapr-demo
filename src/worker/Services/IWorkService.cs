using Worker.Models;

namespace Worker.Services
{
    public interface IWorkService
    {
        Task ProcessWorkAsync(WorkTodo work, Dictionary<string, string> metadata);
    }
}
