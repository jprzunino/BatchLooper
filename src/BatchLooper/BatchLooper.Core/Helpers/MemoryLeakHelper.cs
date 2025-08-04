namespace BatchLooper.Core.Helpers
{
    public static class MemoryLeakHelper
    {
        public static void FixMemoryLeak()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }
}
