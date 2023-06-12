# Looping Workflows

This sample demonstrates three activities that can be used to model looping constructs.

## For

The `For` activity executes a child activity for each step between a start and end value, similar to the following C# code:

```csharp
for(var i = 0; i < 10; i++)
{
   // Logic to execute for each iteration.
}
```

## ForEach

The `ForEach` activity executes a child activity for each item in a list, similar to the following C# code:

```csharp
foreach(var item in items)
{
   // Logic to execute for each iteration.
}
```

## While

The `While` activity executes a child activity for each iteration for as long as a certain condition evaluates to `true`, similar to the following C# code:

```csharp
while(SomeConditionIstrue)
{
   // Logic to execute for each iteration.
}
```