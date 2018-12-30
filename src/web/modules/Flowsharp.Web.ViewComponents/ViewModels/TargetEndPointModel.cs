namespace Flowsharp.Web.ViewComponents.ViewModels
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