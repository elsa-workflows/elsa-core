// using Elsa.DropIns.Core;
//
// namespace Elsa.DropIns;
//
// public class DropInStartupRunner : IDropInStartupRunner
// {
//     public async Task RunAsync(IEnumerable<IDropInStartup> startups, CancellationToken cancellationToken = default)
//     {
//         foreach (var startup in startups)
//         {
//             await startup.RunAsync(cancellationToken);
//         }
//     }
// }