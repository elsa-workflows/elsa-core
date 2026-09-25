# Pinned Studio client source

`Client/ISecretsApi.cs` and `Models/SecretModels.cs` are exact source snapshots from Studio commit
`f0eeb3c7428443b09512049fe890635fa4f7b427`. Both files have the same Git blobs as the previous
`20ceaeeed7e671f0c9662003e82063026f2216de` and `9afd3e36fd1bc90dfdf8ea00b40d89e4a50c8822` pins. The pinned-source contract workflow compares each file to
that commit before building this integration test, so the Refit proxy exercised by the test cannot drift
from the recorded Studio source without a review-visible fixture and pin update.

The reviewed Git blobs are `27f4d47ccf6937f125d47d5efc7862d966cef3d1` for `ISecretsApi.cs` and
`f33f97684f1b599dceef3fa7323cab4b396b87be` for `SecretModels.cs` at both the former and current tip.
`Pages/Secrets.razor` did change between these commits; the browser proof remains separate.
