using BatchLooper.Core.Interfaces.Batcher.Engine;
using System.Diagnostics.CodeAnalysis;

namespace BatchLooper.Core.Interfaces.Batcher
{
    public interface IBatcherBuilder<TEntity> : IBatchExecutionEngine<TEntity>
    {
        void WithConfigurations([NotNull] IBatcherConfiguration<TEntity> configuration);
    }
}
