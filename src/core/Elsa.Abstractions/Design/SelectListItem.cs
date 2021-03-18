namespace Elsa.Design
{
    public class SelectListItem
    {
        public SelectListItem()
        {
        }

        public SelectListItem(string text, string value)
        {
            Text = text;
            Value = value;
        }
        
        public SelectListItem(string text)
        {
            Text = text;
            Value = text;
        }
        
        public string Text { get; set; } = default!;
        public string Value { get; set; }= default!;
    }
}