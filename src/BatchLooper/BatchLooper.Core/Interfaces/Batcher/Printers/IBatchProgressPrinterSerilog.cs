namespace BatchLooper.Core.Interfaces.Batcher.Printers
{
    public interface IBatchProgressPrinterSerilog<TEntity> : IBatchProgressPrinterDebug<TEntity>
    {
        public void PrintInformation(string message, bool inDebugToo = false);
    }
}
