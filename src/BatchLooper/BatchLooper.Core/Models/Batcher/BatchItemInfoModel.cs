using System.Diagnostics.CodeAnalysis;

namespace BatchLooper.Core.Models.Batcher
{
    public record BatchItemInfoModel<TEntity>
    {
        public TEntity? Data { get; init; }
        public int Index { get; init; }

        public BatchItemInfoModel()
        {
            Data = default;
            Index = 1;
        }

        public BatchItemInfoModel([NotNull] TEntity data, int index)
            : this()
        {
            ArgumentNullException.ThrowIfNull(data);
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(index, 0);
            Data = data;
            Index = index;
        }


        public static implicit operator BatchItemInfoModel<TEntity>([NotNull] BatchOrderModel<TEntity> batchOrderModel)
        {
            ArgumentNullException.ThrowIfNull(batchOrderModel);
            ArgumentNullException.ThrowIfNull(batchOrderModel.Data);

            return new BatchItemInfoModel<TEntity>(batchOrderModel.Data, batchOrderModel.Index);
        }
    }
}
