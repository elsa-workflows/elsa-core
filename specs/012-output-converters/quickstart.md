# Quickstart: Verify an Output Converter

## Register a converter

Follow the complete implementation and registration example in [Output Converters](../../doc/wiki/output-converters.md). Create a deterministic converter that turns a sample object into text, declare source/result types and a settings schema, and register it with a stable ID through the public service-registration extension.

## Author a workflow

1. Add a string variable or workflow output.
2. Bind an activity's sample-object output to that destination.
3. Select the registered converter.
4. Configure its formatting setting.
5. Save and reopen the workflow.

## Execute

Run the workflow and verify:

- The bound variable/workflow output contains the converted text.
- The activity output register and journal contain the original sample object.
- The converter resolves from the workflow scope.
- Replaying the same assignment produces the same Bound Value.

## Exercise failures

Verify definition rejection for an unknown ID, incompatible destination, and invalid settings. Then remove the registration after publishing and execute the definition:

- The activity faults through normal fault handling.
- The destination remains unchanged.
- The native Activity Output remains available.
- Persisted exception metadata identifies the converter, activity, output, destination, and resolution stage.
- The default message contains neither the native value nor raw settings.

## Verify backward compatibility

Execute and serialize a workflow with no converter configuration:

- JSON omits the converter object.
- Existing output assignment tests remain unchanged.
- Instrumentation observes no converter registry lookup.
