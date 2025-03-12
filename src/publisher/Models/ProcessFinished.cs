namespace Publisher.Models;

public class ProcessFinished
{
    public Guid Id { get; set; }
    public DateTime FinishedAt { get; set; }
    public string Status { get; set; }
}