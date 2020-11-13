namespace ElsaDashboard.Blazor.Application.Models
{
    public class MenuItem
    {
        public MenuItem(string text, string url, string icon)
        {
            Text = text;
            Url = url;
            Icon = icon;
        }
        
        public string Text { get; set; }
        public string Icon { get; set; }
        public string Url { get; set; }
    }
}