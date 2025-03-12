namespace Worker.Models
{
    public class WorkTodo
    {
        public Guid Id { get; set; }
        public Guid ProcessId { get; set; }
        public DateTime startAt { get; set; }
        public int Total { get; set; }
        public int Index { get; set; }
        public string Name { get; set; }
        public int Duration { get; set; }
        public string Status { get; set; }
    }
}
