using BatchLooper.Core.Interfaces.Batcher;
using BatchLooper.Core.Models.Batcher;
using BatchLooper.Infrastructure.Services.Batcher;
using BatchLooper.Sample;
using BatchLooper.Sample.Models;
using Serilog;

var appConfig = new ArgumentsModel(args);
var listItens = Enumerable.Range(1, appConfig.MaxInterations);

ArgumentNullException.ThrowIfNull(listItens);
IBatcherConfiguration<int> batcherConfiguration = new BatcherConfiguration<int>(listItens);

batcherConfiguration.WithMaxDegreeOfParallelism(appConfig.MaxDegreeOfParallelism);

if (appConfig.PrintExecution)
    batcherConfiguration.ShowProgress();

using var cts = new CancellationTokenSource();

var result = appConfig switch
{
    ({ ExecuteAsync: false, RunWithSerilog: false }) => Run(cts),
    ({ ExecuteAsync: false, RunWithSerilog: true }) => RunSerilog(cts),
    ({ ExecuteAsync: true, RunWithSerilog: false }) => await RunAsync(cts),
    ({ ExecuteAsync: true, RunWithSerilog: true }) => await RunSerilogAsync(cts),
    _ => throw new NotImplementedException()
};

BatcherBuilderResult Run(CancellationTokenSource cts) => BatcherBuilderSample.WithCustomsSamples
        .ExecutionSample(
            configuration: batcherConfiguration,
            waitTime: appConfig.Sleep,
            skipHandle: () => cts.IsCancellationRequested,
            throwError: null /*Action to throw error for testing in this action return true.*/,
            cts);

BatcherBuilderResult RunSerilog(CancellationTokenSource cts)
{
    ILogger logger = new LoggerConfiguration()
                    //.WriteTo.Console(outputTemplate: "\r{Message}")
                    .WriteTo.Console()
                    .Enrich.WithProperty("WithMachineName", Environment.MachineName)
                    .Enrich.WithProperty("WithThreadId", Environment.CurrentManagedThreadId)
                    .CreateLogger()
                    .ForContext<Program>();

#if DEBUG
    Serilog.Debugging.SelfLog.Enable(Console.Error);
#endif

    var result = BatcherBuilderSample.WithCustomsSamples
        .ExecutionSerilogSample(
        logger: logger,
        configuration: batcherConfiguration,
        waitTime: appConfig.Sleep,
        skipHandle: () => cts.IsCancellationRequested,
        throwError: null /*Action to throw error for testing in this action return true.*/,
        cts);

    return result;
}

async Task<BatcherBuilderResult> RunAsync(CancellationTokenSource cts) => await BatcherBuilderSample.WithCustomsSamples
        .ExecutionSampleAsync(
        configuration: batcherConfiguration,
        waitTime: appConfig.Sleep,
        skipHandle: () => cts.Token.IsCancellationRequested,
        throwError: null,/*Action to throw error for testing in this action return true.*/
        cts: cts);

async Task<BatcherBuilderResult> RunSerilogAsync(CancellationTokenSource cts)
{
    ILogger logger = new LoggerConfiguration()
                    //.WriteTo.Console(outputTemplate: "\r{Message}")
                    .WriteTo.Console()
                    .Enrich.WithProperty("WithMachineName", Environment.MachineName)
                    .Enrich.WithProperty("WithThreadId", Environment.CurrentManagedThreadId)
                    .CreateLogger()
                    .ForContext<Program>();

#if DEBUG
    Serilog.Debugging.SelfLog.Enable(Console.Error);//
#endif

    var result = await BatcherBuilderSample.WithCustomsSamples
        .ExecutionSerilogSampleAsync(
        logger: logger,
        configuration: batcherConfiguration,
        waitTime: appConfig.Sleep,
        skipHandle: () => cts.Token.IsCancellationRequested,
        throwError: null, /*Action to throw error for testing in this action return true.*/
        cts: cts);

    return result;
}
