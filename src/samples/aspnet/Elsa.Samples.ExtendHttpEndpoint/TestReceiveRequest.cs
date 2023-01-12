using Elsa.Activities.Http;
using Elsa.Activities.Http.Bookmarks;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Events;
using Elsa.Services;
using Elsa.Services.Models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Samples.ExtendHttpEndpoint
{
    /**
     * Demonstrate how to make your own HTTP Endpoint like activity by extending HttpEndpoint class.
     * 
     * In you derived activity you can set default values for existing HttpEndpoint input parameters and you can add your own input and output parameters.
     * In OnExecuteAsync you make use of new input parameters and, after invoking base OnExecuteAsync, fill new output parameters (and clear ones in base you don't need).
     * In the INotificationHandler<DescribingActivityType> you can hide input and output parameters you don't want to be seen.
     * You need to implement your BookmarkProvider<HttpEndpointBookmark, TestReceiveRequest> , make sure to use nameof(HttpEndpoint)
     * as second parameter to result because HttpEndpointMiddleware looks up bookmarks with that activity type. 
     * In bookmark you can hardcode path and/or methods too.
     */
    public class CustomizeTestReceiveRequest : INotificationHandler<DescribingActivityType>
    {
        // hiding some properties from UI
        public Task Handle(DescribingActivityType notification, CancellationToken cancellationToken)
        {
            var descriptor = notification.ActivityDescriptor;

            if (descriptor.Type != nameof(TestReceiveRequest))
                return Task.CompletedTask;

            var hiddenInputProperties = descriptor.InputProperties.Where(x => x.Name != "Path");

            foreach (var hiddenProperty in hiddenInputProperties)
                hiddenProperty.IsBrowsable = false;

            var hiddenOutputProperties = descriptor.OutputProperties.Where(x => x.Name != "Body");

            foreach (var hiddenProperty in hiddenOutputProperties)
                hiddenProperty.IsBrowsable = false;

            return Task.CompletedTask;
        }
    }

    [Trigger(
        Category = "Test",
        DisplayName = "Receive Request",
        Description = "Wait for an test request.",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class TestReceiveRequest : HttpEndpoint
    {
        // here to specify default value
        [ActivityInput(DefaultValue = true)]
        public new bool ReadContent { get; set; } = true;

        // here to specify default value
        [ActivityInput(DefaultValue = typeof(Dictionary<string, string>))]
        public new Type? TargetType { get; set; } = typeof(Dictionary<string, string>);

        [ActivityOutput(Hint = "The body of the received HTTP request.")]
        public object? Body { get; set; }

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            var baseRes = await base.OnExecuteAsync(context);

            if (Output != null)
            {
                Body = Output.Body;
                Output = null;
            }

            return baseRes;
        }
    }

    public class TestReceiveRequestBookmarkkProvider : BookmarkProvider<HttpEndpointBookmark, TestReceiveRequest>
    {
        public override async ValueTask<IEnumerable<BookmarkResult>> GetBookmarksAsync(BookmarkProviderContext<TestReceiveRequest> context, CancellationToken cancellationToken)
        {
            var path = ToLower((await context.ReadActivityPropertyAsync(x => x.Path, cancellationToken))!);
            var methods = new[] { "post" };

            BookmarkResult CreateBookmark(string method) => Result(new(path ?? "", method), nameof(HttpEndpoint));
            return methods.Select(CreateBookmark);
        }

        private static string? ToLower(string? s) => s?.ToLowerInvariant();
    }
}
