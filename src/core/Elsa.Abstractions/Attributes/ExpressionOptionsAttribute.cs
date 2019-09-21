namespace Elsa.Attributes
{
    public class ExpressionOptionsAttribute : ActivityPropertyOptionsAttribute
    {
        public bool Multiline { get; set; }

        public override object GetOptions()
        {
            return new
            {
                Multiline
            };
        }
    }
}