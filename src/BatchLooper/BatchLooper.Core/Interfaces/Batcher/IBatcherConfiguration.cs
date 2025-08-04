using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks.Dataflow;

namespace BatchLooper.Core.Interfaces.Batcher
{
    public interface IBatcherConfiguration<TEntity>
    {
        IEnumerable<TEntity> Source { get; }
        int SourceCount { get; }
        int BatchCount { get; }
        int BatchSize { get; }
        int MaxDegreeOfParallelism { get; }
        DataflowLinkOptions LinkOptions { get; }
        GroupingDataflowBlockOptions GroupingOptions { get; }
        ExecutionDataflowBlockOptions ExecutionOptions { get; }
        DataflowBlockOptions? BufferOptions { get; }
        int MaxTries { get; }
        int WaitTime { get; }
        TimeSpan? TimeForcerStartDelay { get; }
        TimeSpan? TimeForcerWaitBetweenExecution { get; }
        bool PrintProgress { get; }
        bool PrintProgressManual { get; }
        bool DefaultBuffer { get; }
        CancellationToken CancellationToken { get; }


        /// <summary>
        /// Configure the source that will be interate and auto calculate de batch size and batch count.
        /// </summary>
        /// <param name="source">the collection hat will be processed.</param>
        /// <exception cref="ArgumentNullException">If source is null</exception>
        IBatcherConfiguration<TEntity> WithSource([NotNull] IEnumerable<TEntity> source);

        /// <summary>
        /// Configure the size ou batch and the count of batches.
        /// </summary>
        /// <param name="batchSize">the amount that will be processed in one batch</param>
        IBatcherConfiguration<TEntity> WithBatchSize(int batchSize);

        /// <summary>
        /// Configure Parallelism of execution.
        /// </summary>
        /// <param name="maxDegreeOfParallelism">Amount of parallel thread, not be lass then 1 and greather then 'Environment.ProcessorCount'.</param>
        IBatcherConfiguration<TEntity> WithMaxDegreeOfParallelism(int maxDegreeOfParallelism);

        /// <summary>
        /// Configure Batch size based on MaxDegreeOfParallelism.
        /// </summary>
        IBatcherConfiguration<TEntity> WithBatchSizeCalculateByMaxDegreeOfParallelism();

        /// <summary>
        /// Configure custom option for link between action in pipeline.
        /// </summary>
        /// <param name="linkOptions">The options that will link all actions.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">If linkOptions is null.</exception>
        IBatcherConfiguration<TEntity> WithLinkOptions([NotNull] DataflowLinkOptions linkOptions);

        /// <summary>
        /// Configure Custom option for batching process in pipeline.
        /// </summary>
        /// <param name="batchBlockOptions">The option that will manager batching process in pipeline.</param>
        /// <exception cref="ArgumentNullException">If batchBlockOptions is null.</exception>
        IBatcherConfiguration<TEntity> WithBatchBlockOptions([NotNull] GroupingDataflowBlockOptions batchBlockOptions);

        /// <summary>
        /// Configure Custom option for batching process in pipeline.
        /// </summary>
        /// <param name="options">The option that will manager batching process in pipeline.</param>
        IBatcherConfiguration<TEntity> WithBatchBlockOptions([NotNull] DataflowBlockOptions options);

        /// <summary>
        /// Configure Custom option for execution handling of pipeline.
        /// </summary>
        /// <param name="batchHandleOptions">The Option tha will manager process handling.</param>
        /// <exception cref="ArgumentNullException">If batchHandleOptions is null.</exception>
        IBatcherConfiguration<TEntity> WithBatchExecutionOptions([NotNull] ExecutionDataflowBlockOptions batchHandleOptions);

        /// <summary>
        /// Configure Custom option for execution handling of pipeline.
        /// </summary>
        /// <param name="options">The Option tha will manager process handling.</param>
        IBatcherConfiguration<TEntity> WithBatchExecutionOptions([NotNull] DataflowBlockOptions options);

        /// <summary>
        /// Configure a default buffer without any option. Active buffer in pipeline.
        /// </summary>
        public IBatcherConfiguration<TEntity> WithBuffer();

        /// <summary>
        /// Configure a default buffer with specific option. Active buffer in pipeline.
        /// </summary>
        /// <param name="options">The Option that manager buffer in pipeline.</param>
        /// <returns></returns>
        IBatcherConfiguration<TEntity> WithBuffer([NotNull] DataflowBlockOptions options);

        /// <summary>
        /// Configure Tries time.
        /// </summary>
        /// <param name="tries">the many times will try, not be less than 1. Default: 3 seconds.</param>
        IBatcherConfiguration<TEntity> WithMaxTries(int tries);

        /// <summary>
        /// Configure wait in seconds between tries.
        /// </summary>
        /// <param name="time">Time to wait in seconds, not be less than 1. Default: 2 seconds.</param>
        IBatcherConfiguration<TEntity> WithWaitTime(int time);

        /// <summary>
        /// Configure one timer to force Execution if batch size not reseach.
        /// </summary>
        /// <param name="delayBeforeExecution">Delay in second before execution callback, default 150 seconds. Note: must have time necessary to await pipe execution.</param>
        /// <param name="waitBetweenExecution">Delay time in seconds between executions, default 10 seconds.</param>
        IBatcherConfiguration<TEntity> WithForceExecutionTimer(int delayBeforeExecution = 150, int waitBetweenExecution = 10);

        /// <summary>
        /// Configure one timer to force Execution if batch size not reseach.
        /// </summary>
        /// <param name="delayBeforeExecution">Delay in second before execution callback. Note: must have time necessary to await pipe execution.</param>
        /// <param name="waitBetweenExecution">Delay time in seconds between executions</param>
        IBatcherConfiguration<TEntity> WithForceExecutionTimer([NotNull] TimeSpan delayBeforeExecution, [NotNull] TimeSpan waitBetweenExecution);

        /// <summary>
        /// Active Console progress print execution
        /// </summary>
        IBatcherConfiguration<TEntity> ShowProgress();

        /// <summary>
        /// Deactive Console progress print execution
        /// </summary>
        IBatcherConfiguration<TEntity> HideProgress();

        /// <summary>
        /// Configure Cancelation token.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        IBatcherConfiguration<TEntity> WithCancellationToken(CancellationToken cancellationToken = default);

        /// <summary>
        /// Integrate and validate all Configurations
        /// </summary>
        void MountConfiguration();
    }
}
