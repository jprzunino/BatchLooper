using static BatchLooper.Sample.Helpers.ArgumentIdentifierHelper;

namespace BatchLooper.Sample.Models
{
    public record ArgumentsModel
    {
        public const string MaxInterationsKey = "MaxInterations";
        public const string ExecuteAsyncKey = "EnableAsync";
        public const string DisableAsyncKey = "DisableAsync";
        public const string SleepTimeKey = "SleepTime";
        public const string MaxDegreeOfParallelismKey = "DegreeOfParallelism";
        public const string EnableProgressKey = "EnableProgress";
        public const string DisableProgressKey = "DisableProgress";
        public const string RunWithSerilogKey = "RunWithSerilog";


        public int MaxInterations { get; private set; } = 50_063_860;
        public int MaxDegreeOfParallelism { get; private set; } = (int)(Environment.ProcessorCount * 0.9);
        public TimeSpan? Sleep { get; private set; }
        public bool PrintExecution { get; private set; } = true;
        public bool ExecuteAsync { get; private set; } = true;
        public bool RunWithSerilog { get; private set; } = false;

        public ArgumentsModel(IEnumerable<string>? args)
        {
            if (args != null && args.Any())
            {
                string argValue = string.Empty;

                if (args.IdentifyArg(MaxInterationsKey, out argValue) && !string.IsNullOrEmpty(argValue?.Trim()))
                    MaxInterations = int.Parse(new string([.. argValue.Where(static c => char.IsDigit(c))]));

                if (args.IdentifyArg(MaxDegreeOfParallelismKey, out argValue) && !string.IsNullOrEmpty(argValue?.Trim()))
                    MaxDegreeOfParallelism = int.Parse(new string([.. argValue.Where(static c => char.IsDigit(c) || c == '-')]));

                if (args.IdentifyArg(SleepTimeKey, out argValue) && !string.IsNullOrEmpty(argValue?.Trim()))
                    Sleep = TimeSpan.FromSeconds(double.Parse(new string([.. argValue.Where(static c => char.IsDigit(c) || c=='.')])));

                if ((args.IdentifyArg(EnableProgressKey, out _) && !args.IdentifyArg(DisableProgressKey, out _)) ||
                    (!args.IdentifyArg(EnableProgressKey, out _) && args.IdentifyArg(DisableProgressKey, out _)))
                    PrintExecution = args.IdentifyArg(EnableProgressKey, out _) && !args.IdentifyArg(DisableProgressKey, out _);

                if ((args.IdentifyArg(ExecuteAsyncKey, out _) && !args.IdentifyArg(DisableAsyncKey, out _)) ||
                    (!args.IdentifyArg(ExecuteAsyncKey, out _) && args.IdentifyArg(DisableAsyncKey, out _)))
                    ExecuteAsync = args.IdentifyArg(ExecuteAsyncKey, out _) && !args.IdentifyArg(DisableAsyncKey, out _);

                RunWithSerilog = args.IdentifyArg(RunWithSerilogKey, out _);

                if (MaxDegreeOfParallelism == -1 && PrintExecution)
                    PrintExecution = false;
            }
        }
    }
}
