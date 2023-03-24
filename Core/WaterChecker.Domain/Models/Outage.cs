namespace WaterChecker.Domain.Models
{
    public class Outage
    {
        public Guid Id { get; set; }

        public DateOnly Date { get; set; }

        public string Message { get; set; }
    }
}
