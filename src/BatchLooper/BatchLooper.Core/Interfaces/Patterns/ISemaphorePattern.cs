using System.Collections.Concurrent;

namespace BatchLooper.Core.Interfaces.Patterns
{
    public interface ISemaphorePattern : IDisposable
    {
        /// <summary>
        /// Handles concurrency in a multithreaded environment with a timeout and an action.
        /// </summary>
        /// <param name="timeout">The maximum time to wait for the semaphore.</param>
        /// <param name="action">The action to execute within the semaphore.</param>
        /// <returns>A queue of exceptions that occurred during the execution of the action.</returns>
        ConcurrentQueue<Exception> HandleConcurencyMultithread(TimeSpan timeout, Action action);

        /// <summary>
        /// Handles concurrency in a multithreaded environment with a timeout and a function that returns a result.
        /// </summary>
        /// <typeparam name="TResult">The type of the result returned by the function.</typeparam>
        /// <param name="timeout">The maximum time to wait for the semaphore.</param>
        /// <param name="action">The function to execute within the semaphore.</param>
        /// <returns>A tuple containing the result of the function and a queue of exceptions that occurred during the execution of the function.</returns>
        (TResult Result, ConcurrentQueue<Exception> Errors) HandleConcurencyMultithread<TResult>(TimeSpan timeout, Func<TResult> action);

        /// <summary>
        /// Asynchronously handles concurrency in a multithreaded environment with a timeout and an asynchronous action.
        /// </summary>
        /// <param name="timeout">The maximum time to wait for the semaphore.</param>
        /// <param name="action">The asynchronous action to execute within the semaphore.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a queue of exceptions that occurred during the execution of the action.</returns>
        Task<ConcurrentQueue<Exception>> HandleConcurencyMultithreadAsync(TimeSpan timeout, Func<Task> action, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously handles concurrency in a multithreaded environment with a timeout and an asynchronous function that returns a result.
        /// </summary>
        /// <typeparam name="TResult">The type of the result returned by the function.</typeparam>
        /// <param name="timeout">The maximum time to wait for the semaphore.</param>
        /// <param name="action">The asynchronous function to execute within the semaphore.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a tuple with the result of the function and a queue of exceptions that occurred during the execution of the function.</returns>
        Task<(TResult Result, ConcurrentQueue<Exception> Errors)> HandleConcurencyMultithreadAsync<TResult>(TimeSpan timeout, Func<Task<TResult>> action, CancellationToken cancellationToken = default);
    }
}
