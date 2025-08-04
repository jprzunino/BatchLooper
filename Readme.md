# 🌀 BatchLooper
![.Net](https://img.shields.io/badge/.NET-5C2D91?style=for-the-badge&logo=.net&logoColor=white) ![C#](https://img.shields.io/badge/c%23-%23239120.svg?style=for-the-badge&logo=csharp&logoColor=white) ![Visual Studio](https://img.shields.io/badge/Visual%20Studio-5C2D91.svg?style=for-the-badge&logo=visual-studio&logoColor=white) ![Visual Studio Code](https://img.shields.io/badge/Visual%20Studio%20Code-0078d7.svg?style=for-the-badge&logo=visual-studio-code&logoColor=white) ![GitHub](https://img.shields.io/badge/github-%23121011.svg?style=for-the-badge&logo=github&logoColor=white) ![Lincese](https://camo.githubusercontent.com/cd878d57e2b361acc4718461dd7a9c2828f3c132dcfb18d363883883a7df60a3/68747470733a2f2f696d672e736869656c64732e696f2f6769746875622f6c6963656e73652f496c65726961796f2f6d61726b646f776e2d6261646765733f7374796c653d666f722d7468652d6261646765) 
![.NET](https://img.shields.io/badge/.NET-9.0-blueviolet?label=.NET-SDK) [![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](https://opensource.org/licenses/MIT)

_Is a robust library for asynchronous and parallel batch processing with support for logging, concurrency control, execution statistics, and error handling. Developed in .NET 9.0, the project is modular and extensible, with practical usage examples._

## 📁 Project Structure

> src\
> ├─ BatchLooper/\
> ├──├── BatchLooper.Core/\
> ├──├── BatchLooper.Infrastructure/\
> ├──└── BatchLooper.Sample/

- **Core**: _Contains the main models, extensions, helpers, and interfaces._
- **Infrastructure**: _Concrete implementations of batch execution, statistics, and progress printing._
- **Sample**: _Example project with synchronous and asynchronous execution, with and without Serilog._

## 🚀 Features

- Batch execution with configurable parallelism
- IEnumerable and IAsyncEnumerable support
- Automatic retry with retry control
- Print progress to the console, debug, or Serilog
- Execution statistics (total and average time)
- Pipeline and semaphore patterns for concurrency control
- Extensions for Span, Unsafe, Parallel LINQ, and more

## 🛠️ Technologies Used

- .NET 9.0 (C#)
- System.Threading.Tasks.Dataflow
- System.Linq.Async
- System.Threading.Channels
- Serilog

## 🧠 Design Decisions

_BatchLooper was designed with the following principles in mind:_

- **Modularity**: _Core logic is separated from infrastructure and sample usage._
- **Extensibility**: _Interfaces and builders allow for easy customization._
- **Performance**: _Uses parallelism, channels, and spans for efficient processing._
- **Observability**: _Built-in support for logging and statistics collection._

## 📦 Installation

```
dotnet restore
dotnet build
```

## ⚙️ Configuration

 _To configure BatchLooper, you can use the BatcherConfiguration<T> class to define how batches are processed. Here's a basic example:_

 ```
 var config = new BatcherConfiguration<MyEntity>()
    .WithSource(myList)
    .WithBatchSize(10)
    .WithDegreeOfParallelism(4);
 ```

You can also configure:

- **CancellationToken** for graceful shutdown
- **Progress Printers** (console, debug, Serilog)
- **Error Handling** strategies
- **Statistics** collection and reporting

## ⚙️ Advanced Settings

- **WithBuffer()**: _Enables buffering with DataflowBlockOptions._
- **WithForceExecutionTimer()**: _Forces execution after timeout._
- **WithRetry()**: _Sets the number of retries and wait time._
- **WithProgressPrinter()**: _Sets the progress print type._
- **WithStatistics()**: _Enables statistics collection._

## 📚 API Reference
_Here are some of the key classes and interfaces:_

**BatcherConfiguration<TEntity>**
_Defines the configuration for batch processing._

**BatcherBuilder<TEntity>**
_Builds and executes the batch processing pipeline._

**IBatchExecutionEngine<TEntity>**
_Interface for custom execution engines._

**IBatchProgressPrinter<TEntity>**
_Interface for printing progress updates._

**BatchStatistics<TEntity>**
_Collects and reports execution metrics._

**ParallelExtension**
_Provides custom parallel LINQ extensions._

**RetryExtension**
_Adds retry logic to synchronous and asynchronous operations._

## Statistics

_BatchLooper provides execution time tracking and average duration calculations._


## ▶️ Some Usage Examples

### Synchronous Execution

```
var config = new BatcherConfiguration<int>(Enumerable.Range(1, 100))
    .WithMaxDegreeOfParallelism(4)
    .WithBatchSize(10);

var builder = new BatcherBuilder<int>(config);
builder.Execute(item => Console.WriteLine($"Processing {item}"));
```

### Asynchronous Execution with Serilog

```
var logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

var config = new BatcherConfiguration<int>(Enumerable.Range(1, 100))
    .WithMaxDegreeOfParallelism(4)
    .WithBatchSize(10);

var builder = new BatcherBuilderSerilog<int>(logger, config);
await builder.ExecuteAsync(async item => await Task.Delay(100));
```

## 🧪 Example Project

 _Run the example project with:_

 ```
 dotnet run --project BatchLooper.Sample
 ```

## 🛠️ Troubleshooting

- **Build errors**: _Ensure you're using .NET 9.0 and have restored all dependencies._
- **Logging not appearing**: _Check your Serilog configuration and sinks (when using serilog) or it is enable in configuration._
- **Parallelism issues**: _Tune MaxDegreeOfParallelism and batch sizes for your workload._
- **Unhandled exceptions**: _Use WithRetry() and proper exception handling in your batch actions._

 ## 📄 License

 _This project is licensed under the MIT License._