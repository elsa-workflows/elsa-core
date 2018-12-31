using Elsa.Models;

namespace Elsa.Web.Components.ViewModels
{
    public class ConnectionModel
    {
        public ConnectionModel(Connection connection)
        {
            Source = new SourceEndPointModel(connection.Source.Name, connection.Source.Activity.Id);
            Target = new TargetEndPointModel(connection.Target.Activity.Id);
        }

        public SourceEndPointModel Source { get; }
        public TargetEndPointModel Target { get; }
    }
}