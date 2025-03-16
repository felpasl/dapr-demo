namespace OrderProcessing.Models;

public class Order
{
    public Order(Guid id, DateTime startAt, int quantity, string name)
    {
        Id = id;
        StartAt = startAt;
        Quantity = quantity;
        Name = name;
        EndAt = null;
        Status = "Started";
    }

    public Guid Id { get; set; }
    public DateTime StartAt { get; set; }
    public int Quantity { get; set; }
    public string Name { get; set; }
    public DateTime? EndAt { get; set; }
    public string Status { get; set; }
}
