namespace ElsaDashboard.Application.Models
{
    public record TabItem(string Text, string Name)
    {
        public TabItem(string text) : this(text, text)
        {
        }
    }
    
}