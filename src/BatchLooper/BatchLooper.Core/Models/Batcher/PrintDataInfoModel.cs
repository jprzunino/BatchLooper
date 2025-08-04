using BatchLooper.Core.Helpers;
using BatchLooper.Core.Interfaces.Patterns;
using BatchLooper.Core.Patterns;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace BatchLooper.Core.Models.Batcher
{
    public partial record PrintDataInfoModel<TEntity>
    {
        private readonly IEnumerable<Type> _lstNumber;
        private readonly IEnumerable<Type> _lstFloat;
        protected ISemaphorePattern _semaphore;

        /// <summary>
        /// Batch: ({currentBatch:N0}/{batchCount:N0}) {currentProgress:G}% | Elapsed: {duration};
        /// </summary>
        private readonly string _totalProgressMessage;

        /// <summary>
        /// Batch: ({currentBatch:N0}/{batchCount:N0}) {totalProgress:G}% | Current: ({index:N0}/{batchSize:N0}) {processCurrentItem:G}% | Elapsed: {duration};
        /// </summary>
        private readonly string _currentProgressMessage;


        public int BatchIndex { get; init; }
        public int BatchesCount { get; init; }
        public int ItemIndex { get; internal protected set; }
        public int ItemsCount { get; init; }
        public int LinePosition { get; init; }

        public PrintDataInfoModel()
        {
            _totalProgressMessage = "Batch: ({0:N0}/{1:N0}) {2:G}% | Elapsed: {3}";
            _currentProgressMessage = "Batch: ({0:N0}/{1:N0}) {2:G}% | Current: ({3:N0}/{4:N0}) {5:G}% | Elapsed: {6}";

            BatchesCount = BatchIndex = ItemIndex = ItemsCount = 1;
            LinePosition = 0;

            _lstNumber = [typeof(int), typeof(long)];
            _lstFloat = [typeof(float), typeof(double), typeof(decimal)];
            _semaphore = default!;
        }

        public PrintDataInfoModel(ISemaphorePattern? semaphore)
            : this() => _semaphore = semaphore ?? new SemaphorePattern();

        public PrintDataInfoModel([NotNull] ISemaphorePattern semaphore, int batchIndex, int batchesCount, int itemIndex, int itemsCount, int linePosition)
            : this(semaphore)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(batchIndex, 0);
            ArgumentOutOfRangeException.ThrowIfLessThan(itemsCount, 1);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchesCount);

            BatchIndex = batchIndex;
            BatchesCount = batchesCount;
            ItemIndex = itemIndex;
            ItemsCount = itemsCount;
            LinePosition = linePosition;
        }

        public PrintDataInfoModel([NotNull] ISemaphorePattern semaphore, [NotNull] BatchInfoModel<TEntity> batchInfo, [NotNull] BatchItemInfoModel<TEntity> itemsInfo, int? linePosition = null)
            : this(semaphore: semaphore,
                  batchIndex: batchInfo?.Index ?? throw new ArgumentNullException(nameof(batchInfo)),
                  batchesCount: batchInfo?.BatchesCount ?? throw new ArgumentNullException(nameof(batchInfo)),
                  itemIndex: itemsInfo?.Index ?? throw new ArgumentNullException(nameof(itemsInfo)),
                  itemsCount: batchInfo.ItemsCount,
                  linePosition: linePosition ?? batchInfo.LinePosition)
        { }

        public PrintDataInfoModel([NotNull] ISemaphorePattern semaphore, [NotNull] BatchInfoModel<TEntity> batchInfo, int? linePosition = null)
            : this(semaphore,
                  batchIndex: batchInfo?.Index ?? throw new ArgumentNullException(nameof(batchInfo)),
                  batchesCount: batchInfo?.BatchesCount ?? throw new ArgumentNullException(nameof(batchInfo)),
                  itemIndex: 1,
                  itemsCount: 1,
                  linePosition: linePosition ?? batchInfo?.LinePosition ?? throw new ArgumentNullException(nameof(batchInfo)))
        {
            ItemIndex = -1;
            ItemsCount = -1;
        }

        [Obsolete($"This is deprecated, please use the other overload instead, this remeanig becaue 'BatcherBuilderOlder<TEntity>' and will removed soon.")]
        public void PrintProgress(TimeSpan totalDuration, bool inConsole)
        {
            var errors = _semaphore.HandleConcurencyMultithread(TimeSpan.FromSeconds(2), () =>
            {
                string message = (ItemIndex > 0 ?
                GetItemProgressMessage(totalDuration) :
                GetTotalProgressMssage(totalDuration))
                .Trim();

                if (inConsole)
                {
                    if (LinePosition >= 0 && LinePosition < Console.WindowHeight)
                    {
                        Console.SetCursorPosition(0, LinePosition);
                        //Console.Write(new string(' ', Console.WindowWidth));//clean lane.
                    }

                    if (ItemIndex > 0)
                        Console.Write($"\r{message}");
                    else
                        Console.WriteLine(message);
                }

#if DEBUG
                Debug.Print(message + $" | ThreadId: {Environment.CurrentManagedThreadId} | Line: {LinePosition}");
#endif
            });

            if (errors is not null && !errors.IsEmpty)
                throw new AggregateException($"Error in {nameof(PrintProgress)}, see inner exceptions from more details.", errors);
        }

        public string PrintProgress(TimeSpan totalDuration)
        {
            string result = (ItemIndex > 0 ?
                GetItemProgressMessage(totalDuration) :
                GetTotalProgressMssage(totalDuration))
                .Trim();

            return result;
        }

        public void CalculateIndexByThreadId(int line, int columnSelector, int maxDegreeOfParallelism) =>
            ItemIndex = CaculateBatchIndex(line, intervalRange: maxDegreeOfParallelism, columnSelector: columnSelector);

        public decimal CalculateTotalProgress()
        {
            var result = Math.Round((BatchIndex / (decimal)(BatchesCount)) * 100, 2);

            if (result % 10 == 0)
                result += 0.00m;

            return result;
        }

        public decimal CalculateBatchProgress()
        {
            var result = Math.Round((ItemIndex / (decimal)(ItemsCount)) * 100, 2);

            if (result % 10 == 0)
                result += 0.00m;

            return result;
        }

        private string GetItemProgressMessage(TimeSpan totalDuration)
        {
            var batchesCharLength = GetBatchesCharLength();
            var totalPercent = PrintNumberFormatter(CalculateTotalProgress(), 6);
            var batchCharLength = GetBatchCharLength();
            var idx = PrintNumberFormatter(ItemIndex, batchCharLength);
            var itemPercent = PrintNumberFormatter(CalculateBatchProgress(), 6);

            string result = string.Format(_currentProgressMessage,
                        PrintNumberFormatter(BatchIndex, batchesCharLength),
                        BatchesCount,
                        totalPercent,
                        idx,
                        ItemsCount,
                        itemPercent,
                        totalDuration);

            return result.Trim();
        }

        private string GetTotalProgressMssage(TimeSpan totalDuration)
        {
            var totalPercent = PrintNumberFormatter(CalculateTotalProgress(), 6);
            string result = string.Format(_totalProgressMessage,
                         PrintNumberFormatter(BatchIndex, GetBatchesCharLength()),
                        BatchesCount,
                        totalPercent,
                        totalDuration);

            return result;
        }

        private int GetBatchesCharLength()
        {
            var separatorCount = (int)Math.Round(BatchesCount.ToString().Length / (decimal)3, 0);
            var charLength = BatchesCount.ToString().Length;
            var result = Interlocked.Add(ref charLength, separatorCount);
            return result;
        }

        private int GetBatchCharLength()
        {
            var separatorCount = (int)Math.Round(ItemsCount.ToString().Length / (decimal)3, 0);
            var charLength = ItemsCount.ToString().Length;
            var result = Interlocked.Add(ref charLength, separatorCount);
            return result;
        }

        private static int CaculateBatchIndex(int line, int intervalRange, int columnSelector = 0)
        {
            if (columnSelector < 0)
                columnSelector = 0;

            var result = columnSelector * (intervalRange) + line;
            return result;
        }

        private string PrintNumberFormatter<TValue>(TValue value, int formatSize, char charFill = '0')
        {
            var tmp = (value?.ToString() ?? string.Empty);
            var typeValue = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);
            var numberFormatInfo = CultureInfo.CurrentUICulture.NumberFormat;

            if (_lstNumber.Any(type => type == typeValue))
                tmp = value.Cast<TValue, long>().ToString("N0", numberFormatInfo);

            if (_lstFloat.Any(type => type == typeValue))
                tmp = value.Cast<TValue, decimal>().ToString("G", numberFormatInfo);

            var result = tmp.PadLeft(formatSize, charFill);
            return result;
        }
    }
}
