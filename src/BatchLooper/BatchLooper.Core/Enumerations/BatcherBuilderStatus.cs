using System.ComponentModel;

namespace BatchLooper.Core.Enumerations
{
    public enum BatcherBuilderStatus
    {
        [Description("None")] None = 0,
        [Description("Running")] Running = 1,
        [Description("Canceled")] Canceled = 2,
        [Description("With Errors")] WithErrors = 3,
        [Description("Succesfuly")] Success = 4
    }
}
