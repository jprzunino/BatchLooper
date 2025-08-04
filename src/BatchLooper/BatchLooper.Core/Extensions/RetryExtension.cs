using System.Collections.Concurrent;

namespace BatchLooper.Core.Extensions
{
    public static class RetryExtension
    {
        #region Publics

        #region Async

        public static async Task<ConcurrentQueue<Exception>> RetryAsync(this Func<Task> action, int maxTries, Action<Exception, int>? actionInError = null, int waitTime = 5)
        {
            var exceptions = new ConcurrentQueue<Exception>();

            await action.RetryInternalAsync(maxTries, exception: exceptions, actionInError: actionInError, waitTime: waitTime);

            return exceptions;
        }

        public static async Task<ConcurrentQueue<Exception>> RetryAsync(this Func<Task> action, int maxTries, TimeSpan waitTime, Action<Exception, int>? actionInError = null)
        {
            var exceptions = new ConcurrentQueue<Exception>();

            await action.RetryInternalAsync(maxTries, exception: exceptions, actionInError: actionInError, waitTime: waitTime);

            return exceptions;
        }

        public static async Task<(TResult? Result, ConcurrentQueue<Exception> Exceptions)> RetryAsync<TResult>(this Func<int, Task<TResult>> action, int maxTries, Action<Exception, int>? actionInError = null, int waitTime = 5)
        {
            var exceptions = new ConcurrentQueue<Exception>();

            TResult? result = await action.RetryInternalAsync(maxTries, exception: exceptions, actionInError: actionInError, waitTime: waitTime);

            return (result, exceptions);
        }

        public static async Task<(TResult? Result, ConcurrentQueue<Exception> Exceptions)> RetryAsync<TResult>(this Func<int, Task<TResult>> action, int maxTries, TimeSpan waitTime, Action<Exception, int>? actionInError = null)
        {
            var exceptions = new ConcurrentQueue<Exception>();

            TResult? result = await action.RetryInternalAsync(maxTries, exception: exceptions, actionInError: actionInError, waitTime: waitTime);

            return (result, exceptions);
        }

        #endregion

        #region Sync

        public static ConcurrentQueue<Exception> Retry(this Action action, int maxTries, Action<Exception, int>? actionInError = null, int waitTime = 2) =>
            action.RetryInternal(maxTries, actionInError: actionInError, waitTime: waitTime);

        public static ConcurrentQueue<Exception> Retry(this Action action, int maxTries, TimeSpan waitTime, Action<Exception, int>? actionInError = null) =>
           action.RetryInternal(maxTries, actionInError: actionInError, waitTime: waitTime);

        public static ConcurrentQueue<Exception> Retry<TParam>(this Action<TParam> action, TParam param, int maxTries, int waitTime = 2) =>
            action.RetryInternal(param, maxTries, waitTime: waitTime);

        public static ConcurrentQueue<Exception> Retry<TParam>(this Action<TParam> action, TParam param, int maxTries, TimeSpan waitTime) =>
            action.RetryInternal(param, maxTries, waitTime: waitTime);

        public static TResult? Retry<TResult>(this Func<TResult> action, int maxTries, out ConcurrentQueue<Exception> exceptions, int waitTime = 2)
        {
            exceptions = new ConcurrentQueue<Exception>();

            TResult? result = action.RetryInternal(maxTries, exception: ref exceptions, waitTime: waitTime);

            return result;
        }

        public static TResult? Retry<TResult>(this Func<TResult> action, int maxTries, TimeSpan waitTime, out ConcurrentQueue<Exception> exceptions)
        {
            exceptions = new ConcurrentQueue<Exception>();

            TResult? result = action.RetryInternal(maxTries, exception: ref exceptions, waitTime: waitTime);

            return result;
        }

        public static TResult? Retry<TResult>(this Func<TResult> action, int maxTries, Action<Exception, int> actionInError, out ConcurrentQueue<Exception> exceptions, int waitTime = 2)
        {
            exceptions = new ConcurrentQueue<Exception>();

            TResult? result = action.RetryInternal(maxTries, ref exceptions, actionInError: actionInError, waitTime: waitTime);

            return result;
        }

        public static TResult? Retry<TResult>(this Func<TResult> action, int maxTries, Action<Exception, int> actionInError, TimeSpan waitTime, out ConcurrentQueue<Exception> exceptions)
        {
            exceptions = new ConcurrentQueue<Exception>();

            TResult? result = action.RetryInternal(maxTries, ref exceptions, actionInError: actionInError, waitTime: waitTime);

            return result;
        }

        #endregion

        #endregion

        #region Privates

        #region Async

