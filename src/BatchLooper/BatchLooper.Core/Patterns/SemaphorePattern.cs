using BatchLooper.Core.Interfaces.Patterns;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace BatchLooper.Core.Patterns
{
    /// <summary>
    /// Provides a pattern for handling concurrency using SemaphoreSlim.
    /// </summary>
    [DebuggerDisplay("SemaphorePattern: RemainingTheads = {_semaphore.CurrentCount}")]
    public class SemaphorePattern : ISemaphorePattern
    {
        #region Constants

        #endregion

        #region Variables

        private readonly SemaphoreSlim _semaphore;
        private bool disposedValue;

        #endregion

        #region Initialzers

        /// <summary>
        /// Initializes a new instance of the <see cref="SemaphorePattern"/> class with the specified initial and maximum count.
        /// </summary>
        /// <param name="initialCount">The initial number of requests that can be granted concurrently.</param>
        /// <param name="maxCount">The maximum number of requests that can be granted concurrently.</param>
        public SemaphorePattern(int initialCount = 1, int maxCount = 1) :
            this(new SemaphoreSlim(initialCount, maxCount))
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SemaphorePattern"/> class with the specified semaphore.
        /// </summary>
        /// <param name="semaphore">The semaphore to use for concurrency control.</param>
        public SemaphorePattern([NotNull] SemaphoreSlim semaphore)
        {
            ArgumentNullException.ThrowIfNull(semaphore);
            _semaphore = semaphore;
        }

        #endregion

        #region Functions and Routines

        #region Publics

        ConcurrentQueue<Exception> ISemaphorePattern.HandleConcurencyMultithread(TimeSpan timeout, Action action)
        {
            var result = new ConcurrentQueue<Exception>();

            if (!_semaphore.Wait(timeout))
                return result;

            try
            {
                action();
            }
            catch (Exception ex)
            {
                result.Enqueue(ex);
            }
            finally
            {
                _semaphore.Release();
            }

            return result;
        }

        (TResult Result, ConcurrentQueue<Exception> Errors) ISemaphorePattern.HandleConcurencyMultithread<TResult>(TimeSpan timeout, Func<TResult> action)
        {
            TResult result = default!;
            var errors = new ConcurrentQueue<Exception>();

            if (!_semaphore.Wait(timeout))
                return (result, errors);

            try
            {
                result = action();
            }
            catch (Exception ex)
            {
                errors.Enqueue(ex);
            }
            finally
            {
                _semaphore.Release();
            }

            return (result, errors);
        }

        async Task<ConcurrentQueue<Exception>> ISemaphorePattern.HandleConcurencyMultithreadAsync(TimeSpan timeout, Func<Task> action, CancellationToken cancellationToken)
        {

            var result = new ConcurrentQueue<Exception>();

            if (!await _semaphore.WaitAsync(timeout, cancellationToken))
                return result;

            try
            {
                await action();
            }
            catch (Exception ex)
            {
                result.Enqueue(ex);
            }
            finally
            {
                _semaphore.Release();
            }

            return result;
        }

        async Task<(TResult Result, ConcurrentQueue<Exception> Errors)> ISemaphorePattern.HandleConcurencyMultithreadAsync<TResult>(TimeSpan timeout, Func<Task<TResult>> action, CancellationToken cancellationToken)
        {
            TResult result = default!;
            var errors = new ConcurrentQueue<Exception>();

            if (!await _semaphore.WaitAsync(timeout, cancellationToken))
                return (result, errors);

            try
            {
                result = await action();
            }
            catch (Exception ex)
            {
                errors.Enqueue(ex);
            }
            finally
            {
                _semaphore.Release();
            }

            return (result, errors);
        }

        /// <summary>
        /// Releases all resources used by the <see cref="SemaphorePattern"/>.
        /// </summary>
        void IDisposable.Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Privates

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="SemaphorePattern"/> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _semaphore?.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        #endregion

        #endregion
    }
}
