namespace Publisher.Models;

public class ProcessFinished
{
    public Guid Id { get; set; }
    public DateTime FinishedAt { get; set; }
    public required string Status { get; set; }
}
