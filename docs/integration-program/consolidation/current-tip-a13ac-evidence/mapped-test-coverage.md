# Mapped source test coverage candidates

Program #8194; packaging proof #8260; import preparation #8287. This is a proposed **post-preparation test overlay** for the disposable, no-remote three-parent rehearsal at Core `a13ac7a412e037280d7a568fd9dd87b06ff8b724`, Extensions `ba8b71d91c15ffe5be4b2c539cf9f712e74af775`, and Studio `20ceaeeed7e671f0c9662003e82063026f2216de`. Its synthetic commit is `372ed7ed3973c90fa581bf6003040900137bc093`. Apply [the patch](../../../../scripts/integration-program/consolidated-build/mapped-source-test-coverage.patch) only **after** `prepare_consolidated_build.py`; its paths and project references target the mapped tree. The patch SHA-256 is `b9343822aef595927da7aea6a316ac62cbc8f82aaaaf267b4f0cc15a1b9edeb6`.

The patch replaces the skipped Slack `CreateChannel` placeholder with an offline activity test that verifies forwarded inputs and captured output. It makes the mapped test helper a `ProjectReference` to the same Core source. It also removes the TODO skip from the Azure Service Bus topic component test. The patch contains no connector production change or provider credential.

From the reviewed tooling checkout, after preparation, apply it to the disposable mapped source with `git -C /path/to/prepared-source apply --check /path/to/reviewed-tooling/scripts/integration-program/consolidated-build/mapped-source-test-coverage.patch`, then the same command without `--check`. Run the test commands below from `/path/to/prepared-source`. This is an overlay step; `prepare_consolidated_build.py` does not apply it automatically.

On 2026-09-24, the following filtered `net10.0` commands passed on the patched mapped source with SDK 10.0.300, `UseProjectReferences=true`, and packing disabled:

```sh
dotnet test test/extensions/modules/slack/Elsa.Slack.Tests/Elsa.Slack.Tests.csproj -f net10.0 -p:UseProjectReferences=true -p:IsPackable=false -p:GeneratePackageOnBuild=false --filter FullyQualifiedName~CreateChannelTests.ExecuteAsync --logger 'console;verbosity=minimal'
dotnet test test/extensions/modules/servicebus/Elsa.ServiceBus.AzureServiceBus.ComponentTests/Elsa.ServiceBus.AzureServiceBus.ComponentTests.csproj -f net10.0 -p:UseProjectReferences=true -p:IsPackable=false -p:GeneratePackageOnBuild=false --filter FullyQualifiedName~WorkflowReceivesMessage_WhenSendingMessageToTopic --logger 'console;verbosity=minimal'
```

Each run reported **1 passed, 0 failed, 0 skipped**. The private raw log SHA-256 values are `f5800e8aac470d4131ea4ba3f46d9cdd3aec537a71e3b26cd42431358c03a78e` (Slack) and `3142b4a2daa01f00080076a72f59f2c4b39d49bff19ac7f29ae84eecc2977f0d` (Service Bus). `git apply --reverse --check` on the patched rehearsal and a reverse/forward roundtrip check on copies of the three affected files passed.

These results are bounded. Slack uses an in-process fake client and a private-cache test seam; it does not call Slack. The Azure test uses a fake Azure SDK sender with Docker-backed PostgreSQL and RabbitMQ fixture services; it does not certify an Azure broker. The Core clustered registry tests remain skipped and their underlying cross-pod transport gap is tracked in reopened #7292. The earlier [pinned source-closure CI run](https://github.com/elsa-workflows/elsa-core/actions/runs/36046189300) is unchanged and remains incomplete. The two replacement tests must be carried into the final history-bearing imported source and rerun in CI; this local overlay does not make #8260 complete, prove all connector behavior, or authorize package publication.
