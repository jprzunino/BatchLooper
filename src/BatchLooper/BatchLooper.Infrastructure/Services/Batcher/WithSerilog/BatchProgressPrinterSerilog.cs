using BatchLooper.Core.Interfaces.Batcher;
using BatchLooper.Core.Interfaces.Batcher.Printers;
using BatchLooper.Core.Models.Batcher;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Settings.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace BatchLooper.Infrastructure.Services.Batcher.WithSerilog
{
    public class BatchProgressPrinterSerilog<TEntity> : BatchProgressPrinterDebug<TEntity>, IBatchProgressPrinterSerilog<TEntity>
    {
        #region Constants

        private const string ConfigSerilogName = "BatchSerilog";

        #endregion

        #region Variables

        private readonly ILogger _logger;
        private readonly IBatchProgressPrinterSerilog<TEntity> _recursive;

        #endregion

        #region Initializers

        public BatchProgressPrinterSerilog([NotNull] IBatcherConfiguration<TEntity> configuration)
            : base(configuration) { _logger = default!; _recursive = this; }

        public BatchProgressPrinterSerilog([NotNull] ILogger logger, [NotNull] IBatcherConfiguration<TEntity> configuration)
            : this(configuration) => _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public BatchProgressPrinterSerilog([NotNull] IConfiguration configuration, [NotNull] IBatcherConfiguration<TEntity> batcherConfiguration)
          : this(new LoggerConfiguration()
          .ReadFrom.Configuration(configuration, new ConfigurationReaderOptions() { SectionName = ConfigSerilogName })
          .CreateLogger(), batcherConfiguration)
        { }

        #endregion

        #region Functions And Routines

        #region Publics

        public override void PrintProgress(PrintDataInfoModel<TEntity> printInfoDto, TimeSpan duration)
        {
            var message = printInfoDto.PrintProgress(duration);

            _recursive.PrintInformation(message);

            base.PrintProgress(printInfoDto, duration); //print message in debug console when debugging.
        }

        public override void PrintDebug(string message)
        {
            if (_logger.IsEnabled(Serilog.Events.LogEventLevel.Debug) && _configuration.PrintProgress)
                _logger.Debug(message);

            base.PrintDebug(message);
        }

        void IBatchProgressPrinterSerilog<TEntity>.PrintInformation(string message, bool inDebugToo)
        {
            if (_logger.IsEnabled(Serilog.Events.LogEventLevel.Information) && (_configuration.PrintProgress || _configuration.PrintProgressManual))
                _logger.Information(message);

            if (inDebugToo)
                base.PrintDebug(message); //print message in debug console when debugging.
        }

        #endregion

        #region Privates

        #endregion

        #endregion
    }
}
