using JetBrains.Annotations;

namespace Elsa.Secrets.UniqueName;

[UsedImplicitly] public record IsUniqueNameResponse(bool IsUnique);