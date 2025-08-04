using BatchLooper.Core.Models.Patterns;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace BatchLooper.Core.Interfaces.Patterns
{
    public interface IChannelPattern<TEntity>
          where TEntity : notnull
    {
        /// <summary>
        /// Errors that occur in channel producer or consumer.
        /// </summary>
        ConcurrentQueue<ChannelErrorModel<IEnumerable<TEntity>>> Errors { get; }

        /// <summary>
        /// Produces an item in the channel.
        /// </summary>
        /// <param name="entity">Item to be produced.</param>
        void Producer(TEntity entity);

        /// <summary>
        /// Produces a collection of items in the channel.
        /// </summary>
        /// <param name="collection">Collection of items to be produced.</param>
        void Producer([NotNull] IEnumerable<TEntity> collection);

        /// <summary>
        /// Produces an item in the channel from a function.
        /// </summary>
        /// <param name="action">Function that returns the item to be produced.</param>
        void Producer([NotNull] Func<TEntity> action);

        /// <summary>
        /// Produces an item in the channel asynchronously.
        /// </summary>
        /// <param name="entity">Item to be produced.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task ProducerAsync(TEntity entity, CancellationToken cancellation = default);

        /// <summary>
        /// Produces a collection of items in the channel asynchronously.
        /// </summary>
        /// <param name="collection">Collection of items to be produced.</param>
        /// <param name="cancellation">Cancellation token.</param>
        Task ProducerAsync([NotNull] IEnumerable<TEntity> collection, CancellationToken cancellation = default);

        /// <summary>
        /// Produces an item in the channel asynchronously from a function.
        /// </summary>
        /// <param name="action">Asynchronous function that returns the item.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task ProducerAsync([NotNull] Func<Task<TEntity>> action, CancellationToken cancellationToken = default);

        /// <summary>
        /// Consumes an item from the channel and executes the provided action.
        /// </summary>
        /// <param name="action">Action to be executed with the consumed item.</param>
        void Consumer([NotNull] Action<TEntity> action);

        /// <summary>
        /// Consumes a collection of items from the channel and executes the provided action.
        /// </summary>
        /// <param name="action">Action to be executed with the consumed items.</param>
        /// <param name="size">Maximum number of items to consume.</param>
        void Consumer([NotNull] Action<IEnumerable<TEntity>> action, int? size = null);

        /// <summary>
        /// Consumes an item from the channel asynchronously.
        /// </summary>
        /// <param name="action">Asynchronous function to be executed with the item.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task ConsumerAsync([NotNull] Func<TEntity, Task> action, CancellationToken cancellationToken = default);

        /// <summary>
        /// Consumes a collection of items from the channel asynchronously.
        /// </summary>
        /// <param name="action">Asynchronous function to be executed with the items.</param>
        /// <param name="size">Maximum number of items to consume.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task ConsumerAsync([NotNull] Func<IEnumerable<TEntity>, Task> action, int? size = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Starts a resilient consumer that automatically restarts after failures.
        /// </summary>
        /// <param name="action">Asynchronous function to be executed with the item.</param>
        /// <param name="waitBetweenTries">Wait time between attempts.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task StartResilientConsumerAsync([NotNull] Func<TEntity, Task> action, [NotNull] TimeSpan waitBetweenTries, CancellationToken cancellationToken = default);

        /// <summary>
        /// Starts a resilient consumer for collections that automatically restarts after failures.
        /// </summary>
        /// <param name="action">Asynchronous function to be executed with the items.</param>
        /// <param name="waitBetweenTries">Wait time between attempts.</param>
        /// <param name="size">Maximum number of items to consume.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task StartResilientConsumerAsync([NotNull] Func<IEnumerable<TEntity>, Task> action, [NotNull] TimeSpan waitBetweenTries, int? size = null, CancellationToken cancellationToken = default);

        /// <summary>Attempts to mark the channel as being completed, meaning no more data will be written to it.</summary>
        /// <returns>
        /// true if this operation successfully completes the channel; otherwise, false if the channel could not be marked for completion,
        /// for example due to having already been marked as such, or due to not supporting completion.
        /// </returns>
        bool CompleteProducer();

        /// <summary>
        /// Waits for the completion of all consumers synchronously.
        /// </summary>
        void WaitUtilConsumerCompletion();

        /// <summary>
        /// Waits for the completion of all consumers asynchronously.
        /// </summary>
        Task WaitUtilConsumerCompletionAsync();
    }
}
