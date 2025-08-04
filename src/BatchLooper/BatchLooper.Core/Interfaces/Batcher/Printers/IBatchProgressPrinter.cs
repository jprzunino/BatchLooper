using BatchLooper.Core.Models.Batcher;

namespace BatchLooper.Core.Interfaces.Batcher.Printers
{
    public interface IBatchProgressPrinter<TEntity>
    {
        void PrintProgress(PrintDataInfoModel<TEntity> printInfoDto, TimeSpan duration);
        void PrintProgressManual(PrintDataInfoModel<TEntity> printInfoDto, TimeSpan duration);
    }
}
