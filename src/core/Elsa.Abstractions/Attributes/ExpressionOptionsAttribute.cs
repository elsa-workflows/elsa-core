namespace Elsa.Attributes
{
    public class ExpressionOptionsAttribute : ActivityPropertyOptionsAttribute
    {
        public bool Multiline { get; set; }

        public override object GetOptions() => new ExpressionOptions(Multiline);
    }

    public class ExpressionOptions
    {
        public ExpressionOptions(bool multiline)
        {
            Multiline = multiline;
        }

        public bool Multiline { get; }
    }
}