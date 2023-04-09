// using Elsa.Hangfire.Jobs;
// using Elsa.Jobs.Contracts;
// using Hangfire;
// using Hangfire.States;
// using HangfireJob = Hangfire.Common.Job;
//
// namespace Elsa.Hangfire.Services;
//
// public class HangfireJobQueue : IJobQueue
// {
//     private readonly IBackgroundJobClient _backgroundJobClient;
//
//     public HangfireJobQueue(IBackgroundJobClient backgroundJobClient)
//     {
//         _backgroundJobClient = backgroundJobClient;
//     }
//
//     public Task SubmitJobAsync(IJob job, string? queueName = default, CancellationToken cancellationToken = default)
//     {
//         var hangfireJob = HangfireJob.FromExpression<RunElsaJob>(x => x.RunAsync(job, CancellationToken.None));
//         _backgroundJobClient.Create(hangfireJob, new EnqueuedState(queueName ?? "default"));
//
//         return Task.CompletedTask;
//     }
// }