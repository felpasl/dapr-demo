namespace OrderProcessing.Models;

public class OrderItem
{
    public Guid Id { get; set; }
    public Guid ProcessId { get; set; }
    public DateTime startAt { get; set; }
    public int Total { get; set; }
    public int Index { get; set; }
    public required string Name { get; set; }
    public int Duration { get; set; }
    public required string Status { get; set; }
}
