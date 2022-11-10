# Conditional Workflows

Many workflows consist of conditional logic to determine which path of execution to take next.
One option to model such a workflow is the use of `If`

## If

The `If` activity provides a `Condition` property that can be set to a flavor of a **workflow expression**.
In addition, the activity provides a `Then` and `Else` property, to which we can assign an activity to execute.

When the condition evaluates to `true`, the `Then` activity is executed, otherwise the `Else` activity is executed. 

