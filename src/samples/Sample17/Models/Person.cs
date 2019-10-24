namespace Sample17.Models
{
    public class Person
    {
        public string FullName { get; set; }
        public int Age { get; set; }

        public override string ToString() => FullName;
    }
}