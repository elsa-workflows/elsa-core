# Workflow Engine Setup
To execute the activities in this project, you must configure the following items in you Elsa workflow engine:

## Bootstrap Activities
It may be necessary to to load the customized activities into Elsa Provider in Program.cs. 

To add an individual activity
```csharp
services.AddElsa(elsa =>
{
    elsa.AddActivity<SendHttpRequestMapper>();
});
```

You may also add all activities in the assembly (except generic activities)
```csharp
services.AddElsa(elsa =>
{
    elsa.AddActivitiesFrom<SendHttpRequestMapper>();
});
```

## Single Bootstrap Method
The individual bootstrap methods below may be invoked using a single method:

```csharp
services.AddElsa(elsa =>
{
    elsa.UseTrimbleActivities(configuration, "TokenProviders");
});
```


## Loading TokenProviders
Add the following to your `program.cs`.

```csharp
// When using WebApplication Builder
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddActivityTokenProviders("TokenProviders")
    .AddCustomExpressions()

// When using other Builders
builder.Services
    .AddActivityTokenProviders(configuration, "TokenProviders")
    .AddCustomExpressions()
```

The following configuration values must be present in your execution environment. You
 may use any value store available to the .Net Core Configuration system, for example, 
 appsettings.json or environment variables.
 These will be loaded by `HttpRequestAuthenticator` activity to obtain tokens for use in workflows.

```bash
TOKENPROVIDERS__SALESFORCEDX__CLIENTSECRET
TOKENPROVIDERS__SALESFORCEDX__ENABLED
TOKENPROVIDERS__SALESFORCEDX__PASSWORD
TOKENPROVIDERS__SALESFORCEDX__USERNAME
TOKENPROVIDERS__SALESFORCEDX__USERTOKEN
TOKENPROVIDERS__TRIMBLEID__ENABLED
TOKENPROVIDERS__TRIMBLEID__CLIENTID
TOKENPROVIDERS__TRIMBLEID__CLIENTSECRET
TOKENPROVIDERS__TRIMBLEID__SCOPES
TOKENPROVIDERS__TRIMBLEID__TOKENURL
TOKENPROVIDERS__VIEWPOINTID__ENABLED
TOKENPROVIDERS__VIEWPOINTID__CLIENTID
TOKENPROVIDERS__VIEWPOINTID__CLIENTSECRET
TOKENPROVIDERS__VIEWPOINTID__SCOPES
TOKENPROVIDERS__VIEWPOINTID__TOKENURL
```

# Extending the Elsa Workflows
The Elsa workflow engine provides UI and programmatic tools for creating and managing workflows. This project extends the Elsa core to provide additional functionality.

## Activities
Trimble Activities extend or modify existing Elsa activities or activity base classes. They generally follow the [Elsa Custom Activity Guidelines](https://v3.elsaworkflows.io/docs/extensibility/custom-activities).

### MustacheStringActivity
This activity allows the use of Mustache templates in string variables. It's used to replace templated variable names with their values.

For example, you may wish to adjust a URL value of  
```csharp
var resultingVariable = new Variable<string>();
var urlShaver = new MustacheStringActivity(httpRequestAction.Url.ToString(), resultingVariable);

// Assign the resultingVariable to the Input property of a downstream activity
```

### HttpRequestAuthenticator<T>
Obtains a token from an HTTP request and saves it to a `Output<string> Token` property that can be used in the Authorization header of an HttpRequest activity. Note that the token is saved as an [EncryptedVariableString](#encryption).

### SendHttpRequestMapper
Overrides the default HttpRequest activity and adds the ability to provide a JSON Path and destination variable to save the result of the HTTP request.

## Variables
Elsa Variables do not actually hold data. They simply add properties like a name to their underlying `MemoryBlockReference`, which references the actual values stored in a `MemoryRegister`. A variable's `Value` property is the default value assign to a memory register, but if that underlying memory register value were to change for example, the Variable's Value property would no loner match.

Multiple memory registers exist in a hierarchy of "execution contexts" that range from the global WorkflowExecutionContext, which contains various ActivityExecutionContexts, which contains ExpressionExecutionContexts. When a variable is referenced in one of these contexts, the memory block corresponding to the Variable's ID is searched first in the immediate context, and if not found, up the parent hierarchy until the top-level workflow execution context is reached.

```csharp
// searches for the value pointed to by urlVariable starting at
// context and working up to the global workflow context 
request.URI = urlVariable.Get(context)
```


The variable values defined in Service Registry are scoped to the entire workflow execution context so they are globally available to all activities. Since the ID of a variable is internally assigned by the workflow builder, we use variable names to reference them in expressions. These names are not guaranteed to be unique, so we use two conventions to avoid collisions:

1. The [Data Variable Syntax](https://developer-docs.development.vpplatdev.trimble.com/designs/provisioning-design/detailed-design/service-registration/#data-variable-syntax) is used by Capability Definitions in Service Registry. When a data scchema value is defined it is translated into a variable name matching this convention.
1. When the Workflow Builder creates variables not defined by Data Variable Syntax such as passing a token from one request to another, it assigns a variable name scoped to the Capability definition or operation to ensure global uniqueness. This happens automatically using the internal ScopedName function and does not require any input from the Activity consumer.
```csharp
public static string ScopedName(string nodeName, string operation)
{
    return $"{nodeName}:{operation}:{Guid.NewGuid().ToString("N")}";
}
```

## Encryption
Serialized workflow definition files or executing workflows whose state is saved may contain sensitive information. Examples include credentials used to obtain tokens for HTTP authorization.

Two variable container classes are provided to help manage these data:

* **EncryptedVariableDictionary** - use in place of `Variable<Dictionary<string, string >>`
* **EncryptedVariableString** - use in place of `Variable<string>`

The default `Value` properties of these variables are encrypted when set and decrypted when read using an encryption key provided as an environment variable in the executing environment. This key must be shared between the workflow definition and the executing environment.

Activities must use these classes in their Input, Output, and Result properties when dealing with sensitive information. This may require overriding default Elsa Activities.



Default Variable "Values" are saved once to a MemoryBlock. Encrypted Variables encrypt raw values before saving to the MemoryBlock.

```csharp
public override MemoryBlock Declare()
{
    // This is invoked by the Elsa framework to set the default MemoryBlock value
    // This MemoryBlock value may be updated by other activities
    if (!IsEncrypted && Value is Dictionary<string, string> dict)
    {
        Value = dict.Encrypt(Name);
        IsEncrypted = true;
    }

    return new(Value, new VariableBlockMetadata(this, StorageDriverType, false));
}

```

## Resources
* [Elsa Core source code](https://github.com/elsa-workflows/elsa-core)