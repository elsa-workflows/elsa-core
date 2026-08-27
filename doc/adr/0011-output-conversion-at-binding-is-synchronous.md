# Output conversion occurs synchronously at the binding boundary

An Activity Output remains the native value defined by its activity and is the value exposed by output registers, journals, APIs, and diagnostics. An explicitly configured Output Converter synchronously transforms only the Bound Value delivered to a variable or workflow output; null inputs bypass conversion, and a converter-produced null is valid only for a nullable destination. Conversion and result validation complete before the destination is written, so a failure leaves it unchanged while retaining the native Activity Output for diagnostics.

Async conversion, activity-input conversion, expression coercion, converter chains, and converter configuration without a destination are outside this boundary. A binding without converter configuration follows the existing assignment path without converter-related lookup, validation, processing, or allocation.
