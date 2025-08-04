namespace BatchLooper.Core.Helpers
{
    public static class SyncTaskHelper
    {
        public static void Sync(this Task task, bool continueOnCapturedContext = false)
        {
            if (task is null) return;

            task.Sync(continueOnCapturedContext ? ConfigureAwaitOptions.ContinueOnCapturedContext : ConfigureAwaitOptions.None);
        }

        public static void Sync(this Task task, ConfigureAwaitOptions awaitOptions)
        {
            if (task is null) return;

            task
                .ConfigureAwait(awaitOptions)
                .GetAwaiter();
        }

        public static TTask Sync<TTask>(this Task<TTask> task, bool continueOnCapturedContext = false)
        {
            if (task is null) return default!;

            TTask result = task.Sync(continueOnCapturedContext ? ConfigureAwaitOptions.ContinueOnCapturedContext : ConfigureAwaitOptions.None);

            return result;
        }

        public static TTask Sync<TTask>(this Task<TTask> task, ConfigureAwaitOptions awaitOptions)
        {
            if (task is null) return default!;

            TTask result = task
                .ConfigureAwait(awaitOptions)
                .GetAwaiter()
                .GetResult();

            return result;
        }
    }
}
