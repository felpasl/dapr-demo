namespace OrderApi.Models;

public class OrderCompleted
{
    public Guid Id { get; set; }
    public DateTime FinishedAt { get; set; }
    public required string Status { get; set; }
}
