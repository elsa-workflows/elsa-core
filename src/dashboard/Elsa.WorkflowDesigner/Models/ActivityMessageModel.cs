namespace Elsa.WorkflowDesigner.Models
{
    public class ActivityMessageModel
    {
        public ActivityMessageModel()
        {
        }
        
        public ActivityMessageModel(string title, string content)
        {
            Title = title;
            Content = content;
        }
        
        public string? Title { get; set; }
        public string? Content { get; set; }
    }
}