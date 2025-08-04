using BatchLooper.Core.Interfaces.Batcher;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks.Dataflow;
using static BatchLooper.Core.Helpers.PaginationHelper;

namespace BatchLooper.Infrastructure.Services.Batcher
{
    public record BatcherConfiguration<TEntity> : IBatcherConfiguration<TEntity>
    {
        #region Variables

        private IEnumerable<TEntity> _source;
        private int _sourceCount;
        private int _batchCount;
        private int _batchSize;
        private int _maxDegreeOfParallelism;
        private bool _printProgress;
        private int _maxTries;
        private int _waitTime;
        private TimeSpan? _timeForcerStartDelay;
        private TimeSpan? _timeForcerWaitBetweenExecution;
        private DataflowLinkOptions _linkOptions;
        private GroupingDataflowBlockOptions _groupingOptions;
        private ExecutionDataflowBlockOptions _executionOptions;
        private DataflowBlockOptions? _bufferOptions;
        private bool _defaultBuffer;
        private CancellationToken _cancellationToken;
        private readonly IBatcherConfiguration<TEntity> _recusive;

        #endregion

        #region Properties

        IEnumerable<TEntity> IBatcherConfiguration<TEntity>.Source => _source;

        int IBatcherConfiguration<TEntity>.SourceCount => _sourceCount;

        int IBatcherConfiguration<TEntity>.BatchCount => _batchCount;

        int IBatcherConfiguration<TEntity>.BatchSize => _batchSize;

        int IBatcherConfiguration<TEntity>.MaxDegreeOfParallelism => _maxDegreeOfParallelism;

        DataflowLinkOptions IBatcherConfiguration<TEntity>.LinkOptions => _linkOptions;

        GroupingDataflowBlockOptions IBatcherConfiguration<TEntity>.GroupingOptions => _groupingOptions;

        ExecutionDataflowBlockOptions IBatcherConfiguration<TEntity>.ExecutionOptions => _executionOptions;

        int IBatcherConfiguration<TEntity>.MaxTries => _maxTries;

        int IBatcherConfiguration<TEntity>.WaitTime => _waitTime;

        TimeSpan? IBatcherConfiguration<TEntity>.TimeForcerStartDelay => _timeForcerStartDelay;

        TimeSpan? IBatcherConfiguration<TEntity>.TimeForcerWaitBetweenExecution => _timeForcerWaitBetweenExecution;

        bool IBatcherConfiguration<TEntity>.PrintProgress => _printProgress;

        bool IBatcherConfiguration<TEntity>.PrintProgressManual => _maxDegreeOfParallelism == -1 || !_printProgress;

        CancellationToken IBatcherConfiguration<TEntity>.CancellationToken => _cancellationToken;

        DataflowBlockOptions? IBatcherConfiguration<TEntity>.BufferOptions => _bufferOptions;

        bool IBatcherConfiguration<TEntity>.DefaultBuffer => _defaultBuffer;

        #endregion

        #region Initializers

        public BatcherConfiguration()
        {
            _defaultBuffer = false;
            _recusive = this;

            _source = [];
            _linkOptions = default!;
            _groupingOptions = default!;
            _executionOptions = default!;

            _recusive
                .WithLinkOptions(new DataflowLinkOptions()
                {
                    PropagateCompletion = true,
                })
            .WithBatchBlockOptions(new GroupingDataflowBlockOptions()
            {
                EnsureOrdered = true,
                TaskScheduler = TaskScheduler.Default, //TaskScheduler.FromCurrentSynchronizationContext(),
                BoundedCapacity = DataflowBlockOptions.Unbounded,
                Greedy = true,
            })
            .WithBatchExecutionOptions(new ExecutionDataflowBlockOptions()
            {
                EnsureOrdered = true,
                TaskScheduler = TaskScheduler.Default, //TaskScheduler.FromCurrentSynchronizationContext()
                BoundedCapacity = DataflowBlockOptions.Unbounded,
                SingleProducerConstrained = true,
            })
            .WithMaxDegreeOfParallelism(1)
            .HideProgress()
            .WithMaxTries(3)
            .WithWaitTime(2);
        }

