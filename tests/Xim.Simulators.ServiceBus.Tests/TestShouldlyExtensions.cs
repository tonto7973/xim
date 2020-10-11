using System;
using System.Threading.Tasks;
using Shouldly;

namespace Xim.Simulators.ServiceBus.Tests
{
    internal static class TestShouldlyExtensions
    {
        public static async Task<OperationCanceledException> ShouldCancelOperationAsync(this Task task, TimeSpan? timeout = null)
        {
            TimeSpan finishIn = timeout ?? TimeSpan.FromSeconds(10);
            Task finishedTask = null;
            Task<Task> mainTask = task.ContinueWith(t => finishedTask = t);
            var waitTask = Task.Delay(finishIn);

            await Task.WhenAny(mainTask, waitTask).ConfigureAwait(false);

            if (finishedTask == null)
            {
                throw new ShouldAssertException($"Should complete in {finishIn} but did not");
            }

            OperationCanceledException operationCancelledException = await GetCanceledExceptionAsync(finishedTask).ConfigureAwait(false);

            finishedTask.ShouldSatisfyAllConditions(
                () => operationCancelledException.ShouldNotBeNull(),
                () => finishedTask.ShouldNotBeNull(),
                () => finishedTask.IsCanceled.ShouldBeTrue()
            );
            return operationCancelledException;
        }

        private static async Task<OperationCanceledException> GetCanceledExceptionAsync(Task finishedTask)
        {
            try
            {
                await finishedTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException e)
            {
                return e;
            }

            return null;
        }
    }
}
