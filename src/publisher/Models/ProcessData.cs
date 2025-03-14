namespace Publisher.Models;

public class ProcessData
{
    public ProcessData(Guid id, DateTime startAt, string name)
    {
        Id = id;
        StartAt = startAt;
        Name = name;
        EndAt = null;
        Status = "Started";
    }

    public Guid Id { get; set; }
    public DateTime StartAt { get; set; }
    public string Name { get; set; }
    public DateTime? EndAt { get; set; }
    public string Status { get; set; }
}
