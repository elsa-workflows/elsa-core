using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Handlers.Notifications;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Notifications;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Elsa.Workflows.Management.UnitTests.Handlers.Notifications;

public class ValidateOutputConvertersTests
{
    [Test]
    public async Task HandleAsync_WhenConverterHasNoDestination_AddsValidationError()
    {
        var fixture = new Fixture();
        fixture.DestinationResolver.Resolve(fixture.Graph, fixture.Node, fixture.Activity.Result).Returns((OutputBindingDestination?)null);

        var errors = await fixture.ValidateAsync();

        var error = await Assert.That(errors).HasSingleItem();
        await Assert.That(error.ActivityId).IsEqualTo(fixture.Activity.Id);
        await Assert.That(error.Message).Contains("only be configured").WithComparison(StringComparison.CurrentCulture);
    }

    [Test]
    public async Task HandleAsync_WhenConverterIsUnknown_AddsValidationError()
    {
        var fixture = new Fixture();

        var errors = await fixture.ValidateAsync();

        await Assert.That(errors).Contains(x => x.Message.Contains("is not registered", StringComparison.CurrentCulture));
    }

    [Test]
    public async Task HandleAsync_WhenBindingAndSettingsAreValid_AddsNoValidationErrors()
    {
        var fixture = new Fixture();
        var descriptor = new OutputConverterDescriptor("test", typeof(int), typeof(string), "Test");
        fixture.Registry.FindRegistration("test").Returns(new OutputConverterRegistration(descriptor, "test", ServiceLifetime.Singleton));
        fixture.SettingsValidator.Validate(descriptor, Arg.Any<IOutputConverter>(), null).Returns([]);

        var errors = await fixture.ValidateAsync();

        await Assert.That(errors).IsEmpty();
    }

    private sealed class Fixture
    {
        public Fixture()
        {
            Activity = new TestActivity
            {
                Id = "activity-1",
                Result = new()
                {
                    Converter = new("test")
                }
            };
            Node = new(Activity, "Body");
            var workflow = new Workflow();
            Graph = new(workflow, Node, [Node]);
            GraphBuilder.BuildAsync(workflow, Arg.Any<CancellationToken>()).Returns(Graph);
            ActivityRegistry.FindAsync(Activity.Type, Activity.Version).Returns(new ActivityDescriptor
            {
                Outputs =
                [
                    new(
                        nameof(TestActivity.Result),
                        "Result",
                        typeof(int),
                        activity => ((TestActivity)activity).Result,
                        (activity, value) => ((TestActivity)activity).Result = (Output<int>)value!)
                ]
            });
            DestinationResolver.Resolve(Graph, Node, Activity.Result).Returns(
                new OutputBindingDestination("workflow-result", typeof(string), true, OutputBindingDestinationKind.WorkflowOutput));

            var services = new ServiceCollection();
            services.AddKeyedSingleton<IOutputConverter, TestConverter>("test");
            ServiceProvider = services.BuildServiceProvider();
        }

        public TestActivity Activity { get; }
        public ActivityNode Node { get; }
        public WorkflowGraph Graph { get; }
        public IWorkflowGraphBuilder GraphBuilder { get; } = Substitute.For<IWorkflowGraphBuilder>();
        public IActivityRegistryLookupService ActivityRegistry { get; } = Substitute.For<IActivityRegistryLookupService>();
        public IOutputConverterRegistry Registry { get; } = Substitute.For<IOutputConverterRegistry>();
        public IOutputBindingDestinationResolver DestinationResolver { get; } = Substitute.For<IOutputBindingDestinationResolver>();
        public IOutputConverterSettingsValidator SettingsValidator { get; } = Substitute.For<IOutputConverterSettingsValidator>();
        public ServiceProvider ServiceProvider { get; }

        public async Task<ICollection<WorkflowValidationError>> ValidateAsync()
        {
            var errors = new List<WorkflowValidationError>();
            var notification = new WorkflowDefinitionValidating(Graph.Workflow, errors);
            var handler = new ValidateOutputConverters(
                GraphBuilder,
                ActivityRegistry,
                Registry,
                DestinationResolver,
                SettingsValidator,
                ServiceProvider);

            await handler.HandleAsync(notification, CancellationToken.None);
            return errors;
        }
    }

    private sealed class TestActivity : CodeActivity
    {
        public Output<int> Result { get; set; } = new();

        protected override void Execute(ActivityExecutionContext context)
        {
        }
    }

    private sealed class TestConverter : IOutputConverter
    {
        public object Convert(OutputConversionContext context) => context.Value.ToString()!;
    }
}
