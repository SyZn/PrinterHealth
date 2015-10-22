namespace PrinterHealth.Model
{
    public interface IPrinterToKeepWarm : IPrinter
    {
        /// <summary>
        /// Keeps this printer warm.
        /// </summary>
        void KeepWarm();
    }
}
