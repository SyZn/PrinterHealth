namespace PrinterHealth.Model
{
    public interface IPrinterWithJobCleanup : IPrinter
    {
        /// <summary>
        /// Potentially removes print jobs that are stuck in the printer.
        /// </summary>
        void CleanupBrokenJobs();
    }
}
