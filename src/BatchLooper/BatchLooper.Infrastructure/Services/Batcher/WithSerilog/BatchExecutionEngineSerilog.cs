using BatchLooper.Core.Enumerations;
using BatchLooper.Core.Extensions;
using BatchLooper.Core.Helpers;
using BatchLooper.Core.Interfaces.Batcher;
using BatchLooper.Core.Interfaces.Batcher.Engine;
using BatchLooper.Core.Interfaces.Batcher.Printers;
using BatchLooper.Core.Interfaces.Patterns;
using BatchLooper.Core.Models.Batcher;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks.Dataflow;
using static BatchLooper.Core.Helpers.ExceptionHelper;
using static BatchLooper.Core.Helpers.IterateHelper;
using static BatchLooper.Core.Helpers.MemoryLeakHelper;
using static BatchLooper.Core.Helpers.SyncTaskHelper;

namespace BatchLooper.Infrastructure.Services.Batcher.WithSerilog
{
    /*
     * Atention must sync this with 'BatchExecutionEngine'.
     * Atenção refetir ajustes no 'BatchExecutionEngine'.
     */
    [DebuggerDisplay("BatchExecutionEngine: Status = {Status} | CurrentBatch = {_currentBatch} | LastError = {LastErrorMessage,nq}")]
    public class BatchExecutionEngineSerilog<TEntity> : IBatchExecutionEngine<TEntity>
    {
        #region Variables

        private readonly IBatcherConfiguration<TEntity> _config;
        private readonly IBatchProgressPrinterSerilog<TEntity> _printer;
        private readonly IBatchStatistics<TEntity> _stats;
        private readonly ISemaphorePattern _semaphore;

        private BufferBlock<TEntity>? _buffer;
        private BatchBlock<TEntity> _batcher;
        private ActionBlock<IEnumerable<TEntity>> _actionHandle;
        private Timer? _batchTimeForcer;
        private bool _finalBatchTriggered = false;
        private readonly ConcurrentQueue<Exception> _errors;
        private int _currentBatch = 0;

        private readonly ConcurrentDictionary<int, int> _printLineCache = new();
        private int _columnSelector = 0;
        private int _lineIndex = 0;
        private int _status;
        private readonly JsonSerializerOptions _serializerOptions;
        private static readonly IEnumerable<TEntity> _emptyItems = [];
        private BatchManagerModel<TEntity>? _currentManager;
        private readonly ConfigureAwaitOptions _defaultConfigureAwait;
        private readonly IEnumerable<decimal> percentileToShow;
        private ParallelOptions _defaultParallelOptions;
        private readonly IBatchExecutionEngine<TEntity> _recursive;

        #endregion

        #region Properties

        /// <summary>
        /// Used to print last error message in debug on channel object.
        /// </summary>
        private string? LastErrorMessage => _errors.TryPeek(out var error) ? error?.Message : null;

        /// <summary>
        /// Used to prit Status in debug and cast int to enum to make result component of response.
        /// </summary>
        private BatcherBuilderStatus Status => _status.ToEnum<BatcherBuilderStatus, int>();

        #endregion

        #region Initializers        

        public BatchExecutionEngineSerilog([NotNull] ISemaphorePattern semaphore, [NotNull] IBatcherConfiguration<TEntity> batcherConfiguration, [NotNull] IBatchProgressPrinterSerilog<TEntity> printer, [NotNull] IBatchStatistics<TEntity> stats)
        {
            _config = batcherConfiguration ?? throw new ArgumentNullException(nameof(batcherConfiguration));
            _printer = printer ?? throw new ArgumentNullException(nameof(printer));
            _stats = stats ?? throw new ArgumentNullException(nameof(stats));
            _semaphore = semaphore ?? throw new ArgumentNullException(nameof(semaphore));

            _batcher = default!;
            _actionHandle = default!;
            _batchTimeForcer = default!;
            _errors = new ConcurrentQueue<Exception>();
            Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.None);
            _serializerOptions = new JsonSerializerOptions()
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
                PropertyNameCaseInsensitive = true,
            };
            percentileToShow = [0.25M, 0.5M, 0.75M, 1M];
            _defaultConfigureAwait = ConfigureAwaitOptions.None;
            _defaultParallelOptions = new();

