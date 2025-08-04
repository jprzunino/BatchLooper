using BatchLooper.Core.Models.Batcher;
using System.Diagnostics.CodeAnalysis;

namespace BatchLooper.Core.Interfaces.Batcher.Engine
{
    public interface IBatchExecutionEngineItem<TEntity>
    {
        #region Async Execution

        /// <summary>
        ///  Interate over batch in source collection.
        /// </summary>
        /// <param name="handle">Async function with item to be processed.</param>
        /// <param name="skipHandle">Skip interation if Post/SendAsync batch/Buffer or interate that.</param>
        /// <param name="cancellationToken">Token to cancellate process, this use skiphandle.</param>
        /// <returns>Collection of all exception, had one, otherwise return empty collection and status of execution.</returns>
        Task<BatcherBuilderResult> ExecuteAsync([NotNull] Func<TEntity, Task> handler, Func<bool>? skipHandle = null, CancellationToken cancellationToken = default);

        /// <summary>
        ///  Interate over batch in source collection.
        /// </summary>
        /// <param name="handle">Async function with item to be processed.</param>
        /// <param name="skipHandle">Skip interation if Post/SendAsync batch/Buffer or interate that.</param>
        /// <param name="cancellationToken">Token to cancellate process, this use skiphandle.</param>
        /// <returns>Collection of all exception, had one, otherwise return empty collection and status of execution.</returns>
        Task<BatcherBuilderResult> ExecuteWithBatchItemInfoAsync([NotNull] Func<BatchItemInfoModel<TEntity>, Task> handler, Func<bool>? skipHandle = null, CancellationToken cancellationToken = default);

        #endregion

        #region Sync Execution

        /// <summary>
        ///  Interate over batch in source collection.
        /// </summary>
        /// <param name="handle">Action with item to be processed.</param>
        /// <param name="skipHandle">Skip interation if Post/SendAsync batch/Buffer or interate that.</param>
        /// <param name="cancellationToken">Token to cancellate process, this use skiphandle.</param>
        /// <returns>Collection of all exception, had one, otherwise return empty collection and status of execution.</returns>
        BatcherBuilderResult Execute([NotNull] Action<TEntity> handler, Func<bool>? skipHandle = null);

        /// <summary>
        ///  Interate over batch in source collection.
        /// </summary>
        /// <param name="handle">Action with item to be processed.</param>
        /// <param name="skipHandle">Skip interation if Post/SendAsync batch/Buffer or interate that.</param>
        /// <param name="cancellationToken">Token to cancellate process, this use skiphandle.</param>
        /// <returns>Collection of all exception, had one, otherwise return empty collection and status of execution.</returns>
        BatcherBuilderResult ExecuteWithBatchItemInfo([NotNull] Action<BatchItemInfoModel<TEntity>> handler, Func<bool>? skipHandle = null);

        #endregion
    }
}
