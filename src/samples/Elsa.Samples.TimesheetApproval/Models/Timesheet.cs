namespace Elsa.Samples.TimesheetApproval.Models
{
    public class Timesheet
    {
        public string Id { get; set; }
        public string User { get; set; }
        public float TotalHours { get; set; }
        public TimesheetStatus Status { get; set; } = TimesheetStatus.Draft;
    }
}