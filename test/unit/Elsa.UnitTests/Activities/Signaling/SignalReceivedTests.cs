using System.Threading.Tasks;
using Elsa.Services.Models;
using Elsa.Activities.Signaling.Models;
using Xunit;
using Elsa.ActivityResults;
using System.Linq;
using Elsa.Testing.Shared;

namespace Elsa.Activities.Signaling
{
    public class SignalReceivedTests
    {
        [Theory(DisplayName = "The CanExecuteAsync method should return a task of false if the signal name does not match the triggered signal"), AutoMoqData]
        public async Task CanExecuteAsyncShouldReturnATaskOfFalseIfTheSignalNameDoesNotMatch(SignalReceived sut,
                                                                                             Signal triggeredSignal)
        {
            var context = new ActivityExecutionContext(default, default, default, triggeredSignal, default, default);
            triggeredSignal.SignalName = "Yes";
            sut.Signal = "No";

            var result = await sut.CanExecuteAsync(context);

            Assert.False(result);
        }

        [Theory(DisplayName = "The CanExecuteAsync method should return a task of true if the signal name matches the triggered signal"), AutoMoqData]
        public async Task CanExecuteAsyncShouldReturnATaskOfTrueIfTheSignalNameMatchs(SignalReceived sut,
                                                                                      string signalName,
                                                                                      Signal triggeredSignal)
        {
            var context = new ActivityExecutionContext(default, default, default, triggeredSignal, default, default);
            triggeredSignal.SignalName = signalName;
            sut.Signal = signalName;

            var result = await sut.CanExecuteAsync(context);

            Assert.True(result);
        }

        [Theory(DisplayName = "The CanExecuteAsync method should return a task of false if the input from the context is not a triggered signal"), AutoMoqData]
        public async Task CanExecuteAsyncShouldReturnATaskOfFalseIfTheInputIsNotATriggeredSignal(SignalReceived sut,
                                                                                                 string signalName,
                                                                                                 object notASignal)
        {
            var context = new ActivityExecutionContext(default, default, default, notASignal, default, default);
            sut.Signal = signalName;

            var result = await sut.CanExecuteAsync(context);

            Assert.False(result);
        }

        [Theory(DisplayName = "The ResumeAsync method should return a combined 'Done' Outcome & Output result"), AutoMoqData]
        public async Task ResumeAsyncShouldReturnCombinedDoneAndOutputResult(SignalReceived sut, Signal triggeredSignal, object signalInput)
        {
            var context = new ActivityExecutionContext(default, default, default, triggeredSignal, default, default);
            triggeredSignal.Input = signalInput;
            
            var result = await sut.ResumeAsync(context);

            Assert.True((result is CombinedResult combinedResult)
                        && combinedResult.Results.Any(res => res is OutcomeResult outcome && outcome.Outcomes.Any(o => o == OutcomeNames.Done))
                        && combinedResult.Results.Any(res => res is OutputResult),
                        $"The result is {nameof(CombinedResult)} which contains both a {nameof(OutcomeResult)} (of \"Done\") and a {nameof(OutputResult)}.");
        }

        [Theory(DisplayName = "The ResumeAsync method should include the original signal input within the Output result"), AutoMoqData]
        public async Task ResumeAsyncShouldReturnOriginalSignalInputWithinDoneResult(SignalReceived sut, Signal triggeredSignal, object signalInput)
        {
            var context = new ActivityExecutionContext(default, default, default, triggeredSignal, default, default);
            triggeredSignal.Input = signalInput;
            
            var result = await sut.ResumeAsync(context);
            var resultOutput = ((CombinedResult) result).Results
                .OfType<OutputResult>()
                ?.FirstOrDefault()
                ?.Output;

            Assert.Same(signalInput, resultOutput);
        }
    }
}