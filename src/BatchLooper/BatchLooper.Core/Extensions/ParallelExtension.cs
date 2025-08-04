using System.Diagnostics.CodeAnalysis;

namespace BatchLooper.Core.Extensions
{
    public static class ParallelExtension
    {
        public static bool SkipInteration([NotNull] this CancellationToken cancellationToken) => cancellationToken.IsCancellationRequested;

        public static bool SkipInteration([NotNull] this ParallelLoopState state) => state.IsStopped || state.ShouldExitCurrentIteration;

        public static bool SkipInteration([NotNull] this ParallelLoopState state, CancellationToken cancellationToken = default) => state.SkipInteration() || cancellationToken.SkipInteration();

        public static bool SkipInteration([NotNull] this ParallelLoopState state, long idx) =>
            state.IsStopped || (state.ShouldExitCurrentIteration && state.LowestBreakIteration < idx);

        public static bool SkipInteration([NotNull] this ParallelLoopState state, bool cancel, long idx) =>
            state.SkipInteration(idx) || cancel;

        public static bool SkipInteration([NotNull] this ParallelLoopState state, long idx, CancellationToken cancellationToken = default) =>
            SkipInteration(state, idx) || cancellationToken.SkipInteration();

        public static bool SkipInteration([NotNull] this ParallelLoopState state, bool cancel, long idx, CancellationToken cancellationToken = default) =>
            state.SkipInteration(cancel, idx) || cancellationToken.SkipInteration();

        public static int DefaultMaxDegreeOfParallelism() => DefaultMaxDegreeOfParallelism(1);//TODO: make configurable in Config.

        public static int DefaultMaxDegreeOfParallelism(int offset) => Environment.ProcessorCount > offset ? (Environment.ProcessorCount - offset) : Environment.ProcessorCount;

        public static ParallelQuery<TSource> AsParallelCustom<TSource>([NotNull] this IEnumerable<TSource> source, CancellationToken? cancellationToken = null, int? degreeOfParallelism = null, ParallelExecutionMode? executionMode = null, ParallelMergeOptions? mergeOptions = null)
        {
            ArgumentNullException.ThrowIfNull(source);

            var query = source.AsParallel();

            if (cancellationToken is not null)
                query = query.WithCancellation(cancellationToken.Value);

            if (degreeOfParallelism.HasValue)
                query = query.WithDegreeOfParallelism(degreeOfParallelism.Value);

            if (executionMode.HasValue)
                query = query.WithExecutionMode(executionMode.Value);

            if (mergeOptions.HasValue)
                query = query.WithMergeOptions(mergeOptions.Value);

            return query;
        }

        public static ParallelQuery<TSource> AsParallelCustom<TSource>([NotNull] this IQueryable<TSource> source, CancellationToken? cancellationToken = null, int? degreeOfParallelism = null, ParallelExecutionMode? executionMode = null, ParallelMergeOptions? mergeOptions = null)
        {
            ArgumentNullException.ThrowIfNull(source);

            if (!degreeOfParallelism.HasValue || degreeOfParallelism < 0)
                degreeOfParallelism = DefaultMaxDegreeOfParallelism();

            var query = source.AsParallel();

            if (cancellationToken is not null)
                query = query.WithCancellation(cancellationToken.Value);

            if (degreeOfParallelism.HasValue)
                query = query.WithDegreeOfParallelism(degreeOfParallelism.Value);

            if (executionMode.HasValue)
                query = query.WithExecutionMode(executionMode.Value);

            if (mergeOptions.HasValue)
                query = query.WithMergeOptions(mergeOptions.Value);

            return query;
        }
    }
}
