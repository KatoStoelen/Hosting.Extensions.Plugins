using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Hosting.Extensions.Plugins.Internal
{
    internal static class TaskExecutor
    {
        private const int MaxAttempts = 3;
        private static readonly TimeSpan s_attemptTimeIncrease = TimeSpan.FromMilliseconds(200);
        private const int MaxDegreeOfParallelism = 3;

        public static Task Parallel<TItem>(
            IEnumerable<TItem> items,
            Action<TItem> processItem,
            Action<TItem, Exception> onError,
            CancellationToken cancellationToken = default)
        {
            return Parallel(
                items,
                item =>
                {
                    processItem(item);

                    return Task.CompletedTask;
                },
                onError,
                cancellationToken);
        }

        public static Task Parallel<TItem>(
            IEnumerable<TItem> items,
            Func<TItem, Task> processItem,
            Action<TItem, Exception> onError,
            CancellationToken cancellationToken = default)
        {
            var workerBlock = new ActionBlock<TItem>(
                async item =>
                {
                    var attempts = 0;

                    try
                    {
                        await processItem(item).ConfigureAwait(false);
                    }
                    catch (Exception) when (attempts < MaxAttempts)
                    {
                        attempts++;

                        await Task.Delay(s_attemptTimeIncrease * attempts, cancellationToken)
                            .ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        onError(item, e);
                    }
                },
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = MaxDegreeOfParallelism,
                    CancellationToken = cancellationToken
                });

            foreach (var item in items)
            {
                _ = workerBlock.Post(item);
            }

            if (workerBlock.InputCount == 0)
            {
                return Task.CompletedTask;
            }

            workerBlock.Complete();

            return workerBlock.Completion;
        }

        public static async Task Parallel<TItem>(
            IAsyncEnumerable<TItem> items,
            Func<TItem, Task> processItem,
            Action<TItem, Exception> onError,
            CancellationToken cancellationToken = default)
        {
            var loadedItems = new List<TItem>();

            await foreach (var item in items.ConfigureAwait(false))
            {
                loadedItems.Add(item);
            }

            await Parallel(loadedItems, processItem, onError, cancellationToken)
                .ConfigureAwait(false);
        }

        public static void Execute(
            Func<Task> operation,
            Action<Exception> onError,
            CancellationToken cancellationToken = default)
        {
            _ = Task
                .Run(
                    async () =>
                    {
                        var attempts = 0;

                        try
                        {
                            await operation().ConfigureAwait(false);
                        }
                        catch (Exception) when (attempts < MaxAttempts)
                        {
                            attempts++;

                            await Task.Delay(s_attemptTimeIncrease * attempts, cancellationToken)
                                .ConfigureAwait(false);
                        }
                    },
                    cancellationToken)
                .ContinueWith(task => HandleTaskResult(task, onError), cancellationToken);
        }

        public static void ExecuteAsync(
            Action operation,
            Action<Exception> onError,
            CancellationToken cancellationToken = default)
        {
            _ = Task
                .Run(
                    async () =>
                    {
                        var attempts = 0;

                        try
                        {
                            operation();
                        }
                        catch (Exception) when (attempts < MaxAttempts)
                        {
                            attempts++;

                            await Task.Delay(s_attemptTimeIncrease * attempts, cancellationToken)
                                .ConfigureAwait(false);
                        }
                    },
                    cancellationToken)
                .ContinueWith(task => HandleTaskResult(task, onError), cancellationToken);
        }

        private static void HandleTaskResult(Task task, Action<Exception> onError)
        {
            if (task.IsFaulted)
            {
                var exception = task.Exception!.InnerExceptions.Count == 1
                    ? task.Exception.InnerExceptions.Single()
                    : task.Exception;

                onError(exception);
            }
        }
    }
}