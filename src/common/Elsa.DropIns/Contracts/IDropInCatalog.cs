using Elsa.DropIns.Models;

namespace Elsa.DropIns.Contracts;

public interface IDropInCatalog
{
    IEnumerable<DropInDescriptor> List();
}