namespace BatchLooper.Core.Interfaces.Batcher.Printers
{
    public interface IBatchProgressPrinterDebug<TEntity> : IBatchProgressPrinter<TEntity>
    {
        void PrintDebug(string message);
    }
}
