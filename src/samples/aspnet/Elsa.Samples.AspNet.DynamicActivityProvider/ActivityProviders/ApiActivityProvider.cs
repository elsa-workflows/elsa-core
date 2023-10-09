using System.Text;
using System.Text.Json;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Http;
using Elsa.Samples.AspNet.DynamicActivityProvider.Models;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Management;
using Humanizer;

namespace Elsa.Samples.AspNet.DynamicActivityProvider.ActivityProviders;

/// <summary>
/// Provides activities based on API descriptions (see /Data/apis.json).
/// </summary>
public class ApiActivityProvider : IActivityProvider
{
    private readonly IActivityFactory _activityFactory;
    private readonly IActivityDescriber _activityDescriber;

    public ApiActivityProvider(IActivityFactory activityFactory, IActivityDescriber activityDescriber)
    {
        _activityFactory = activityFactory;
        _activityDescriber = activityDescriber;
    }

    public async ValueTask<IEnumerable<ActivityDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default)
    {
        // Read JSON file.
        var json = await File.ReadAllTextAsync("Data/apis.json", cancellationToken);

        // Parse JSON into a list of API definitions.
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var apisDefinitions = JsonSerializer.Deserialize<ApiDefinition[]>(json, options)!;

        // Create activity descriptors from API definitions.
        var activities = ((await Task.WhenAll(apisDefinitions.Select(async x => await CreateActivityDescriptors(x, cancellationToken)))).SelectMany(x => x)).ToList();

        // Return activity descriptors.
        return activities;
    }

    /// <summary>
    /// Creates activity descriptors from an API definition's endpoints.
    /// </summary>
    /// <param name="api">The API definition containing the endpoints.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>A list of activity descriptors.</returns>
    private async Task<IEnumerable<ActivityDescriptor>> CreateActivityDescriptors(ApiDefinition api, CancellationToken cancellationToken = default)
    {
        var descriptorTasks = api.Endpoints.Select(async endpoint => await CreateActivityDescriptorFromEndpointAsync(api, endpoint, cancellationToken)).ToList();

        return await Task.WhenAll(descriptorTasks);
    }

    private async Task<ActivityDescriptor> CreateActivityDescriptorFromEndpointAsync(ApiDefinition api, ApiEndpointDefinition endpoint, CancellationToken cancellationToken = default)
    {
        // Create a fully qualified type name for the activity ("{namespace}.{apiName}.{endpointName}").
        var fullTypeName = $"Demo.{api.Name}.{endpoint.Name}";

        // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
        // Justification: the JSON input may not have a parameters property.
        var parameters = endpoint.Parameters ?? new List<ApiParameterDefinition>();

        // Create inputs from endpoint parameters.
        var inputs = parameters.Select(CreateInputDescriptor).ToList();

        // Create inputs for the URL and method properties of the SendHttpRequest activity, which will be used as the implementation for executing instances of this activity.
        // They are set in the constructor, and should not be visible to the user.
        var urlInputDescriptor = await _activityDescriber.DescribeInputPropertyAsync<SendHttpRequest, Input<Uri?>>(x => x.Url, cancellationToken);
        var urlInputReferenceId = Guid.NewGuid().ToString();
        var methodInputDescriptor = await _activityDescriber.DescribeInputPropertyAsync<SendHttpRequest, Input<string>>(x => x.Method, cancellationToken);
        var methodInputReferenceId = Guid.NewGuid().ToString();

        urlInputDescriptor.IsBrowsable = false;
        urlInputDescriptor.ValueGetter = activity =>
        {
            var input = new Input<Uri>(context =>
            {
                // Replace path parameters with input values.
                var pathBuilder = new StringBuilder(endpoint.Path);
                
                foreach (var parameter in parameters)
                {
                    var inputDescriptor = inputs.FirstOrDefault(x => x.Name == parameter.Name);
                    var inputValue = inputDescriptor?.ValueGetter(activity) is Input inputValueInput ? context.Get(inputValueInput.MemoryBlockReference()) : default;
                    pathBuilder.Replace("{" + parameter.Name + "}", inputValue?.ToString());
                }
                
                return new(new Uri(api.BaseUrl, pathBuilder.ToString()));
            }, urlInputReferenceId);
            return input;
        };

        methodInputDescriptor.IsBrowsable = false;
        methodInputDescriptor.ValueGetter = _ => new Input<string>(endpoint.Method, methodInputReferenceId);

        inputs.Add(urlInputDescriptor);
        inputs.Add(methodInputDescriptor);

        // Create an output descriptor based on the send HTTP request activity's ParsedContent property.
        var outputDescriptor = await _activityDescriber.DescribeOutputProperty<SendHttpRequest, Output<object?>>(x => x.ParsedContent, cancellationToken);

        return new ActivityDescriptor
        {
            Kind = ActivityKind.Task,
            Category = "Demo",
            Description = endpoint.Description,
            Name = endpoint.Name,
            TypeName = fullTypeName,
            Namespace = $"Api.{api.Name}",
            DisplayName = endpoint.Name.Humanize(),
            Inputs = inputs,
            Outputs = new[] { outputDescriptor },
            Constructor = context =>
            {
                // The constructor is called when an activity instance of this type is requested.
                
                // Create the activity instance.
                var activity = _activityFactory.Create<SendHttpRequest>(context);
                
                // Customize the activity type name.
                activity.Type = fullTypeName;
                
                // Configure the activity's URL and method properties.
                activity.Url = new Input<Uri?>(new ReadReference(urlInputReferenceId));
                activity.Method = new(new ReadReference(methodInputReferenceId));

                return activity;
            }
        };
    }

    /// <summary>
    /// Creates an input descriptor from an API parameter definition.
    /// </summary>
    /// <param name="parameter">The API parameter definition.</param>
    private static InputDescriptor CreateInputDescriptor(ApiParameterDefinition parameter)
    {
        var inputDescriptor = new InputDescriptor
        {
            Description = parameter.Description,
            DefaultValue = parameter.DefaultValue,
            Type = parameter.Type == "number" ? typeof(int) : typeof(string),
            Name = parameter.Name,
            DisplayName = parameter.Name.Humanize(),
            IsSynthetic = true, // This is a synthetic property, i.e. it is not part of the activity's .NET type.
            IsWrapped = true, // This property is wrapped within an Input<T> object.
            UIHint = InputUIHints.SingleLine,
            ValueGetter = activity => activity.SyntheticProperties.GetValueOrDefault(parameter.Name),
            ValueSetter = (activity, value) => activity.SyntheticProperties[parameter.Name] = value!,
        };
        return inputDescriptor;
    }
}