        private static async Task RetryInternalAsync(this Func<Task> action, int maxTries, ConcurrentQueue<Exception> exception, int currentTry = 1, Action<Exception, int>? actionInError = null, int waitTime = 5)
        {
            ArgumentNullException.ThrowIfNull(action);

            exception = exception ?? new ConcurrentQueue<Exception>();

            try
            {
                await action();
            }
            catch (Exception ex)
            {
                exception.Enqueue(new Exception($"Error on try {currentTry}, see inner for more information.", ex));
                actionInError?.Invoke(ex, currentTry);

                if (currentTry < maxTries)
                {
                    await Task.Delay(TimeSpan.FromSeconds(waitTime));

                    await action.RetryInternalAsync(maxTries: maxTries,
                        currentTry: ++currentTry,
                        exception: exception,
                        actionInError: actionInError,
                        waitTime: waitTime);
                }
            }
        }

        private static async Task RetryInternalAsync(this Func<Task> action, int maxTries, ConcurrentQueue<Exception> exception, TimeSpan waitTime, int currentTry = 1, Action<Exception, int>? actionInError = null)
        {
            ArgumentNullException.ThrowIfNull(action);

            exception = exception ?? new ConcurrentQueue<Exception>();

            try
            {
                await action();
            }
            catch (Exception ex)
            {
                exception.Enqueue(new Exception($"Error on try {currentTry}, see inner for more information.", ex));
                actionInError?.Invoke(ex, currentTry);

                if (currentTry < maxTries)
                {
                    await Task.Delay(waitTime);

                    await action.RetryInternalAsync(maxTries: maxTries,
                        exception: exception,
                        waitTime: waitTime,
                        currentTry: ++currentTry,
                        actionInError: actionInError);
                }
            }
        }

        private static async Task<TResult?> RetryInternalAsync<TResult>(this Func<int, Task<TResult>> action, int maxTries, ConcurrentQueue<Exception>? exception = null, int currentTry = 1, Action<Exception, int>? actionInError = null, int waitTime = 5)
        {
            ArgumentNullException.ThrowIfNull(action);

            TResult? result = default;
            exception = exception ?? new ConcurrentQueue<Exception>();

            try
            {
                result = await action(currentTry);
            }
            catch (Exception ex)
            {
                exception.Enqueue(new Exception($"Error on try {currentTry}, see inner for more information.", ex));
                actionInError?.Invoke(ex, currentTry);

                if (currentTry < maxTries)
                {
                    await Task.Delay(TimeSpan.FromSeconds(waitTime));

                    result = await action.RetryInternalAsync(maxTries: maxTries,
                        currentTry: ++currentTry,
                        exception: exception,
                        actionInError: actionInError,
                        waitTime: waitTime);
                }
            }

            return result;
        }

        private static async Task<TResult?> RetryInternalAsync<TResult>(this Func<int, Task<TResult>> action, int maxTries, TimeSpan waitTime, ConcurrentQueue<Exception>? exception = null, int currentTry = 1, Action<Exception, int>? actionInError = null)
        {
            ArgumentNullException.ThrowIfNull(action);

            TResult? result = default;
            exception = exception ?? new ConcurrentQueue<Exception>();

            try
            {
                result = await action(currentTry);
            }
            catch (Exception ex)
            {
                exception.Enqueue(new Exception($"Error on try {currentTry}, see inner for more information.", ex));
                actionInError?.Invoke(ex, currentTry);

                if (currentTry < maxTries)
                {
                    await Task.Delay(waitTime);

                    result = await action.RetryInternalAsync(maxTries: maxTries,
                        currentTry: ++currentTry,
                        exception: exception,
                        actionInError: actionInError,
                        waitTime: waitTime);
                }
            }

            return result;
        }

        #endregion

        #region Sync

        //private static ConcurrentQueue<Exception> RetryInternal(this Action action, int maxTries, int currentTry = 1, ConcurrentQueue<Exception>? exception = null, int waitTime = 5)
        //{
        //    ArgumentNullException.ThrowIfNull(action);

        //    exception = exception ?? new ConcurrentQueue<Exception>();

        //    try
        //    {
        //        action();
        //    }
        //    catch (Exception ex)
        //    {
        //        exception.Enqueue(new Exception($"Error on try {currentTry}, see inner for more information.", ex));

        //        if (currentTry < maxTries)
        //        {
        //            Thread.Sleep(TimeSpan.FromSeconds(waitTime));
        //            action.RetryInternal(maxTries, ++currentTry, exception, waitTime);
        //        }
        //    }

        //    return exception;
        //}

