namespace Elsa.Samples.TimesheetApproval.Models
{
    public class Timesheet
    {
        public int Id { get; set; }
        public string TimesheetId { get; set; }
        public string User { get; set; }
        public float TotalHours { get; set; }
        public TimesheetStatus Status { get; set; } = TimesheetStatus.Draft;
    }
}