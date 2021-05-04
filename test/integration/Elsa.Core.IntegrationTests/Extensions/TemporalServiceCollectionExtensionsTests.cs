using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Core.IntegrationTests.Autofixture;
using Elsa.Testing.Shared;
using Elsa.Testing.Shared.Helpers;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Elsa.Core.IntegrationTests.Extensions
{
    public class TemporalServiceCollectionExtensionsTests
    {
        [Theory(DisplayName = "Starting a hosted app which uses only AddCommonTemporalActivities should throw InvalidOperationException because of the missing impl"), AutoMoqData]
        public void AddCommonTemporalActivitiesThrowsDuringStartupIfNoTemporalImplementationPresent([WithCommonTemporalActivities] ElsaHostBuilderBuilder hostBuilderBuilder)
        {
            var hostBuilder = hostBuilderBuilder.GetHostBuilder();

            var cancellationSource = new CancellationTokenSource();
            try
            {
                Assert.ThrowsAsync<InvalidOperationException>(() => StartAppAndAllowToRun(hostBuilder, cancellationToken: cancellationSource.Token));
            }
            finally
            {
                // If the test passed then this line doesn't do anything.
                // If the test fails though, then this prevents it from stalling test execution.
                CancelAndSquelchExceptions(cancellationSource);
            }
        }

        [Theory(DisplayName = "Starting a hosted app which uses AddHangfireTemporalActivities should not throw"), AutoMoqData]
        public void AddHangfireTemporalActivitiesDoesNotThrowDuringStartup([WithHangfire] ElsaHostBuilderBuilder hostBuilderBuilder)
        {
            var hostBuilder = hostBuilderBuilder.GetHostBuilder();
            
            var cancellationSource = new CancellationTokenSource();
            try
            {
                StartAppAndAllowToRun(hostBuilder, cancellationToken: cancellationSource.Token);
            }
            catch(Exception e)
            {
                Assert.False(true, $"Test failed because starting the app threw an exception:\n\n{e}");
            }
            finally
            {
                CancelAndSquelchExceptions(cancellationSource);
            }
        }

        [Theory(DisplayName = "Starting a hosted app which uses AddQuartzTemporalActivities should not throw"), AutoMoqData]
        public void AddQuartzTemporalActivitiesDoesNotThrowDuringStartup([WithQuartz] ElsaHostBuilderBuilder hostBuilderBuilder)
        {
            var hostBuilder = hostBuilderBuilder.GetHostBuilder();
            
            var cancellationSource = new CancellationTokenSource();
            try
            {
                StartAppAndAllowToRun(hostBuilder, cancellationToken: cancellationSource.Token);
            }
            catch(Exception e)
            {
                Assert.False(true, $"Test failed because starting the app threw an exception:\n\n{e}");
            }
            finally
            {
                CancelAndSquelchExceptions(cancellationSource);
            }
        }

        /// <summary>
        /// Starts up the specified <paramref name="hostBuilder"/> instance using console lifetime.
        /// The current thread is then blocked for a short while, giving the app a chance to start up and run.
        /// </summary>
        /// <param name="hostBuilder">A host builder</param>
        /// <param name="millisecondsToWait">The duration to block the current thread, default = 1000 (1 second)</param>
        /// <param name="cancellationToken">A cancellation token for killing the app later</param>
        /// <returns>A <see cref="Task"/> for the running app, created from the host builder</returns>
        static Task StartAppAndAllowToRun(IHostBuilder hostBuilder, int millisecondsToWait = 1000, CancellationToken cancellationToken = default)
        {
            var applicationTask = Task.Run(() => hostBuilder.UseConsoleLifetime().Build().Run(), cancellationToken);
            Thread.Sleep(millisecondsToWait);
            return applicationTask;
        }

        /// <summary>
        /// Cancels anything controlled by the cancellation source and ignores/suppresses any exceptions encountered.
        /// </summary>
        /// <param name="cancellationSource">The cancellation source</param>
        static void CancelAndSquelchExceptions(CancellationTokenSource cancellationSource)
        {
            try { cancellationSource.Cancel(); }
            catch(Exception) {
                // This is only ever used as teardown code, which is why we don't care about exceptions.
            }
        }
    }
}