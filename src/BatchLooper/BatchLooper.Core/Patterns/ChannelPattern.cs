using BatchLooper.Core.Interfaces.Patterns;
using BatchLooper.Core.Models.Patterns;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using static BatchLooper.Core.Helpers.IterateHelper;
using static BatchLooper.Core.Helpers.SyncTaskHelper;

namespace BatchLooper.Core.Patterns
{
    [DebuggerDisplay("ChannelPattern: QueueSize = {_channel.Reader.Count} | IsConsumerCompleted = {_channel.Reader.Completion.IsCompleted} | IsProducerCompleted = {_isProducerCompleted} | NoHasError = {_errors.IsEmpty} | LastError = {LastErrorMessage,nq}")]
    public class ChannelPattern<TEntity> : IChannelPattern<TEntity>
        where TEntity : notnull
    {
        #region Constants

        #endregion

        #region Variables

        private readonly Channel<TEntity> _channel;
        private readonly ConcurrentQueue<ChannelErrorModel<IEnumerable<TEntity>>> _errors;
        private readonly IChannelPattern<TEntity> _recursive;
        private bool _isProducerCompleted;

        #endregion

        #region Properties

        ConcurrentQueue<ChannelErrorModel<IEnumerable<TEntity>>> IChannelPattern<TEntity>.Errors => _errors;

        /// <summary>
        /// Used to print last error message in debug on channel object.
        /// </summary>
        private string? LastErrorMessage => _errors.TryPeek(out var dto) ? dto?.Error?.Message : null;

        #endregion

        #region Initializers

        public ChannelPattern() : this(Channel.CreateUnbounded<TEntity>()) { }

        public ChannelPattern([NotNull] Channel<TEntity> channel)
        {
            _channel = channel ?? throw new ArgumentNullException(nameof(channel));
            _errors = new ConcurrentQueue<ChannelErrorModel<IEnumerable<TEntity>>>();
            _recursive = this;
        }

        public ChannelPattern([NotNull] BoundedChannelOptions options)
            : this(Channel.CreateBounded<TEntity>(options ?? throw new ArgumentNullException(nameof(options)))) { }

        public ChannelPattern([NotNull] UnboundedChannelOptions options)
            : this(Channel.CreateUnbounded<TEntity>(options ?? throw new ArgumentNullException(nameof(options)))) { }

        public ChannelPattern([NotNull] UnboundedPrioritizedChannelOptions<TEntity> options)
            : this(Channel.CreateUnboundedPrioritized<TEntity>(options ?? throw new ArgumentNullException(nameof(options)))) { }

        #endregion


        #region Function And Routines

        #region Publics

        #region Async

        async Task IChannelPattern<TEntity>.ConsumerAsync([NotNull] Func<TEntity, Task> action, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(_channel);
            ArgumentNullException.ThrowIfNull(action);

            TEntity? item = default;
            try
            {
                if (await _channel.Reader.WaitToReadAsync(cancellationToken))
                {
                    item = await _channel.Reader.ReadAsync(cancellationToken);

                    if (item is not null)
                        await action(item);
                }
            }
            catch (Exception ex)
            {
                if (ex is not OperationCanceledException)
                    _errors.Enqueue(new ChannelErrorModel<IEnumerable<TEntity>>(item is not null ? [item] : [], ex));
            }
        }

        async Task IChannelPattern<TEntity>.ConsumerAsync([NotNull] Func<IEnumerable<TEntity>, Task> action, int? size, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(_channel);
            ArgumentNullException.ThrowIfNull(action);

            IEnumerable<TEntity> collection = [];

            try
            {
                if (await _channel.Reader.WaitToReadAsync(cancellationToken))
                {
                    collection = [.. (size.HasValue && size > 0 ?
                        Enumerable.Range(1, Math.Min(size.Value, _channel.Reader.Count))
                            .Select(_ => _channel.Reader.ReadAsync(cancellationToken).AsTask().Sync()) :
                        _channel.Reader.ReadAllAsync(cancellationToken).ToBlockingEnumerable(cancellationToken))];

                    await action(collection);
                }
            }
            catch (Exception ex)
            {
                if (ex is not OperationCanceledException)
                    _errors.Enqueue(new ChannelErrorModel<IEnumerable<TEntity>>(collection, ex));
            }
        }

        async Task IChannelPattern<TEntity>.ProducerAsync(TEntity entity, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(_channel);

            try
            {
                if (entity is not null)
                    await _channel.Writer.WriteAsync(entity, cancellationToken);
            }
            catch (Exception ex)
            {
                if (ex is not OperationCanceledException)
                    _errors.Enqueue(new ChannelErrorModel<IEnumerable<TEntity>>(entity is not null ? [entity] : [], ex));
            }
        }

        async Task IChannelPattern<TEntity>.ProducerAsync([NotNull] IEnumerable<TEntity> collection, CancellationToken cancellation)
        {
            ArgumentNullException.ThrowIfNull(collection);

            try
            {
                collection
                    .IterateSpanAndUnsafe(
                        async item => await _recursive.ProducerAsync(item, cancellation),
                        () => cancellation.IsCancellationRequested);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                if (ex is not OperationCanceledException)
                    _errors.Enqueue(new ChannelErrorModel<IEnumerable<TEntity>>(collection, ex));
            }
        }

        async Task IChannelPattern<TEntity>.ProducerAsync([NotNull] Func<Task<TEntity>> action, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(_channel);
            ArgumentNullException.ThrowIfNull(action);

            TEntity entity = default!;

            try
            {
                entity = await action();
                await _recursive.ProducerAsync(entity, cancellationToken);
            }
            catch (Exception ex)
            {
                if (ex is not OperationCanceledException)
                    _errors.Enqueue(new ChannelErrorModel<IEnumerable<TEntity>>(entity is not null ? [entity] : [], ex));
            }
        }

        async Task IChannelPattern<TEntity>.StartResilientConsumerAsync([NotNull] Func<TEntity, Task> action, [NotNull] TimeSpan waitBetweenTries, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await _recursive.ConsumerAsync(action, cancellationToken);
                }
                catch (Exception ex)
                {
                    if (ex is not OperationCanceledException)
                    {
                        _errors.Enqueue(new ChannelErrorModel<IEnumerable<TEntity>>([], ex));
                        await Task.Delay(waitBetweenTries, cancellationToken);
                    }
                }
            }
        }

        async Task IChannelPattern<TEntity>.StartResilientConsumerAsync([NotNull] Func<IEnumerable<TEntity>, Task> action, [NotNull] TimeSpan waitBetweenTries, int? size, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await _recursive.ConsumerAsync(action, size, cancellationToken);
                }
                catch (Exception ex)
                {
                    if (ex is not OperationCanceledException)
                    {
                        _errors.Enqueue(new ChannelErrorModel<IEnumerable<TEntity>>([], ex));
                        await Task.Delay(waitBetweenTries, cancellationToken);
                    }
                }
            }
        }

        #endregion

        #region Managers

        bool IChannelPattern<TEntity>.CompleteProducer() => _isProducerCompleted = _channel.Writer.TryComplete();

        void IChannelPattern<TEntity>.WaitUtilConsumerCompletion() => Task.WaitAll(_channel.Reader.Completion);

        async Task IChannelPattern<TEntity>.WaitUtilConsumerCompletionAsync() => await _channel.Reader.Completion;

        #endregion

        #region Sync

        void IChannelPattern<TEntity>.Consumer([NotNull] Action<TEntity> action)
        {
            ArgumentNullException.ThrowIfNull(_channel);
            ArgumentNullException.ThrowIfNull(action);

            TEntity? item = default;
            try
            {
                if (_channel.Reader.WaitToReadAsync().AsTask().Sync() && _channel.Reader.TryRead(out item) && item is not null)
                    action(item);
            }
            catch (Exception ex)
            {
                if (ex is not OperationCanceledException)
                    _errors.Enqueue(new ChannelErrorModel<IEnumerable<TEntity>>(item is not null ? [item] : [], ex));
            }
        }

        void IChannelPattern<TEntity>.Consumer([NotNull] Action<IEnumerable<TEntity>> action, int? size)
        {
            ArgumentNullException.ThrowIfNull(_channel);
            ArgumentNullException.ThrowIfNull(action);

            IEnumerable<TEntity> collection = [];
            try
            {
                if (_channel.Reader.WaitToReadAsync().AsTask().Sync())
                {
                    collection = [.. (size.HasValue && size > 0 ?
                        Enumerable.Range(1, Math.Min(size.Value, _channel.Reader.Count))
                            .Select(_ => _channel.Reader.ReadAsync().AsTask().Sync()) :
                        _channel.Reader.ReadAllAsync().ToBlockingEnumerable())];

                    action(collection);
                }
            }
            catch (Exception ex)
            {
                if (ex is not OperationCanceledException)
                    _errors.Enqueue(new ChannelErrorModel<IEnumerable<TEntity>>(collection, ex));
            }
        }

        void IChannelPattern<TEntity>.Producer(TEntity entity)
        {
            ArgumentNullException.ThrowIfNull(_channel);

            try
            {
                if (entity is not null)
                    _channel.Writer.WriteAsync(entity)
                        .AsTask()
                        .Sync();
            }
            catch (Exception ex)
            {
                if (ex is not OperationCanceledException)
                    _errors.Enqueue(new ChannelErrorModel<IEnumerable<TEntity>>(entity is not null ? [entity] : [], ex));
            }
        }

        void IChannelPattern<TEntity>.Producer([NotNull] IEnumerable<TEntity> collection)
        {
            ArgumentNullException.ThrowIfNull(collection);

            try
            {
                collection
                    .IterateSpanAndUnsafe(item => _recursive.Producer(item));
            }
            catch (Exception ex)
            {
                if (ex is not OperationCanceledException)
                    _errors.Enqueue(new ChannelErrorModel<IEnumerable<TEntity>>(collection, ex));
            }
        }

        void IChannelPattern<TEntity>.Producer([NotNull] Func<TEntity> action)
        {
            ArgumentNullException.ThrowIfNull(action);

            TEntity entity = default!;

            try
            {
                entity = action();
                _recursive.Producer(entity);
            }
            catch (Exception ex)
            {
                if (ex is not OperationCanceledException)
                    _errors.Enqueue(new ChannelErrorModel<IEnumerable<TEntity>>(entity is not null ? [entity] : [], ex));
            }
        }

        #endregion

        #endregion

        #region Privates

        #endregion

        #endregion
    }
}