        public BatcherConfiguration([NotNull] IEnumerable<TEntity> source) : this() => _recusive.WithSource(source);

        public BatcherConfiguration([NotNull] IEnumerable<TEntity> source, int batchSize) : this(source) => _recusive.WithBatchSize(batchSize);

        public BatcherConfiguration([NotNull] IEnumerable<TEntity> source, int maxDegreeOfParallelism, bool batchSizeByMaxDegreeOfParallelism) : this(source)
        {
            _recusive.WithMaxDegreeOfParallelism(maxDegreeOfParallelism);

            if (batchSizeByMaxDegreeOfParallelism)
                _recusive.WithBatchSizeCalculateByMaxDegreeOfParallelism();
        }

        #endregion

        #region Functions And Routines

        #region Public

        IBatcherConfiguration<TEntity> IBatcherConfiguration<TEntity>.HideProgress()
        {
            _printProgress = false;
            return this;
        }

        void IBatcherConfiguration<TEntity>.MountConfiguration()
        {
            Guard();

            if (_maxDegreeOfParallelism == -1)
                _recusive.HideProgress();

            _groupingOptions.MaxMessagesPerTask = _executionOptions.MaxMessagesPerTask = _batchSize;
            _groupingOptions.MaxNumberOfGroups = _batchCount;
            _executionOptions.MaxDegreeOfParallelism = _maxDegreeOfParallelism;
            _groupingOptions.CancellationToken = _executionOptions.CancellationToken = _cancellationToken;
        }

        IBatcherConfiguration<TEntity> IBatcherConfiguration<TEntity>.ShowProgress()
        {
            _printProgress = true;
            return this;
        }

        IBatcherConfiguration<TEntity> IBatcherConfiguration<TEntity>.WithBatchBlockOptions([NotNull] GroupingDataflowBlockOptions batchBlockOptions)
        {
            _groupingOptions = batchBlockOptions ?? throw new ArgumentNullException(nameof(batchBlockOptions));
            return this;
        }

        IBatcherConfiguration<TEntity> IBatcherConfiguration<TEntity>.WithBatchBlockOptions([NotNull] DataflowBlockOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            return _recusive.WithBatchBlockOptions(new()
            {
                BoundedCapacity = options.BoundedCapacity,
                CancellationToken = options.CancellationToken,
                EnsureOrdered = options.EnsureOrdered,
                Greedy = true, //default Value.
                MaxMessagesPerTask = options.MaxMessagesPerTask,
                MaxNumberOfGroups = DataflowBlockOptions.Unbounded, //default Value.
                NameFormat = options.NameFormat,
                TaskScheduler = options.TaskScheduler,
            });
        }

        IBatcherConfiguration<TEntity> IBatcherConfiguration<TEntity>.WithBatchExecutionOptions([NotNull] ExecutionDataflowBlockOptions batchHandleOptions)
        {
            _executionOptions = batchHandleOptions ?? throw new ArgumentNullException(nameof(batchHandleOptions));
            return this;
        }

        IBatcherConfiguration<TEntity> IBatcherConfiguration<TEntity>.WithBatchExecutionOptions([NotNull] DataflowBlockOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            return _recusive.WithBatchExecutionOptions(new()
            {
                BoundedCapacity = options.BoundedCapacity,
                CancellationToken = options.CancellationToken,
                EnsureOrdered = options.EnsureOrdered,
                MaxDegreeOfParallelism = 1,//default Value.
                MaxMessagesPerTask = options.MaxMessagesPerTask,
                NameFormat = options.NameFormat,
                SingleProducerConstrained = false,//default Value.
                TaskScheduler = options.TaskScheduler
            });
        }

        IBatcherConfiguration<TEntity> IBatcherConfiguration<TEntity>.WithBatchSize(int batchSize)
        {
            ArgumentNullException.ThrowIfNull(_source, nameof(_source));
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(_sourceCount, 0, nameof(_source));
            ArgumentOutOfRangeException.ThrowIfLessThan(batchSize, 0);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(batchSize, _sourceCount);

            _batchSize = batchSize;
            _batchCount = (int)Math.Ceiling(_sourceCount / (decimal)_batchSize);

            return this;
        }

