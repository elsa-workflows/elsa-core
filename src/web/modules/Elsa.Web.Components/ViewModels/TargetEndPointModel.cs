namespace Elsa.Web.Components.ViewModels
{
    public class TargetEndPointModel
    {
        public TargetEndPointModel(string activityId)
        {
            ActivityId = activityId;
        }
        
        public string ActivityId { get; }
    }
}