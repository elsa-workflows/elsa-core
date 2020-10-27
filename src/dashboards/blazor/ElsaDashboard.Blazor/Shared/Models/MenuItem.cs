namespace ElsaDashboard.Blazor.Shared.Models
{
    public class MenuItem
    {
        public MenuItem(string text, string icon)
        {
            Text = text;
            Icon = icon;
        }
        
        public string Text { get; set; }
        public string Icon { get; set; }
    }
}