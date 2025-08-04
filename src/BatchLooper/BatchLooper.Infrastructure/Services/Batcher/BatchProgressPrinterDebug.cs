using BatchLooper.Core.Interfaces.Batcher;
using BatchLooper.Core.Interfaces.Batcher.Printers;
using BatchLooper.Core.Models.Batcher;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace BatchLooper.Infrastructure.Services.Batcher
{
    public class BatchProgressPrinterDebug<TEntity> : IBatchProgressPrinterDebug<TEntity>
    {
        #region Variables

        private protected readonly IBatcherConfiguration<TEntity> _configuration;

        #endregion

        #region Initialize

        public BatchProgressPrinterDebug() : this(new BatcherConfiguration<TEntity>())
        {
        }

        public BatchProgressPrinterDebug([NotNull] IBatcherConfiguration<TEntity> batcherConfiguration) =>
            _configuration = batcherConfiguration ?? throw new ArgumentNullException(nameof(batcherConfiguration));

        #endregion

        #region Functions And Routines

        public virtual void PrintProgress(PrintDataInfoModel<TEntity> printInfoDto, TimeSpan duration)
        {
            var progressMessage = printInfoDto.PrintProgress(duration);
            PrintDebug(progressMessage + $" | ThreadId: {Environment.CurrentManagedThreadId} | Line: {printInfoDto.LinePosition}"); //print message in debug console when debugging.
        }

        public virtual void PrintProgressManual(PrintDataInfoModel<TEntity> printInfoDto, TimeSpan duration) => PrintProgress(printInfoDto, duration);

        public virtual void PrintDebug(string message)
        {
#if DEBUG
            Debug.Print($"[Debug] - {message}");
#endif
        }

        #endregion
    }
}