        IBatcherConfiguration<TEntity> IBatcherConfiguration<TEntity>.WithBatchSizeCalculateByMaxDegreeOfParallelism()
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(_maxDegreeOfParallelism, 0);
            ArgumentNullException.ThrowIfNull(_source);

            var batchSize = (int)Math.Ceiling(_sourceCount / (decimal)_maxDegreeOfParallelism);
            return _recusive.WithBatchSize(batchSize);
        }

        IBatcherConfiguration<TEntity> IBatcherConfiguration<TEntity>.WithBuffer()
        {
            _defaultBuffer = true;
            return this;
        }

        IBatcherConfiguration<TEntity> IBatcherConfiguration<TEntity>.WithBuffer([NotNull] DataflowBlockOptions options)
        {
            _bufferOptions = options ?? throw new ArgumentNullException(nameof(options));
            _defaultBuffer = false;

            return this;
        }

        IBatcherConfiguration<TEntity> IBatcherConfiguration<TEntity>.WithCancellationToken(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            return this;
        }

        IBatcherConfiguration<TEntity> IBatcherConfiguration<TEntity>.WithForceExecutionTimer(int delayBeforeExecution, int waitBetweenExecution)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(delayBeforeExecution, 0);
            ArgumentOutOfRangeException.ThrowIfLessThan(waitBetweenExecution, 0);

            return _recusive.WithForceExecutionTimer(TimeSpan.FromSeconds(delayBeforeExecution), TimeSpan.FromSeconds(waitBetweenExecution));
        }

        IBatcherConfiguration<TEntity> IBatcherConfiguration<TEntity>.WithForceExecutionTimer([NotNull] TimeSpan delayBeforeExecution, [NotNull] TimeSpan waitBetweenExecution)
        {
            _timeForcerStartDelay = delayBeforeExecution;
            _timeForcerWaitBetweenExecution = waitBetweenExecution;

            return this;
        }

        IBatcherConfiguration<TEntity> IBatcherConfiguration<TEntity>.WithLinkOptions([NotNull] DataflowLinkOptions linkOptions)
        {
            _linkOptions = linkOptions ?? throw new ArgumentNullException(nameof(linkOptions));
            return this;
        }

        IBatcherConfiguration<TEntity> IBatcherConfiguration<TEntity>.WithMaxDegreeOfParallelism(int maxDegreeOfParallelism)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(maxDegreeOfParallelism, -1);
            ArgumentOutOfRangeException.ThrowIfZero(maxDegreeOfParallelism);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(maxDegreeOfParallelism, Environment.ProcessorCount);

            _maxDegreeOfParallelism = maxDegreeOfParallelism;
            return this;
        }

        IBatcherConfiguration<TEntity> IBatcherConfiguration<TEntity>.WithMaxTries(int tries)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(tries, 1);

            _maxTries = tries;

            return this;
        }

        IBatcherConfiguration<TEntity> IBatcherConfiguration<TEntity>.WithSource([NotNull] IEnumerable<TEntity> source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _sourceCount = source.Count();

            var possibilities = source.GetPaginatePartitionPossibility();
            var (_, batchSize) = possibilities.ElementAt((possibilities.Count() - 1) / 2);

            return _recusive.WithBatchSize(batchSize);
        }

        IBatcherConfiguration<TEntity> IBatcherConfiguration<TEntity>.WithWaitTime(int time)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(time, 1);

            _waitTime = time;

            return this;
        }

        #endregion

        #region Privates

        void Guard()
        {
            ArgumentNullException.ThrowIfNull(_source);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(_sourceCount);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(_batchCount);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(_batchSize);
            ArgumentOutOfRangeException.ThrowIfLessThan(_maxDegreeOfParallelism, -1);

            ArgumentNullException.ThrowIfNull(_groupingOptions);
            ArgumentNullException.ThrowIfNull(_linkOptions);
            ArgumentNullException.ThrowIfNull(_executionOptions);
        }

        #endregion

        #endregion
    }
}
