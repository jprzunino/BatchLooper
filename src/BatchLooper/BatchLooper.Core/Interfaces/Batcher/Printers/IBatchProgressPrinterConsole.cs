using BatchLooper.Core.Interfaces.Patterns;

namespace BatchLooper.Core.Interfaces.Batcher.Printers
{
    public interface IBatchProgressPrinterConsole<TEntity> : IBatchProgressPrinterDebug<TEntity>
    {
        ISemaphorePattern Semaphore { get; }
        IBatcherConfiguration<TEntity> Configuration { get; }

        void PrintConsole(string message, bool useWriteLine = true, bool inDebugToo = false, int lanePosition = -1, ConsoleColor? textcolor = null);
        void ShowCursor();
        void HideCursor();
        void SynchronizeCursor();
    }
}