        private static ConcurrentQueue<Exception> RetryInternal(this Action action, int maxTries, int currentTry = 1, Action<Exception, int>? actionInError = null, ConcurrentQueue<Exception>? exception = null, int waitTime = 5)
        {
            ArgumentNullException.ThrowIfNull(action);

            exception = exception ?? new ConcurrentQueue<Exception>();

            try
            {
                action();
            }
            catch (Exception ex)
            {
                exception.Enqueue(new Exception($"Error on try {currentTry}, see inner for more information.", ex));
                actionInError?.Invoke(ex, currentTry);

                if (currentTry < maxTries)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(waitTime));

                    return action.RetryInternal(maxTries: maxTries,
                        currentTry: ++currentTry,
                        exception: exception,
                        actionInError: actionInError,
                        waitTime: waitTime);
                }
            }

            return exception;
        }

        private static ConcurrentQueue<Exception> RetryInternal(this Action action, int maxTries, TimeSpan waitTime, int currentTry = 1, Action<Exception, int>? actionInError = null, ConcurrentQueue<Exception>? exception = null)
        {
            ArgumentNullException.ThrowIfNull(action);

            exception = exception ?? new ConcurrentQueue<Exception>();

            try
            {
                action();
            }
            catch (Exception ex)
            {
                exception.Enqueue(new Exception($"Error on try {currentTry}, see inner for more information.", ex));
                actionInError?.Invoke(ex, currentTry);

                if (currentTry < maxTries)
                {
                    Thread.Sleep(waitTime);

                    return action.RetryInternal(maxTries: maxTries,
                        currentTry: ++currentTry,
                        exception: exception,
                        actionInError: actionInError,
                        waitTime: waitTime);
                }
            }

            return exception;
        }

        private static ConcurrentQueue<Exception> RetryInternal<TParam>(this Action<TParam> action, TParam param, int maxTries, int currentTry = 1, ConcurrentQueue<Exception>? exception = null, int waitTime = 5)
        {
            ArgumentNullException.ThrowIfNull(action);

            exception = exception ?? new ConcurrentQueue<Exception>();

            try
            {
                action(param);
            }
            catch (Exception ex)
            {
                exception.Enqueue(new Exception($"Error on try {currentTry}, see inner for more information.", ex));

                if (currentTry < maxTries)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(waitTime));
                    action.RetryInternal(param, maxTries, ++currentTry, exception, waitTime);
                }
            }

            return exception;
        }

        private static ConcurrentQueue<Exception> RetryInternal<TParam>(this Action<TParam> action, TParam param, int maxTries, TimeSpan waitTime, int currentTry = 1, ConcurrentQueue<Exception>? exception = null)
        {
            ArgumentNullException.ThrowIfNull(action);

            exception = exception ?? new ConcurrentQueue<Exception>();

            try
            {
                action(param);
            }
            catch (Exception ex)
            {
                exception.Enqueue(new Exception($"Error on try {currentTry}, see inner for more information.", ex));

                if (currentTry < maxTries)
                {
                    Thread.Sleep(waitTime);
                    action.RetryInternal(param, maxTries, waitTime, ++currentTry, exception);
                }
            }

            return exception;
        }

        private static TResult? RetryInternal<TResult>(this Func<TResult> action, int maxTries, ref ConcurrentQueue<Exception> exception, int currentTry = 1, Action<Exception, int>? actionInError = null, int waitTime = 5)
        {
            ArgumentNullException.ThrowIfNull(action);

            exception = exception ?? new ConcurrentQueue<Exception>();

            try
            {
                return action();
            }
            catch (Exception ex)
            {
                exception.Enqueue(new Exception($"Error on try {currentTry}, see inner for more information.", ex));
                actionInError?.Invoke(ex, currentTry);

                if (currentTry < maxTries)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(waitTime));

                    return action.RetryInternal(maxTries: maxTries,
                        currentTry: ++currentTry,
                        exception: ref exception,
                        actionInError: actionInError,
                        waitTime: waitTime);
                }
            }

            return default;
        }

        private static TResult? RetryInternal<TResult>(this Func<TResult> action, int maxTries, ref ConcurrentQueue<Exception> exception, TimeSpan waitTime, int currentTry = 1, Action<Exception, int>? actionInError = null)
        {
            ArgumentNullException.ThrowIfNull(action);

            exception = exception ?? new ConcurrentQueue<Exception>();

            try
            {
                return action();
            }
            catch (Exception ex)
            {
                exception.Enqueue(new Exception($"Error on try {currentTry}, see inner for more information.", ex));
                actionInError?.Invoke(ex, currentTry);

                if (currentTry < maxTries)
                {
                    Thread.Sleep(waitTime);

                    return action.RetryInternal(maxTries: maxTries,
                        currentTry: ++currentTry,
                        exception: ref exception,
                        actionInError: actionInError,
                        waitTime: waitTime);
                }
            }

            return default;
        }

        #endregion

        #endregion
    }
}
