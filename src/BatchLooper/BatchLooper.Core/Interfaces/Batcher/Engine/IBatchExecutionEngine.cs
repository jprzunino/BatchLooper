namespace BatchLooper.Core.Interfaces.Batcher.Engine
{
    public interface IBatchExecutionEngine<TEntity> : IBatchExecutionEngineItem<TEntity>, IBatchExecutionEngineCollection<TEntity>
    {
        void PrintStaticInfo();
    }
}
