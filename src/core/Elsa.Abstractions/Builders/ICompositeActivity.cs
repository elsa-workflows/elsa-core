namespace Elsa.Builders
{
    public interface ICompositeActivity<in TBuilder> where TBuilder: ICompositeActivityBuilder
    {
        void Build(TBuilder compositeActivity);
    }
    
    public interface ICompositeActivity : ICompositeActivity<ICompositeActivityBuilder>
    {
    }
}