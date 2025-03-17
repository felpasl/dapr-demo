namespace OrderItemProcessing.Models;

public class OrderCompleted
{
    public OrderCompleted(Guid id, DateTime finishedAt, string status)
    {
        Id = id;
        FinishedAt = finishedAt;
        Status = status;
    }

    public Guid Id { get; set; }
    public DateTime FinishedAt { get; set; }
    public string Status { get; set; }
}
