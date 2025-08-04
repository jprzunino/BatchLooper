using BatchLooper.Core.Models.Batcher;
using System.Diagnostics.CodeAnalysis;

namespace BatchLooper.Core.Interfaces.Batcher.Engine
{
    public interface IBatchExecutionEngineCollection<TEntity>
    {
        #region Async Execution

        /// <summary>
        /// Interate over batch in source collection.
        /// </summary>
        /// <param name="handle">Async function with items to be processed. with items to be processed.</param>
        /// <param name="skipHandle">Skip interation if Post/SendAsync batch/Buffer or interate that.</param>
        /// <param name="cancellationToken">Token to cancellate process, this use skiphandle.</param>
        /// <returns>CCollection of all exception, had one, otherwise return empty collection and status of execution.</returns>
        Task<BatcherBuilderResult> ExecuteCollectionAsync([NotNull] Func<IEnumerable<TEntity>, Task> handler, Func<bool>? skipHandle = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Interate over batch in source collection.
        /// </summary>
        /// <param name="handle">Async function with items to be processed.</param>
        /// <param name="skipHandle">Skip interation if Post/SendAsync batch/Buffer or interate that.</param>
        /// <param name="cancellationToken">Token to cancellate process, this use skiphandle.</param>
        /// <returns>Collection of all exception, had one, otherwise return empty collection and status of execution.</returns>
        Task<BatcherBuilderResult> ExecuteWithBatchItemInfoCollectionAsync([NotNull] Func<IEnumerable<BatchItemInfoModel<TEntity>>, Task> handler, Func<bool>? skipHandle = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Interate over batch in source collection.
        /// </summary>
        /// <param name="handle">Async function with items to be processed.</param>
        /// <param name="skipHandle">Skip interation if Post/SendAsync batch/Buffer or interate that.</param>
        /// <param name="cancellationToken">Token to cancellate process, this use skiphandle.</param>
        /// <returns>Collection of all exception, had one, otherwise return empty collection and status of execution.</returns>
        Task<BatcherBuilderResult> ExecuteWithBatchInfoCollectionAsync([NotNull] Func<BatchInfoModel<TEntity>, Task> handler, Func<bool>? skipHandle = null, CancellationToken cancellationToken = default);

        #endregion

        #region Sync Execution

        /// <summary>
        /// Interate over batch in source collection.
        /// </summary>
        /// <param name="handle">Action with items to be processed. with items to be processed.</param>
        /// <param name="skipHandle">Skip interation if Post/SendAsync batch/Buffer or interate that.</param>
        /// <param name="cancellationToken">Token to cancellate process, this use skiphandle.</param>
        /// <returns>Collection of all exception, had one, otherwise return empty collection and status of execution.</returns>
        BatcherBuilderResult ExecuteCollection([NotNull] Action<IEnumerable<TEntity>> handler, Func<bool>? skipHandle = null);

        /// <summary>
        /// Interate over batch in source collection.
        /// </summary>
        /// <param name="handle">Action with items to be processed.</param>
        /// <param name="skipHandle">Skip interation if Post/SendAsync batch/Buffer or interate that.</param>
        /// <returns>Collection of all exception, had one, otherwise return empty collection and status of execution.</returns>
        BatcherBuilderResult ExecuteWithBatchItemInfoCollection([NotNull] Action<IEnumerable<BatchItemInfoModel<TEntity>>> handler, Func<bool>? skipHandle = null);

        /// <summary>
        /// Interate over batch in source collection.
        /// </summary>
        /// <param name="handle">Action with items to be processed. with items to be processed.</param>
        /// <param name="skipHandle">Skip interation if Post/SendAsync batch/Buffer or interate that.</param>
        /// <param name="cancellationToken">Token to cancellate process, this use skiphandle.</param>
        /// <returns>Collection of all exception, had one, otherwise return empty collection and status of execution.</returns>
        BatcherBuilderResult ExecuteWithBatchInfoCollection([NotNull] Action<BatchInfoModel<TEntity>> handler, Func<bool>? skipHandle = null);

        #endregion
    }
}
