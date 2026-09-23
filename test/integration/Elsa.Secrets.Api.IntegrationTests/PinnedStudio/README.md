# Pinned Studio client source

`Client/ISecretsApi.cs` and `Models/SecretModels.cs` are exact source snapshots from Studio commit
`9afd3e36fd1bc90dfdf8ea00b40d89e4a50c8822`. The pinned-source contract workflow compares each file to
that commit before building this integration test, so the Refit proxy exercised by the test cannot drift
from the recorded Studio source without a review-visible fixture and pin update.