            _recursive = this;
        }

        #endregion

        #region Functions And Routines

        #region Publics

        #region Execute

        BatcherBuilderResult IBatchExecutionEngineItem<TEntity>.Execute([NotNull] Action<TEntity> handler, Func<bool>? skipHandle)
        {
            if (!_errors.IsEmpty) _errors.Clear();

            var result = new BatcherBuilderResult();

            try
            {
                SetUpPipeline(items => BatchHandleSequential(MountBatchManager(items), handler, skipHandle));
                result = Executor(skipHandle);
            }
            catch (Exception ex) when (ex is OperationCanceledException) { Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.Canceled); }
            catch (Exception ex) { Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.WithErrors); _errors.Enqueue(ex); }
            finally { result = new BatcherBuilderResult(Status, _errors); }

            return result;
        }

        Task<BatcherBuilderResult> IBatchExecutionEngineItem<TEntity>.ExecuteAsync([NotNull] Func<TEntity, Task> handler, Func<bool>? skipHandle, CancellationToken cancellationToken)
        {
            if (!_errors.IsEmpty) _errors.Clear();

            var taskSetup = SetUpPipelineAsync(cancellationToken: cancellationToken,
                    action: items =>
                    {
                        try
                        {
                            return MountBatchManagerAsync(items)
                                    .ContinueWith(async taskManagerModel => BatchHandleSequentialAsync(await taskManagerModel.ConfigureAwait(_defaultConfigureAwait), handler, skipHandle));
                        }
                        catch (Exception ex) when (ex is OperationCanceledException) { Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.Canceled); }
                        catch (Exception ex) { Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.WithErrors); _errors.Enqueue(ex); }

                        return Task.CompletedTask;
                    });

            var taskHandle = ExecutorAsync(skipHandle);
            var resultTask = Task.WhenAll(taskSetup, taskHandle);

            return taskHandle;
        }

        BatcherBuilderResult IBatchExecutionEngineCollection<TEntity>.ExecuteCollection([NotNull] Action<IEnumerable<TEntity>> handle, Func<bool>? skipHandle)
        {
            if (!_errors.IsEmpty) _errors.Clear();

            var result = new BatcherBuilderResult();

            try
            {
                SetUpPipeline(items => BatchHandleCollection(MountBatchManager(items), handle, skipHandle));
                result = Executor(skipHandle);
            }
            catch (Exception ex) when (ex is OperationCanceledException) { Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.Canceled); }
            catch (Exception ex) { Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.WithErrors); _errors.Enqueue(ex); }
            finally { result = new BatcherBuilderResult(Status, _errors); }

            return result;
        }

        Task<BatcherBuilderResult> IBatchExecutionEngineCollection<TEntity>.ExecuteCollectionAsync([NotNull] Func<IEnumerable<TEntity>, Task> handle, Func<bool>? skipHandle, CancellationToken cancellationToken)
        {
            if (!_errors.IsEmpty) _errors.Clear();

            var taskSetup = SetUpPipelineAsync(cancellationToken: cancellationToken,
                action: items =>
                {
                    try
                    {
                        return MountBatchManagerAsync(items)
                        .ContinueWith(async taskManageModel => BatchHandleCollectionAsync(await taskManageModel.ConfigureAwait(_defaultConfigureAwait), handle, skipHandle));
                    }
                    catch (Exception ex) when (ex is OperationCanceledException) { Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.Canceled); }
                    catch (Exception ex) { Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.WithErrors); _errors.Enqueue(ex); }

                    return Task.CompletedTask;
                });

            var taskHandle = ExecutorAsync(skipHandle);
            var resultTask = Task.WhenAll(taskSetup, taskHandle);

            return taskHandle;
        }

        #endregion

        #region ExecuteWithBatchItemInfo

        BatcherBuilderResult IBatchExecutionEngineItem<TEntity>.ExecuteWithBatchItemInfo([NotNull] Action<BatchItemInfoModel<TEntity>> handler, Func<bool>? skipHandle)
        {
            if (!_errors.IsEmpty) _errors.Clear();

            var result = new BatcherBuilderResult();

            try
            {
                SetUpPipeline(items => BatchHandleSequential(MountBatchManager(items), handler, skipHandle));
                result = Executor(skipHandle);
            }
            catch (Exception ex) when (ex is OperationCanceledException) { Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.Canceled); }
            catch (Exception ex) { Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.WithErrors); _errors.Enqueue(ex); }
            finally { result = new BatcherBuilderResult(Status, _errors); }

            return result;
        }

        BatcherBuilderResult IBatchExecutionEngineCollection<TEntity>.ExecuteWithBatchItemInfoCollection([NotNull] Action<IEnumerable<BatchItemInfoModel<TEntity>>> handler, Func<bool>? skipHandle)
        {
            if (!_errors.IsEmpty) _errors.Clear();

            var result = new BatcherBuilderResult();

            try
            {
                SetUpPipeline(items => BatchHandleCollection(MountBatchManager(items), handler, skipHandle));
                result = Executor(skipHandle);
            }
            catch (Exception ex) when (ex is OperationCanceledException) { Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.Canceled); }
            catch (Exception ex) { Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.WithErrors); _errors.Enqueue(ex); }
            finally { result = new BatcherBuilderResult(Status, _errors); }

            return result;
        }

        Task<BatcherBuilderResult> IBatchExecutionEngineItem<TEntity>.ExecuteWithBatchItemInfoAsync([NotNull] Func<BatchItemInfoModel<TEntity>, Task> handler, Func<bool>? skipHandle, CancellationToken cancellationToken)
        {
            if (!_errors.IsEmpty) _errors.Clear();

            var taskSetup = SetUpPipelineAsync(cancellationToken: cancellationToken,
                 action: items =>
                 {
                     try
                     {
                         return MountBatchManagerAsync(items)
                         .ContinueWith(async taskManageModel => BatchHandleSequentialAsync(await taskManageModel.ConfigureAwait(_defaultConfigureAwait), handler, skipHandle));
                     }
                     catch (Exception ex) when (ex is OperationCanceledException) { Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.Canceled); }
                     catch (Exception ex) { Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.WithErrors); _errors.Enqueue(ex); }

                     return Task.CompletedTask;
                 });

            var taskHandle = ExecutorAsync(skipHandle);
            var resultTask = Task.WhenAll(taskSetup, taskHandle);

            return taskHandle;
        }

        Task<BatcherBuilderResult> IBatchExecutionEngineCollection<TEntity>.ExecuteWithBatchItemInfoCollectionAsync([NotNull] Func<IEnumerable<BatchItemInfoModel<TEntity>>, Task> handler, Func<bool>? skipHandle, CancellationToken cancellationToken)
        {
            if (!_errors.IsEmpty) _errors.Clear();

            var taskSetup = SetUpPipelineAsync(cancellationToken: cancellationToken,
                action: items =>
                {

                    try
                    {
                        return MountBatchManagerAsync(items)
                        .ContinueWith(async taskManageModel => BatchHandleCollectionAsync(await taskManageModel.ConfigureAwait(_defaultConfigureAwait), handler, skipHandle));

                    }
                    catch (Exception ex) when (ex is OperationCanceledException) { Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.Canceled); }
                    catch (Exception ex) { Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.WithErrors); _errors.Enqueue(ex); }

                    return Task.CompletedTask;
                });

            var taskHandle = ExecutorAsync(skipHandle);
            var result = Task.WhenAll(taskSetup, taskHandle);

            return taskHandle;
        }

        #endregion

        #region ExecuteWithBatchInfo

        BatcherBuilderResult IBatchExecutionEngineCollection<TEntity>.ExecuteWithBatchInfoCollection([NotNull] Action<BatchInfoModel<TEntity>> handle, Func<bool>? skipHandle)
        {
            if (!_errors.IsEmpty) _errors.Clear();

            var result = new BatcherBuilderResult();

            try
            {
                SetUpPipeline(items => BatchHandleCollection(MountBatchManager(items), handle, skipHandle));
                result = Executor(skipHandle);
            }
            catch (Exception ex) when (ex is OperationCanceledException) { Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.Canceled); }
            catch (Exception ex) { Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.WithErrors); _errors.Enqueue(ex); }
            finally { result = new BatcherBuilderResult(Status, _errors); }

            return result;
        }

        Task<BatcherBuilderResult> IBatchExecutionEngineCollection<TEntity>.ExecuteWithBatchInfoCollectionAsync([NotNull] Func<BatchInfoModel<TEntity>, Task> handle, Func<bool>? skipHandle, CancellationToken cancellationToken)
        {
            if (!_errors.IsEmpty) _errors.Clear();

            var taskSetup = SetUpPipelineAsync(cancellationToken: cancellationToken,
                action: items =>
                {
                    try
                    {
                        return MountBatchManagerAsync(items)
                        .ContinueWith(async taskManagerModel => BatchHandleCollectionAsync(await taskManagerModel.ConfigureAwait(_defaultConfigureAwait), handle, skipHandle));
                    }
                    catch (Exception ex) when (ex is OperationCanceledException) { Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.Canceled); }
                    catch (Exception ex) { Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.WithErrors); _errors.Enqueue(ex); }

                    return Task.CompletedTask;
                });

            var taskHandle = ExecutorAsync(skipHandle);
            var resultTask = Task.WhenAll(taskSetup, taskHandle);

            return taskHandle;
        }

        #endregion

        void IBatchExecutionEngine<TEntity>.PrintStaticInfo()
        {
            if (!_config.PrintProgress && _currentManager is not null)
            {
                _printer.PrintProgress(_currentManager.TotalProgress, _stats.TotalDuration);
                _stats.PrintSummary(Status);
            }
        }

        #endregion

        #region Privates

        #region Async Pipeline

        private async Task SetUpPipelineAsync([NotNull] Func<IEnumerable<TEntity>, Task> action, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(action);

            if (cancellationToken != CancellationToken.None)
            {
                cancellationToken.Register(() =>
                {
                    if (Status == BatcherBuilderStatus.Running)
                        Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.Canceled);

                    _printer.PrintDebug("execution Cancellation requested.");

                    if (_config.PrintProgress)
                        _stats.PrintSummary(Status);
                });
            }

            _config.WithCancellationToken(cancellationToken)
                .MountConfiguration();

            if (_config.BufferOptions is not null)
                _buffer = new BufferBlock<TEntity>(_config.BufferOptions);

            if (_config.DefaultBuffer)
                _buffer = new BufferBlock<TEntity>();

            _batcher = new BatchBlock<TEntity>(_config.BatchSize, _config.GroupingOptions);
            _actionHandle = new ActionBlock<IEnumerable<TEntity>>(async items => await action(items), _config.ExecutionOptions);

            _defaultParallelOptions = new ParallelOptions()
            {
                CancellationToken = _config.CancellationToken,
                MaxDegreeOfParallelism = _config.MaxDegreeOfParallelism
            };

            await Task.CompletedTask;
        }

        private async Task LinkPipelineAsync([NotNull] Func<Task> action)
        {
            ArgumentNullException.ThrowIfNull(action);

            using var batcherLink = _batcher.LinkTo(_actionHandle, _config.LinkOptions);

            if (_config.TimeForcerStartDelay.HasValue && _config.TimeForcerWaitBetweenExecution.HasValue)
            {
                _batchTimeForcer = new Timer(_ =>
                {

                    _printer.PrintDebug($"Force batch execution in: {DateTime.Now}, waiting count items: {_batcher.OutputCount}");

                    if (!_batcher.Completion.IsCompleted)
                        _batcher?.TriggerBatch();
                },
               null,
               _config.TimeForcerStartDelay.Value,
               _config.TimeForcerWaitBetweenExecution.Value);
            }

            await action();
        }

        private async Task LinkPipelineWithBufferAsync([NotNull] Func<Task> action)
        {
            ArgumentNullException.ThrowIfNull(_buffer);
            ArgumentNullException.ThrowIfNull(action);

            using var bufferLink = _buffer.LinkTo(_batcher, _config.LinkOptions);
            using var batcherLink = _batcher.LinkTo(_actionHandle, _config.LinkOptions);

            if (_config.TimeForcerStartDelay.HasValue && _config.TimeForcerWaitBetweenExecution.HasValue)
            {
                _batchTimeForcer = new Timer(_ =>
                {

                    _printer.PrintDebug($"Force batch execution in: {DateTime.Now}, waiting count items: {_batcher.OutputCount}");

                    if (!_batcher.Completion.IsCompleted)
                        _batcher?.TriggerBatch();
                },
               null,
               _config.TimeForcerStartDelay.Value,
               _config.TimeForcerWaitBetweenExecution.Value);
            }

            await action();
        }

        private Task ProducerAsync(Func<bool>? skipHandle = null)
        {
            ArgumentNullException.ThrowIfNull(_batcher);

            Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.Running);

            return _config.Source
                .ProcessInParallelWithPartitionerAsync(parallelOptions: _defaultParallelOptions,
                 action: item => Task.Factory.StartNew(() =>
                 {
                     if (_config.CancellationToken.IsCancellationRequested || (skipHandle is not null && skipHandle()))
                         return Task.CompletedTask;

                     return _batcher.SendAsync(item, _config.CancellationToken)
                     .ContinueWith(async taskPushed =>
                     {
                         var pushed = await taskPushed.ConfigureAwait(_defaultConfigureAwait);

                         if (!pushed)
                             _printer.PrintDebug($"{nameof(ProducerAsync)} - Fail in send item, check 'BoundedCapacity' in option(batcher) maybe limit it.");

                         return Task.CompletedTask;
                     });
                 }))
                .ContinueWith(_ =>
                {
                    if (!_finalBatchTriggered)
                    {
                        _batcher.TriggerBatch();
                        _finalBatchTriggered = true;

                        _printer.PrintDebug($"[Manual Trigger] Final batch forced at {DateTime.Now}");
                    }

                    _batcher.Complete();
                });
        }

        private Task ProducerWithBufferAsync(Func<bool>? skipHandle = null)
        {
            ArgumentNullException.ThrowIfNull(_buffer);

            Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.Running);

            return _config.Source
               .ProcessInParallelWithPartitionerAsync(parallelOptions: _defaultParallelOptions,
               action: item => Task.Factory.StartNew(() =>
               {
                   if (_config.CancellationToken.IsCancellationRequested || (skipHandle is not null && skipHandle()))
                       return Task.CompletedTask;

                   return _batcher.SendAsync(item, _config.CancellationToken)
                   .ContinueWith(async taskPushed =>
                   {
                       var pushed = await taskPushed.ConfigureAwait(_defaultConfigureAwait);

                       if (!pushed)
                           _printer.PrintDebug($"{nameof(ProducerAsync)} - Fail in send item, check 'BoundedCapacity' in option(batcher) maybe limit it.");

                       return Task.CompletedTask;
                   });
               }))
               .ContinueWith(_ =>
               {
                   if (!_finalBatchTriggered)
                   {
                       _batcher.TriggerBatch();
                       _finalBatchTriggered = true;

                       _printer.PrintDebug($"[Manual Trigger] Final batch forced at {DateTime.Now}");
                   }

                   _buffer.Complete();
                   _batcher.Complete();
               });
        }

        private async Task StartPipelineAsync(Func<bool>? skipHandle = null) =>
            await LinkPipelineAsync(async () =>
            {
                try
                {
                    _stats.StartTotalDurationMeter();
                    _printer.PrintInformation($"Start Process: {_config.BatchCount:N0} x {_config.BatchSize:N0} ({_config.SourceCount:N0}) | Parallelism: {_config.MaxDegreeOfParallelism} | Tries: {_config.MaxTries} | WaitBeteewnTries: {_config.WaitTime} (s).", inDebugToo: true);

                    await ProducerAsync(skipHandle).ConfigureAwait(_defaultConfigureAwait);
                    await _actionHandle.Completion.ConfigureAwait(_defaultConfigureAwait);

                    if (Status != BatcherBuilderStatus.Canceled)
                    {
                        _ = _errors.IsEmpty ?
                        Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.Success) :
                        Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.WithErrors);
                    }
                }
                catch (OperationCanceledException) { Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.Canceled); }
                catch (Exception ex) { Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.WithErrors); _errors.Enqueue(ex); }
                finally
                {
                    if (_config.PrintProgressManual)
                        _recursive.PrintStaticInfo();
                    else
                        _stats.PrintSummary(Status);

                    Release();
                }
            });

        private async Task StartPipelineWithBufferAsync(Func<bool>? skipHandle = null) =>
            await LinkPipelineWithBufferAsync(async () =>
            {
                try
                {
                    _stats.StartTotalDurationMeter();
                    _printer.PrintInformation($"Start Process: {_config.BatchCount:N0} x {_config.BatchSize:N0} ({_config.SourceCount:N0}) | Parallelism: {_config.MaxDegreeOfParallelism} | Tries: {_config.MaxTries} | WaitBeteewnTries: {_config.WaitTime} (s).", inDebugToo: true);

                    await ProducerWithBufferAsync(skipHandle).ConfigureAwait(_defaultConfigureAwait);
                    await _actionHandle.Completion.ConfigureAwait(_defaultConfigureAwait);

                    if (Status != BatcherBuilderStatus.Canceled)
                    {
                        _status = _errors.IsEmpty ?
                        Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.Success) :
                        Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.WithErrors);
                    }
                }
                catch (OperationCanceledException) { Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.Canceled); }
                catch (Exception ex) { Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.WithErrors); _errors.Enqueue(ex); }
                finally
                {
                    if (_config.PrintProgressManual)
                        _recursive.PrintStaticInfo();
                    else
                        _stats.PrintSummary(Status);

                    Release();
                }
            })
            .ConfigureAwait(_defaultConfigureAwait);

        #endregion

        #region Sync Pipeline

        private void SetUpPipeline([NotNull] Action<IEnumerable<TEntity>> action)
        {
            ArgumentNullException.ThrowIfNull(action);

            _config.WithCancellationToken(CancellationToken.None)
                .MountConfiguration();

            if (_config.BufferOptions is not null)
                _buffer = new BufferBlock<TEntity>(_config.BufferOptions);

            if (_config.DefaultBuffer)
                _buffer = new BufferBlock<TEntity>();

            _batcher = new BatchBlock<TEntity>(_config.BatchSize, _config.GroupingOptions);
            _actionHandle = new ActionBlock<IEnumerable<TEntity>>(action, _config.ExecutionOptions);

            _defaultParallelOptions = new ParallelOptions()
            {
                CancellationToken = _config.CancellationToken,
                MaxDegreeOfParallelism = _config.MaxDegreeOfParallelism
            };
        }

        private void LinkPipeline([NotNull] Action action)
        {
            ArgumentNullException.ThrowIfNull(action);

            using var batcherLink = _batcher.LinkTo(_actionHandle, _config.LinkOptions);

            if (_config.TimeForcerStartDelay.HasValue && _config.TimeForcerWaitBetweenExecution.HasValue)
            {
                _batchTimeForcer = new Timer(_ =>
                {

                    _printer.PrintDebug($"Force batch execution in: {DateTime.Now}, waiting count items: {_batcher.OutputCount}");

                    if (!_batcher.Completion.IsCompleted)
                        _batcher?.TriggerBatch();
                },
               null,
               _config.TimeForcerStartDelay.Value,
               _config.TimeForcerWaitBetweenExecution.Value);
            }

            action();
        }

        private void LinkPipelineWithBuffer([NotNull] Action action)
        {
            ArgumentNullException.ThrowIfNull(_buffer);
            ArgumentNullException.ThrowIfNull(action);

            using var bufferLink = _buffer.LinkTo(_batcher, _config.LinkOptions);
            using var batcherLink = _batcher.LinkTo(_actionHandle, _config.LinkOptions);

            if (_config.TimeForcerStartDelay.HasValue && _config.TimeForcerWaitBetweenExecution.HasValue)
            {
                _batchTimeForcer = new Timer(_ =>
                {

                    _printer.PrintDebug($"Force batch execution in: {DateTime.Now}, waiting count items: {_batcher.OutputCount}");

                    if (!_batcher.Completion.IsCompleted)
                        _batcher?.TriggerBatch();
                },
               null,
               _config.TimeForcerStartDelay.Value,
               _config.TimeForcerWaitBetweenExecution.Value);
            }

            action();
        }

        private void Producer(Func<bool>? skipHandle = null)
        {
            ArgumentNullException.ThrowIfNull(_batcher);

            Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.Running);

            _config.Source
                .IterateSpanAndUnsafe(item =>
                {
                    var pushed = _batcher.Post(item);

                    if (!pushed)
                        _printer.PrintDebug($"{nameof(Producer)} - Fail in send item, check 'BoundedCapacity' in option(batcher) maybe limit it.");
                }, skipHandle);

            if (!_finalBatchTriggered)
            {
                _batcher.TriggerBatch();
                _finalBatchTriggered = true;

                _printer.PrintDebug($"[Manual Trigger] Final batch forced at {DateTime.Now}");
            }

            _batcher.Complete();
        }

        private void ProducerWithBuffer(Func<bool>? skipHandle = null)
        {
            ArgumentNullException.ThrowIfNull(_buffer);

            Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.Running);

            _config.Source
                .IterateSpanAndUnsafe(item =>
                {
                    var pushed = _buffer.Post(item);

                    if (!pushed)
                        _printer.PrintDebug($"{nameof(ProducerWithBufferAsync)} - Fail in send item, check 'BoundedCapacity' in option(batcher) maybe limit it.");

                }, skipHandle);

            if (!_finalBatchTriggered)
            {
                _batcher.TriggerBatch();
                _finalBatchTriggered = true;

                _printer.PrintDebug($"[Manual Trigger] Final batch forced at {DateTime.Now}");
            }

            _buffer.Complete();
            _batcher.Complete();
        }

        private void StartPipeline(Func<bool>? skipHandle = null) =>
            LinkPipeline(() =>
            {
                try
                {
                    _stats.StartTotalDurationMeter();
                    _printer.PrintInformation($"Start Process: {_config.BatchCount:N0} x {_config.BatchSize:N0} ({_config.SourceCount:N0}) | Parallelism: {_config.MaxDegreeOfParallelism} | Tries: {_config.MaxTries} | WaitBeteewnTries: {_config.WaitTime} (s).", inDebugToo: true);

                    Producer(skipHandle);
                    _actionHandle.Completion.Wait();

                    if (Status != BatcherBuilderStatus.Canceled)
                    {
                        _ = _errors.IsEmpty ?
                        Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.Success) :
                        Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.WithErrors);
                    }
                }
                catch (OperationCanceledException) { Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.Canceled); }
                catch (Exception ex) { Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.WithErrors); _errors.Enqueue(ex); }
                finally
                {
                    if (_config.PrintProgressManual)
                        _recursive.PrintStaticInfo();
                    else
                        _stats.PrintSummary(Status);

                    Release();
                }
            });

        private void StartPipelineWithBuffer(Func<bool>? skipHandle = null) =>
            LinkPipelineWithBuffer(() =>
            {
                try
                {
                    _stats.StartTotalDurationMeter();
                    _printer.PrintInformation($"Start Process: {_config.BatchCount:N0} x {_config.BatchSize:N0} ({_config.SourceCount:N0}) | Parallelism: {_config.MaxDegreeOfParallelism} | Tries: {_config.MaxTries} | WaitBeteewnTries: {_config.WaitTime} (s).", inDebugToo: true);

                    ProducerWithBuffer(skipHandle);
                    _actionHandle.Completion.Wait();

                    if (Status != BatcherBuilderStatus.Canceled)
                    {
                        _ = _errors.IsEmpty ?
                        Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.Success) :
                        Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.WithErrors);
                    }
                }
                catch (OperationCanceledException) { Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.Canceled); }
                catch (Exception ex) { Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.WithErrors); _errors.Enqueue(ex); }
                finally
                {
                    if (_config.PrintProgressManual)
                        _recursive.PrintStaticInfo();
                    else
                        _stats.PrintSummary(Status);

                    Release();
                }
            });

        #endregion

        #region Async BatchHandle

        private async Task BatchHandleSequentialAsync([NotNull] BatchManagerModel<TEntity> batchManager, [NotNull] Func<TEntity, Task> action, Func<bool>? skipHandle = null)
        {
            #region Batch Handle

            var skip = batchManager.BatchInfo is null || _config.CancellationToken.IsCancellationRequested || (skipHandle is not null && skipHandle());

            _printer.PrintDebug($"Step 2: BatchHandle | Thread: {Environment.CurrentManagedThreadId} | lane: {batchManager.BatchInfo?.LinePosition ?? -1} | Count: {batchManager.BatchInfo?.ItemsCount ?? -1} | Skip: {skip}.");

            if (skip)
                return;

            await _stats.CollectDurationAsync(async timestamp =>
             {
                 await batchManager.BatchInfo!.Collection.Span
                  .IterateSpanAndUnsafeAsync(async itemManager =>
                  {
                      try
                      {
                          if (itemManager.Index == 1 || percentileToShow.Any(quater => batchManager.BatchInfo.ItemsCount * quater == itemManager.Index))
                              _printer.PrintProgress(batchManager[itemManager.Index], TimeProvider.System.GetElapsedTime(timestamp));

                          var exceptions = await RetryExtension.RetryAsync(
                              action: async () => await action(itemManager.Data!).ConfigureAwait(_defaultConfigureAwait),
                              actionInError: (ex, tries) =>
                              {
                                  Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.WithErrors);
                                  _printer.PrintDebug($"[Tries: {tries}] => {ex.GetAggregatedMessages()}");
                              },
                              maxTries: _config.MaxTries, waitTime: _config.WaitTime)
                          .ConfigureAwait(_defaultConfigureAwait);

                          if (!exceptions.IsEmpty)
                          {
                              _errors.Enqueue(new AggregateException(message: $"Error in batch '{batchManager.BatchInfo?.Index ?? -1}', see inner.{Environment.NewLine}Info: {JsonSerializer.Serialize(batchManager.BatchInfo, _serializerOptions)}", innerExceptions: [.. exceptions.Where(x => x is not null)]));
                              FixMemoryLeak();
                          }
                      }
                      catch (Exception ex) when (ex is OperationCanceledException) { Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.Canceled); }
                      catch (Exception ex) { Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.WithErrors); _errors.Enqueue(ex); }

                  }, () => _config.CancellationToken.IsCancellationRequested || (skipHandle is not null && skipHandle()))
                  .ConfigureAwait(_defaultConfigureAwait);
             })
                .ConfigureAwait(_defaultConfigureAwait);

            #endregion
        }

        private async Task BatchHandleSequentialAsync([NotNull] BatchManagerModel<TEntity> batchManager, [NotNull] Func<BatchItemInfoModel<TEntity>, Task> action, Func<bool>? skipHandle = null)
        {
            #region Batch Handle

            var skip = batchManager.BatchInfo is null || _config.CancellationToken.IsCancellationRequested || (skipHandle is not null && skipHandle());

            _printer.PrintDebug($"Step 2: BatchHandle | Thread: {Environment.CurrentManagedThreadId} | lane: {batchManager.BatchInfo?.LinePosition ?? -1} | Count: {batchManager.BatchInfo?.ItemsCount ?? -1} | Skip: {skip}.");

            if (skip)
                return;

            await _stats.CollectDurationAsync(async timestamp =>
             {
                 await batchManager.BatchInfo!.Collection.Span
                 .IterateSpanAndUnsafeAsync(async itemManager =>
                 {
                     try
                     {
                         if (itemManager.Index == 1 || percentileToShow.Any(quater => batchManager.BatchInfo.ItemsCount * quater == itemManager.Index))
                             _printer.PrintProgress(batchManager[itemManager.Index], TimeProvider.System.GetElapsedTime(timestamp));

                         var exceptions = await RetryExtension.RetryAsync(
                             action: async () => await action(itemManager).ConfigureAwait(_defaultConfigureAwait),
                             actionInError: (ex, tries) =>
                             {
                                 Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.WithErrors);
                                 _printer.PrintDebug($"[Tries: {tries}] => {ex.GetAggregatedMessages()}");
                             },
                             maxTries: _config.MaxTries, waitTime: _config.WaitTime)
                         .ConfigureAwait(_defaultConfigureAwait);

                         if (!exceptions.IsEmpty)
                         {
                             _errors.Enqueue(new AggregateException(message: $"Error in batch '{batchManager.BatchInfo?.Index ?? -1}', see inner.{Environment.NewLine}Info: {JsonSerializer.Serialize(batchManager.BatchInfo, _serializerOptions)}", innerExceptions: [.. exceptions.Where(x => x is not null)]));
                             FixMemoryLeak();
                         }
                     }
                     catch (Exception ex) when (ex is OperationCanceledException) { Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.Canceled); }
                     catch (Exception ex) { Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.WithErrors); _errors.Enqueue(ex); }
                 }, () => _config.CancellationToken.IsCancellationRequested || (skipHandle is not null && skipHandle()))
                 .ConfigureAwait(_defaultConfigureAwait);
             })
                 .ConfigureAwait(_defaultConfigureAwait);
            #endregion
        }

        private async Task BatchHandleParallelAsync([NotNull] BatchManagerModel<TEntity> batchManager, [NotNull] Func<TEntity, Task> action, Func<bool>? skipHandle = null)
        {
            #region Batch Handle

            var skip = batchManager.BatchInfo is null || _config.CancellationToken.IsCancellationRequested || (skipHandle is not null && skipHandle());

            _printer.PrintDebug($"Step 2: BatchHandle | Thread: {Environment.CurrentManagedThreadId} | lane: {batchManager.BatchInfo?.LinePosition ?? -1} | Count: {batchManager.BatchInfo?.ItemsCount ?? -1} | Skip: {skip}.");

            if (skip)
                return;

            await _stats.CollectDurationAsync(async timestamp =>
             {
                 await batchManager.BatchInfo!.Collection.Span
                 .ProcessInParallelWhenAllAsync(batchesInParallel: _config.BatchCount, degreeOfParallelism: _config is not { MaxDegreeOfParallelism: -1 } ? _config.MaxDegreeOfParallelism : null, partitionerOptions: EnumerablePartitionerOptions.NoBuffering, cancellationToken: _config.CancellationToken, body: async itemManager =>
                 //.ProcessInParallelWithPartitionerAsync(parallelOptions: _defaultParallelOptions, action: async itemManager =>
                 {
                     try
                     {
                         if (_config.CancellationToken.IsCancellationRequested || (skipHandle is not null && skipHandle()))
                             return;

                         if (itemManager.Index == 1 || percentileToShow.Any(quater => batchManager.BatchInfo.ItemsCount * quater == itemManager.Index))
                             _printer.PrintProgress(batchManager[itemManager.Index], TimeProvider.System.GetElapsedTime(timestamp));

                         var exceptions = await RetryExtension.RetryAsync(
                             action: async () => await action(itemManager.Data!).ConfigureAwait(_defaultConfigureAwait),
                             actionInError: (ex, tries) =>
                             {
                                 Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.WithErrors);
                                 _printer.PrintDebug($"[Tries: {tries}] => {ex.GetAggregatedMessages()}");
                             },
                             maxTries: _config.MaxTries, waitTime: _config.WaitTime)
                         .ConfigureAwait(_defaultConfigureAwait);

                         if (!exceptions.IsEmpty)
                         {
                             _errors.Enqueue(new AggregateException(message: $"Error in batch '{batchManager.BatchInfo?.Index ?? -1}', see inner.{Environment.NewLine}Info: {JsonSerializer.Serialize(batchManager.BatchInfo, _serializerOptions)}", innerExceptions: [.. exceptions.Where(x => x is not null)]));
                             FixMemoryLeak();
                         }
                     }
                     catch (Exception ex) when (ex is OperationCanceledException) { Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.Canceled); }
                     catch (Exception ex) { Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.WithErrors); _errors.Enqueue(ex); }
                 })
                 .ConfigureAwait(_defaultConfigureAwait);
             })
                .ConfigureAwait(_defaultConfigureAwait);

            #endregion
        }

        private async Task BatchHandleParallelAsync([NotNull] BatchManagerModel<TEntity> batchManager, [NotNull] Func<BatchItemInfoModel<TEntity>, Task> action, Func<bool>? skipHandle = null)
        {
            #region Batch Handle

            var skip = batchManager.BatchInfo is null || _config.CancellationToken.IsCancellationRequested || (skipHandle is not null && skipHandle());

            _printer.PrintDebug($"Step 2: BatchHandle | Thread: {Environment.CurrentManagedThreadId} | lane: {batchManager.BatchInfo?.LinePosition ?? -1} | Count: {batchManager.BatchInfo?.ItemsCount ?? -1} | Skip: {skip}.");

            if (skip)
                return;

            await _stats.CollectDurationAsync(async timestamp =>
             {
                 await batchManager.BatchInfo!.Collection
                 .ProcessInParallelWhenAllAsync(batchesInParallel: _config.BatchCount, degreeOfParallelism: _config is not { MaxDegreeOfParallelism: -1 } ? _config.MaxDegreeOfParallelism : null, partitionerOptions: EnumerablePartitionerOptions.NoBuffering, cancellationToken: _config.CancellationToken, body: async itemManager =>
                 //.ProcessInParallelWithPartitionerAsync(parallelOptions: _defaultParallelOptions, action: async itemManager =>
                 {
                     try
                     {
                         if (_config.CancellationToken.IsCancellationRequested || (skipHandle is not null && skipHandle()))
                             return;

                         if (itemManager.Index == 1 || percentileToShow.Any(quater => batchManager.BatchInfo.ItemsCount * quater == itemManager.Index))
                             _printer.PrintProgress(batchManager[itemManager.Index], TimeProvider.System.GetElapsedTime(timestamp));

                         var exceptions = await RetryExtension.RetryAsync(
                             action: async () => await action(itemManager).ConfigureAwait(_defaultConfigureAwait),
                             actionInError: (ex, tries) =>
                             {
                                 Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.WithErrors);
                                 _printer.PrintDebug($"[Tries: {tries}] => {ex.GetAggregatedMessages()}");
                             },
                             maxTries: _config.MaxTries, waitTime: _config.WaitTime)
                         .ConfigureAwait(_defaultConfigureAwait);

                         if (!exceptions.IsEmpty)
                         {
                             _errors.Enqueue(new AggregateException(message: $"Error in batch '{batchManager.BatchInfo?.Index ?? -1}', see inner.{Environment.NewLine}Info: {JsonSerializer.Serialize(batchManager.BatchInfo, _serializerOptions)}", innerExceptions: [.. exceptions.Where(x => x is not null)]));
                             FixMemoryLeak();
                         }
                     }
                     catch (Exception ex) when (ex is OperationCanceledException) { Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.Canceled); }
                     catch (Exception ex) { Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.WithErrors); _errors.Enqueue(ex); }
                 })
                 .ConfigureAwait(_defaultConfigureAwait);
             })
                .ConfigureAwait(_defaultConfigureAwait);

            #endregion
        }

        private async Task BatchHandleCollectionAsync([NotNull] BatchManagerModel<TEntity> batchManager, [NotNull] Func<IEnumerable<BatchItemInfoModel<TEntity>>, Task> action, Func<bool>? skipHandle = null)
        {
            #region Batch Handle
            var skip = batchManager.BatchInfo is null || _config.CancellationToken.IsCancellationRequested || (skipHandle is not null && skipHandle());

            _printer.PrintDebug($"Step 2: BatchHandle | Thread: {Environment.CurrentManagedThreadId} | Count: {batchManager.BatchInfo?.ItemsCount ?? -1} | Skip: {skip}.");

            if (skip)
                return;

            await _stats.CollectDurationAsync(async timestamp =>
             {
                 try
                 {
                     _printer.PrintProgress(batchManager.TotalProgress, TimeProvider.System.GetElapsedTime(timestamp));

                     var exceptions = await RetryExtension.RetryAsync(
                         action: async () => await action(batchManager.BatchInfo?.Collection.Span.ToImmutableArray() ?? []).ConfigureAwait(_defaultConfigureAwait),
                         actionInError: (ex, tries) =>
                         {
                             Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.WithErrors);
                             _printer.PrintDebug($"[Tries: {tries}] => {ex.GetAggregatedMessages()}");
                         },
                         maxTries: _config.MaxTries, waitTime: _config.WaitTime)
                     .ConfigureAwait(_defaultConfigureAwait);

                     if (!exceptions.IsEmpty)
                     {
                         _errors.Enqueue(new AggregateException(message: $"Error in batch '{batchManager.BatchInfo?.Index ?? -1}', see inner.{Environment.NewLine}Info: {JsonSerializer.Serialize(batchManager.BatchInfo, _serializerOptions)}", innerExceptions: [.. exceptions.Where(x => x is not null)]));
                         FixMemoryLeak();
                     }
                 }
                 catch (Exception ex) when (ex is OperationCanceledException) { Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.Canceled); }
                 catch (Exception ex) { Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.WithErrors); _errors.Enqueue(ex); }
             })
                .ConfigureAwait(_defaultConfigureAwait);

            #endregion
        }

        private async Task BatchHandleCollectionAsync([NotNull] BatchManagerModel<TEntity> batchManager, [NotNull] Func<IEnumerable<TEntity>, Task> action, Func<bool>? skipHandle = null)
        {
            #region Batch Handle
            var skip = batchManager.BatchInfo is null || _config.CancellationToken.IsCancellationRequested || (skipHandle is not null && skipHandle());

            _printer.PrintDebug($"Step 2: BatchHandle | Thread: {Environment.CurrentManagedThreadId} | Count: {batchManager.BatchInfo?.ItemsCount ?? -1} | Skip: {skip}.");

            if (skip)
                return;

            await _stats.CollectDurationAsync(async timestamp =>
             {
                 try
                 {
                     _printer.PrintProgress(batchManager.TotalProgress, TimeProvider.System.GetElapsedTime(timestamp));

                     var exceptions = await RetryExtension.RetryAsync(
                         action: async () => await action(batchManager.BatchInfo?.Collection.ToArray().Select(s => s.Data!).ToImmutableArray() ?? []).ConfigureAwait(_defaultConfigureAwait),
                         actionInError: (ex, tries) =>
                         {
                             Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.WithErrors);
                             _printer.PrintDebug($"[Tries: {tries}] => {ex.GetAggregatedMessages()}");
                         },
                         maxTries: _config.MaxTries, waitTime: _config.WaitTime)
                     .ConfigureAwait(_defaultConfigureAwait);

                     if (!exceptions.IsEmpty)
                     {
                         _errors.Enqueue(new AggregateException(message: $"Error in batch '{batchManager.BatchInfo?.Index ?? -1}', see inner.{Environment.NewLine}Info: {JsonSerializer.Serialize(batchManager.BatchInfo, _serializerOptions)}", innerExceptions: [.. exceptions.Where(x => x is not null)]));
                         FixMemoryLeak();
                     }
                 }
                 catch (Exception ex) when (ex is OperationCanceledException) { Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.Canceled); }
                 catch (Exception ex) { Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.WithErrors); _errors.Enqueue(ex); }
             })
                .ConfigureAwait(_defaultConfigureAwait);

            #endregion
        }

        private async Task BatchHandleCollectionAsync([NotNull] BatchManagerModel<TEntity> batchManager, [NotNull] Func<BatchInfoModel<TEntity>, Task> action, Func<bool>? skipHandle = null)
        {
            #region Batch Handle
            var skip = batchManager.BatchInfo is null || _config.CancellationToken.IsCancellationRequested || (skipHandle is not null && skipHandle());

            _printer.PrintDebug($"Step 2: BatchHandle | Thread: {Environment.CurrentManagedThreadId} | Count: {batchManager.BatchInfo?.ItemsCount ?? -1} | Skip: {skip}.");

            if (skip)
                return;

            await _stats.CollectDurationAsync(async timestamp =>
             {
                 try
                 {
                     _printer.PrintProgress(batchManager.TotalProgress, TimeProvider.System.GetElapsedTime(timestamp));

                     var exceptions = await RetryExtension.RetryAsync(
                         action: () => { action(batchManager.BatchInfo!); return Task.CompletedTask; },
                         actionInError: (ex, tries) =>
                         {
                             Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.WithErrors);
                             _printer.PrintDebug($"[Tries: {tries}] => {ex.GetAggregatedMessages()}");
                         },
                         maxTries: _config.MaxTries, waitTime: _config.WaitTime)
                     .ConfigureAwait(_defaultConfigureAwait);

                     if (!exceptions.IsEmpty)
                     {
                         _errors.Enqueue(new AggregateException(message: $"Error in batch '{batchManager.BatchInfo?.Index ?? -1}', see inner.{Environment.NewLine}Info: {JsonSerializer.Serialize(batchManager.BatchInfo, _serializerOptions)}", innerExceptions: [.. exceptions.Where(x => x is not null)]));
                         FixMemoryLeak();
                     }
                 }
                 catch (Exception ex) when (ex is OperationCanceledException) { Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.Canceled); }
                 catch (Exception ex) { Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.WithErrors); _errors.Enqueue(ex); }
             })
                .ConfigureAwait(_defaultConfigureAwait);

            #endregion
        }

        #endregion

        #region Sync BatchHandle

        private void BatchHandleSequential(BatchManagerModel<TEntity> batchManager, [NotNull] Action<TEntity> action, Func<bool>? skipHandle = null)
        {
            #region Batch Handle
            var skip = batchManager.BatchInfo is null || (skipHandle is not null && skipHandle());

            _printer.PrintDebug($"Step 2: BatchHandle | Thread: {Environment.CurrentManagedThreadId} | lane: {batchManager.BatchInfo?.LinePosition ?? -1} | Count: {batchManager.BatchInfo?.ItemsCount ?? -1} | Skip: {skip}.");

            if (skip)
                return;

            _stats.CollectDuration((timestamp) =>
            {
                batchManager.BatchInfo!.Collection.Span
                .IterateSpanAndUnsafe(itemManager =>
                {
                    try
                    {
                        if (itemManager.Index == 1 || percentileToShow.Any(quater => batchManager.BatchInfo.ItemsCount * quater == itemManager.Index))
                            _printer.PrintProgress(batchManager[itemManager.Index], TimeProvider.System.GetElapsedTime(timestamp));

                        var exceptions = RetryExtension.Retry(
                           action: () => action(itemManager.Data!),
                           actionInError: (ex, tries) =>
                           {
                               Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.WithErrors);
                               _printer.PrintDebug($"[Tries: {tries}] => {ex.GetAggregatedMessages()}");
                           },
                           maxTries: _config.MaxTries, waitTime: _config.WaitTime);

                        if (!exceptions.IsEmpty)
                        {
                            _errors.Enqueue(new AggregateException(message: $"Error in batch '{batchManager.BatchInfo.Index}', see inner.{Environment.NewLine}Info: {JsonSerializer.Serialize(itemManager, _serializerOptions)}", innerExceptions: [.. exceptions.Where(x => x is not null)]));
                            FixMemoryLeak();
                        }
                    }
                    catch (Exception ex) when (ex is OperationCanceledException) { Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.Canceled); }
                    catch (Exception ex) { Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.WithErrors); _errors.Enqueue(ex); }
                }, () => _config.CancellationToken.IsCancellationRequested || (skipHandle is not null && skipHandle()));
            });

            #endregion
        }

        private void BatchHandleSequential([NotNull] BatchManagerModel<TEntity> batchManager, [NotNull] Action<BatchItemInfoModel<TEntity>> action, Func<bool>? skipHandle = null)
        {
            #region Batch Handle
            var skip = batchManager.BatchInfo is null || (skipHandle is not null && skipHandle());

            _printer.PrintDebug($"Step 2: BatchHandle | Thread: {Environment.CurrentManagedThreadId} | lane: {batchManager.BatchInfo?.LinePosition ?? -1} | Count: {batchManager.BatchInfo?.ItemsCount ?? -1} | Skip: {skip}.");

            if (skip)
                return;

            _stats.CollectDuration((timestamp) =>
            {
                batchManager.BatchInfo!.Collection.Span
                .IterateSpanAndUnsafe(itemManager =>
                {
                    try
                    {
                        if (itemManager.Index == 1 || percentileToShow.Any(quater => batchManager.BatchInfo.ItemsCount * quater == itemManager.Index))
                            _printer.PrintProgress(batchManager[itemManager.Index], TimeProvider.System.GetElapsedTime(timestamp));

                        var exceptions = RetryExtension.Retry(
                           action: () => action(itemManager),
                           actionInError: (ex, tries) =>
                           {
                               Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.WithErrors);
                               _printer.PrintDebug($"[Tries: {tries}] => {ex.GetAggregatedMessages()}");
                           },
                           maxTries: _config.MaxTries, waitTime: _config.WaitTime);

                        if (!exceptions.IsEmpty)
                        {
                            _errors.Enqueue(new AggregateException(message: $"Error in batch '{batchManager.BatchInfo?.Index ?? -1}', see inner.{Environment.NewLine}Info: {JsonSerializer.Serialize(batchManager.BatchInfo, _serializerOptions)}", innerExceptions: [.. exceptions.Where(x => x is not null)]));
                            FixMemoryLeak();
                        }
                    }
                    catch (Exception ex) when (ex is OperationCanceledException) { Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.Canceled); }
                    catch (Exception ex) { Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.WithErrors); _errors.Enqueue(ex); }
                }, () => _config.CancellationToken.IsCancellationRequested || (skipHandle is not null && skipHandle()));
            });

            #endregion
        }

        private void BatchHandleParallel([NotNull] BatchManagerModel<TEntity> batchManager, [NotNull] Action<TEntity> action, Func<bool>? skipHandle = null)
        {
            #region Batch Handle
            var skip = batchManager.BatchInfo is null || (skipHandle is not null && skipHandle());

            _printer.PrintDebug($"Step 2: BatchHandle | Thread: {Environment.CurrentManagedThreadId} | lane: {batchManager.BatchInfo?.LinePosition ?? -1} | Count: {batchManager.BatchInfo?.ItemsCount ?? -1} | Skip: {skip}.");

            if (skip)
                return;

            _stats.CollectDuration((timestamp) =>
            {
                batchManager.BatchInfo!.Collection.Span
                .ProcessInParallelWhenAllAsync(batchesInParallel: _config.BatchCount, degreeOfParallelism: _config is not { MaxDegreeOfParallelism: -1 } ? _config.MaxDegreeOfParallelism : null, partitionerOptions: EnumerablePartitionerOptions.NoBuffering, cancellationToken: _config.CancellationToken, body: itemManager =>
                //.ProcessInParallelWithPartitionerAsync(parallelOptions: _defaultParallelOptions, action: itemManager =>
                {
                    try
                    {
                        if (_config.CancellationToken.IsCancellationRequested || (skipHandle is not null && skipHandle()))
                            return Task.CompletedTask;

                        if (itemManager.Index == 1 || percentileToShow.Any(quater => batchManager.BatchInfo.ItemsCount * quater == itemManager.Index))
                            _printer.PrintProgress(batchManager[itemManager.Index], TimeProvider.System.GetElapsedTime(timestamp));

                        var exceptions = RetryExtension.Retry(
                           action: () => action(itemManager.Data!),
                           actionInError: (ex, tries) =>
                           {
                               Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.WithErrors);
                               _printer.PrintDebug($"[Tries: {tries}] => {ex.GetAggregatedMessages()}");
                           },
                           maxTries: _config.MaxTries, waitTime: _config.WaitTime);

                        if (!exceptions.IsEmpty)
                        {
                            _errors.Enqueue(new AggregateException(message: $"Error in batch '{batchManager.BatchInfo.Index}', see inner.{Environment.NewLine}Info: {JsonSerializer.Serialize(itemManager, _serializerOptions)}", innerExceptions: [.. exceptions.Where(x => x is not null)]));
                            FixMemoryLeak();
                        }
                    }
                    catch (Exception ex) when (ex is OperationCanceledException) { Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.Canceled); }
                    catch (Exception ex) { Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.WithErrors); _errors.Enqueue(ex); }

                    return Task.CompletedTask;
                })
                .Sync(_defaultConfigureAwait);
            });

            #endregion
        }

        private void BatchHandleParallel([NotNull] BatchManagerModel<TEntity> batchManager, [NotNull] Action<BatchItemInfoModel<TEntity>> action, Func<bool>? skipHandle = null)
        {
            #region Batch Handle
            var skip = batchManager.BatchInfo is null || (skipHandle is not null && skipHandle());

            _printer.PrintDebug($"Step 2: BatchHandle | Thread: {Environment.CurrentManagedThreadId} | lane: {batchManager.BatchInfo?.LinePosition ?? -1} | Count: {batchManager.BatchInfo?.ItemsCount ?? -1} | Skip: {skip}.");

            if (skip)
                return;

            _stats.CollectDuration((timestamp) =>
            {
                batchManager.BatchInfo!.Collection.Span
                .ProcessInParallelWhenAllAsync(batchesInParallel: _config.BatchCount, degreeOfParallelism: _config is not { MaxDegreeOfParallelism: -1 } ? _config.MaxDegreeOfParallelism : null, partitionerOptions: EnumerablePartitionerOptions.NoBuffering, cancellationToken: _config.CancellationToken, body: itemManager =>
                //.ProcessInParallelWithPartitionerAsync(parallelOptions: _defaultParallelOptions, action: itemManager =>
                {
                    try
                    {
                        if (_config.CancellationToken.IsCancellationRequested || (skipHandle is not null && skipHandle()))
                            return Task.CompletedTask;

                        if (itemManager.Index == 1 || percentileToShow.Any(quater => batchManager.BatchInfo.ItemsCount * quater == itemManager.Index))
                            _printer.PrintProgress(batchManager[itemManager.Index], TimeProvider.System.GetElapsedTime(timestamp));

                        var exceptions = RetryExtension.Retry(
                           action: () => action(itemManager),
                           actionInError: (ex, tries) =>
                           {
                               Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.WithErrors);
                               _printer.PrintDebug($"[Tries: {tries}] => {ex.GetAggregatedMessages()}");
                           },
                           maxTries: _config.MaxTries, waitTime: _config.WaitTime);

                        if (!exceptions.IsEmpty)
                        {
                            _errors.Enqueue(new AggregateException(message: $"Error in batch '{batchManager.BatchInfo?.Index ?? -1}', see inner.{Environment.NewLine}Info: {JsonSerializer.Serialize(batchManager.BatchInfo, _serializerOptions)}", innerExceptions: [.. exceptions.Where(x => x is not null)]));
                            FixMemoryLeak();
                        }
                    }
                    catch (Exception ex) when (ex is OperationCanceledException) { Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.Canceled); }
                    catch (Exception ex) { Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.WithErrors); _errors.Enqueue(ex); }

                    return Task.CompletedTask;
                })
                .Sync(_defaultConfigureAwait);
            });

            #endregion
        }

        private void BatchHandleCollection([NotNull] BatchManagerModel<TEntity> batchManager, [NotNull] Action<IEnumerable<BatchItemInfoModel<TEntity>>> action, Func<bool>? skipHandle = null)
        {
            #region Batch Handle
            var skip = batchManager.BatchInfo is null || (skipHandle is not null && skipHandle());

            _printer.PrintDebug($"Step 2: BatchHandle | Thread: {Environment.CurrentManagedThreadId} | Count: {batchManager.BatchInfo?.ItemsCount ?? -1} | Skip: {skip}.");

            if (skip)
                return;

            _stats.CollectDuration((timestamp) =>
            {
                try
                {
                    _printer.PrintProgress(batchManager.TotalProgress, TimeProvider.System.GetElapsedTime(timestamp));

                    var exceptions = RetryExtension.Retry(
                       action: () => action(batchManager.BatchInfo?.Collection.Span.ToImmutableArray() ?? []),
                       actionInError: (ex, tries) =>
                       {
                           Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.WithErrors);
                           _printer.PrintDebug($"[Tries: {tries}] => {ex.GetAggregatedMessages()}");
                       },
                       maxTries: _config.MaxTries, waitTime: _config.WaitTime);

                    if (!exceptions.IsEmpty)
                    {
                        _errors.Enqueue(new AggregateException(message: $"Error in batch '{batchManager.BatchInfo?.Index ?? -1}', see inner.{Environment.NewLine}Info: {JsonSerializer.Serialize(batchManager.BatchInfo, _serializerOptions)}", innerExceptions: [.. exceptions.Where(x => x is not null)]));
                        FixMemoryLeak();
                    }
                }
                catch (Exception ex) when (ex is OperationCanceledException) { Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.Canceled); }
                catch (Exception ex) { Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.WithErrors); _errors.Enqueue(ex); }
            });

            #endregion
        }

        private void BatchHandleCollection([NotNull] BatchManagerModel<TEntity> batchManager, [NotNull] Action<IEnumerable<TEntity>> action, Func<bool>? skipHandle = null)
        {
            #region Batch Handle
            var skip = batchManager.BatchInfo is null || (skipHandle is not null && skipHandle());

            _printer.PrintDebug($"Step 2: BatchHandle | Thread: {Environment.CurrentManagedThreadId} | Count: {batchManager.BatchInfo?.ItemsCount ?? -1} | Skip: {skip}.");

            if (skip)
                return;

            _stats.CollectDuration((timestamp) =>
            {
                try
                {
                    _printer.PrintProgress(batchManager.TotalProgress, TimeProvider.System.GetElapsedTime(timestamp));

                    var exceptions = RetryExtension.Retry(
                       action: () => action(batchManager.BatchInfo?.Collection.ToArray().Select(s => s.Data!).ToImmutableArray() ?? []),
                       actionInError: (ex, tries) =>
                       {
                           Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.WithErrors);
                           _printer.PrintDebug($"[Tries: {tries}] => {ex.GetAggregatedMessages()}");
                       },
                       maxTries: _config.MaxTries, waitTime: _config.WaitTime);

                    if (!exceptions.IsEmpty)
                    {
                        _errors.Enqueue(new AggregateException(message: $"Error in batch '{batchManager.BatchInfo?.Index ?? -1}', see inner.{Environment.NewLine}Info: {JsonSerializer.Serialize(batchManager.BatchInfo, _serializerOptions)}", innerExceptions: [.. exceptions.Where(x => x is not null)]));
                        FixMemoryLeak();
                    }
                }
                catch (Exception ex) when (ex is OperationCanceledException) { Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.Canceled); }
                catch (Exception ex) { Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.WithErrors); _errors.Enqueue(ex); }
            });

            #endregion
        }

        private void BatchHandleCollection([NotNull] BatchManagerModel<TEntity> batchManager, [NotNull] Action<BatchInfoModel<TEntity>> action, Func<bool>? skipHandle = null)
        {
            #region Batch Handle
            var skip = skipHandle is not null && skipHandle();

            _printer.PrintDebug($"Step 2: BatchHandle | Thread: {Environment.CurrentManagedThreadId} | Count: {batchManager.BatchInfo?.ItemsCount!} | Skip: {skip}.");

            if (skip)
                return;

            _stats.CollectDuration((timestamp) =>
            {
                try
                {
                    _printer.PrintProgress(batchManager.TotalProgress, TimeProvider.System.GetElapsedTime(timestamp));

                    var exceptions = RetryExtension.Retry(
                       action: () => action(batchManager.BatchInfo!),
                       actionInError: (ex, tries) =>
                       {
                           Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.WithErrors);
                           _printer.PrintDebug($"[Tries: {tries}] => {ex.GetAggregatedMessages()}");
                       },
                       maxTries: _config.MaxTries, waitTime: _config.WaitTime);

                    if (!exceptions.IsEmpty)
                    {
                        _errors.Enqueue(new AggregateException(message: $"Error in batch '{batchManager.BatchInfo?.Index ?? -1}', see inner.{Environment.NewLine}Info: {JsonSerializer.Serialize(batchManager.BatchInfo, _serializerOptions)}", innerExceptions: [.. exceptions.Where(x => x is not null)]));
                        FixMemoryLeak();
                    }
                }
                catch (Exception ex) when (ex is OperationCanceledException) { Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.Canceled); }
                catch (Exception ex) { Interlocked.Exchange(ref _status, (int)BatcherBuilderStatus.WithErrors); _errors.Enqueue(ex); }
            });

            #endregion
        }

        #endregion

        private BatcherBuilderResult Executor(Func<bool>? skipHandle)
        {
            if (_buffer is null)
                StartPipeline(skipHandle);
            else
                StartPipelineWithBuffer(skipHandle);

            var result = new BatcherBuilderResult(Status, _errors);
            return result;
        }

        private Task<BatcherBuilderResult> ExecutorAsync(Func<bool>? skipHandle) => (_buffer is null ?
            StartPipelineAsync(skipHandle) :
            StartPipelineWithBufferAsync(skipHandle))
            .ContinueWith(_ =>
            {
                var result = new BatcherBuilderResult(Status, _errors);
                return result;
            });

        private BatchManagerModel<TEntity> MountBatchManager(IEnumerable<TEntity> items)
        {
            _currentManager = _config.PrintProgress ?
               MountBatchManagerWithProgress(items) :
               MountBatchManagerWithoutProgress(items);

            return _currentManager;
        }

        private async Task<BatchManagerModel<TEntity>> MountBatchManagerAsync(IEnumerable<TEntity> items)
        {
            _currentManager = _config.PrintProgress ?
               await MountBatchManagerWithProgressAsync(items).ConfigureAwait(_defaultConfigureAwait) :
               await MountBatchManagerWithoutProgressAsync(items).ConfigureAwait(_defaultConfigureAwait);

            return _currentManager;
        }

        private BatchManagerModel<TEntity> MountBatchManagerWithProgress(IEnumerable<TEntity> items)
        {
            // this is mandatory to show all batches process in begin.
            var (result, errors) = _semaphore.HandleConcurencyMultithread(TimeSpan.FromSeconds(2), () =>
            {
                Interlocked.Increment(ref _currentBatch);

                var threadId = Environment.CurrentManagedThreadId;
                var lane = _currentBatch % _config.MaxDegreeOfParallelism;

                //if (lane == 0)
                //    lane = _config.MaxDegreeOfParallelism;
                _ = Interlocked.CompareExchange(ref lane, _config.MaxDegreeOfParallelism, 0); //The same logic as the 'if' above in an atomic operation.


                //var managerDto = new BatchManagerModel<TEntity>(_semaphore, items ?? _emptyItems, _currentBatch, _config.BatchCount, lane, _defaultParallelOptions);
                var managerDto = new BatchManagerModel<TEntity>(_semaphore, items ?? _emptyItems, _currentBatch, _config.BatchCount, _config, lane);

                _printer.PrintDebug($"Step 1: Batch arrived, mount BatchManager | Thread: {threadId} | Lane: {lane}");

                return managerDto;
            });

            if (errors is { IsEmpty: false })
                throw new AggregateException($"Error in {nameof(MountBatchManagerWithProgress)}, see inner exceptions for more details.", errors);

            return result;
        }

        private Task<BatchManagerModel<TEntity>> MountBatchManagerWithProgressAsync(IEnumerable<TEntity> items)
        {
            Interlocked.Increment(ref _currentBatch);

            var threadId = Environment.CurrentManagedThreadId;
            var lane = _currentBatch % _config.MaxDegreeOfParallelism;

            //if (lane == 0)
            //    lane = _config.MaxDegreeOfParallelism;
            _ = Interlocked.CompareExchange(ref lane, _config.MaxDegreeOfParallelism, 0); //The same logic as the 'if' above in an atomic operation.

            //var managerDto = new BatchManagerModel<TEntity>(_semaphore, items ?? _emptyItems, _currentBatch, _config.BatchCount, lane, _defaultParallelOptions);
            var managerDto = new BatchManagerModel<TEntity>(_semaphore, items ?? _emptyItems, _currentBatch, _config.BatchCount, _config, lane);

            _printer.PrintDebug($"Step 1: Batch arrived, mount BatchManager | Thread: {threadId} | Lane: {lane}");

            return Task.FromResult(managerDto);
        }

        private BatchManagerModel<TEntity> MountBatchManagerWithoutProgress(IEnumerable<TEntity> items)
        {
            Interlocked.Increment(ref _currentBatch);

            var threadId = Environment.CurrentManagedThreadId;
            var lane = 0;

            //var managerDto = new BatchManagerModel<TEntity>(_semaphore, items ?? _emptyItems, _currentBatch, _config.BatchCount, lane, _defaultParallelOptions);
            var managerDto = new BatchManagerModel<TEntity>(_semaphore, items ?? _emptyItems, _currentBatch, _config.BatchCount, _config, lane);

            _printer.PrintDebug($"Step 1: Batch arrived, mount BatchManager | Thread: {threadId} | Lane: {lane}");

            return managerDto;
        }

        private Task<BatchManagerModel<TEntity>> MountBatchManagerWithoutProgressAsync(IEnumerable<TEntity> items)
        {
            Interlocked.Increment(ref _currentBatch);

            var threadId = Environment.CurrentManagedThreadId;
            var lane = 0;

            //var managerDto = new BatchManagerModel<TEntity>(_semaphore, items ?? _emptyItems, _currentBatch, _config.BatchCount, lane, _defaultParallelOptions);
            var managerDto = new BatchManagerModel<TEntity>(_semaphore, items ?? _emptyItems, _currentBatch, _config.BatchCount, _config, lane);

            _printer.PrintDebug($"Step 1: Batch arrived, mount BatchManager | Thread: {threadId} | Lane: {lane}");

            return Task.FromResult(managerDto);
        }

        private void Release()
        {
            _batchTimeForcer?.Dispose();
            _batchTimeForcer = null;

            _buffer?.Complete();
            _batcher?.TriggerBatch();
            _actionHandle?.Complete();
            _batcher?.Complete();

            _buffer = null;
            _batcher = default!;
            _actionHandle = default!;

            _finalBatchTriggered = false;
            _currentBatch = 0;

            FixMemoryLeak();
        }

        private static int CaculateBatchIndex(int line, int intervalRange, int columnSelector = 0)
        {
            if (columnSelector < 0)
                columnSelector = 0;

            var result = columnSelector * intervalRange + line;
            return result;
        }

        #endregion

        #endregion
    }
}