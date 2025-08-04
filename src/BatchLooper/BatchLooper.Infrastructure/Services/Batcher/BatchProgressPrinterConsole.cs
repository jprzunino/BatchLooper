using BatchLooper.Core.Interfaces.Batcher;
using BatchLooper.Core.Interfaces.Batcher.Printers;
using BatchLooper.Core.Interfaces.Patterns;
using BatchLooper.Core.Models.Batcher;
using System.Diagnostics.CodeAnalysis;

namespace BatchLooper.Infrastructure.Services.Batcher
{
    public class BatchProgressPrinterConsole<TEntity> : BatchProgressPrinterDebug<TEntity>, IBatchProgressPrinterConsole<TEntity>
    {
        #region Variables

        private readonly ISemaphorePattern _semaphore;
        private readonly IBatchProgressPrinterConsole<TEntity> _recursive;
        private int _linesOffSet = -1;

        #endregion

        #region Properties

        ISemaphorePattern IBatchProgressPrinterConsole<TEntity>.Semaphore => _semaphore;

        IBatcherConfiguration<TEntity> IBatchProgressPrinterConsole<TEntity>.Configuration => _configuration;

        #endregion

        #region Initializers

        public BatchProgressPrinterConsole([NotNull] ISemaphorePattern semaphore)
            : base()
        {
            _semaphore = semaphore ?? throw new ArgumentNullException(nameof(semaphore));
            _recursive = this;
        }

        public BatchProgressPrinterConsole([NotNull] ISemaphorePattern semaphore, [NotNull] IBatcherConfiguration<TEntity> batcherConfiguration)
            : base(batcherConfiguration)
        {
            _semaphore = semaphore ?? throw new ArgumentNullException(nameof(semaphore));
            _recursive = this;
        }

        #endregion

        #region Functions And Routines

        #region Publics

        public override void PrintProgress(PrintDataInfoModel<TEntity> printInfoDto, TimeSpan duration)
        {
            var errors = _semaphore.HandleConcurencyMultithread(TimeSpan.FromSeconds(2), () =>
            {
                var message = printInfoDto.PrintProgress(duration);

                if (_configuration.PrintProgress)
                    _recursive.PrintConsole(message, printInfoDto.ItemIndex <= 0, lanePosition: printInfoDto.LinePosition);

                base.PrintProgress(printInfoDto, duration); //print message in debug console when debugging.
            });

            if (errors is not null && !errors.IsEmpty)
                throw new AggregateException($"Error in {nameof(PrintProgress)}, see inner exceptions from more details.", errors);
        }

        public override void PrintProgressManual(PrintDataInfoModel<TEntity> printInfoDto, TimeSpan duration)
        {
            var errors = _semaphore.HandleConcurencyMultithread(TimeSpan.FromSeconds(2), () =>
            {
                var message = printInfoDto.PrintProgress(duration);

                if (_configuration.PrintProgressManual)
                    _recursive.PrintConsole(message, printInfoDto.ItemIndex <= 0, lanePosition: printInfoDto.LinePosition);

                base.PrintProgress(printInfoDto, duration); //print message in debug console when debugging.
            });

            if (errors is not null && !errors.IsEmpty)
                throw new AggregateException($"Error in {nameof(PrintProgress)}, see inner exceptions from more details.", errors);
        }

        void IBatchProgressPrinterConsole<TEntity>.PrintConsole(string message, bool useWriteLine, bool inDebugToo, int lanePosition, ConsoleColor? textcolor)
        {
            if (_configuration.PrintProgress || _configuration.PrintProgressManual)
            {
                ChangeLanePosition(lanePosition);

                if (textcolor.HasValue)
                    Console.ForegroundColor = textcolor.Value;

                if (useWriteLine)
                    Console.WriteLine(message);
                else
                    Console.Write(message);

                if (textcolor.HasValue)
                    Console.ResetColor();
            }

            if (inDebugToo)
                base.PrintDebug(message);
        }

        void IBatchProgressPrinterConsole<TEntity>.ShowCursor()
        {
            if (_configuration.PrintProgress)
                Console.CursorVisible = true;
        }

        void IBatchProgressPrinterConsole<TEntity>.HideCursor()
        {
            if (_configuration.PrintProgress || _configuration.PrintProgressManual)
            {
                Console.CursorVisible = false;

                if (_linesOffSet < 0)
                {
                    (_, _linesOffSet) = Console.GetCursorPosition();
                    _linesOffSet++;
                }
            }
        }

        void IBatchProgressPrinterConsole<TEntity>.SynchronizeCursor()
        {
            if (_configuration.PrintProgress)
            {
                var offset = _configuration.MaxDegreeOfParallelism + 2;
                if (offset < 0)
                    offset = 0;

                //ProtectWrite(offset);
                ChangeLanePosition(offset);
            }
        }

        #endregion

        #region Privates

        private static void ProtectWrite(int lanePosition)
        {
            if (OperatingSystem.IsWindows() && lanePosition > Console.WindowHeight)
                Console.SetWindowSize(Console.WindowWidth, lanePosition);

            if (OperatingSystem.IsLinux())
            {
                //TODO: Treat when console not have space enough to print messages in linux
            }
        }

        private void ChangeLanePosition(int lanePosition)
        {
            if (lanePosition >= 0 && lanePosition < Console.WindowHeight && lanePosition < Console.BufferHeight)
            {
                var calc = lanePosition + _linesOffSet - 1;
                Console.SetCursorPosition(0, calc);
                //Console.Write(new string(' ', Console.WindowWidth));//clean lane.
            }
        }

        #endregion

        #endregion
    }
}
