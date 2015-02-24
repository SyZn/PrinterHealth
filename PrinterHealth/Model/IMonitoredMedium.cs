namespace PrinterHealth.Model
{
    /// <summary>
    /// A medium that is monitored for being low.
    /// </summary>
    public interface IMonitoredMedium : IMedium
    {
        /// <summary>
        /// Whether the medium is still available, but slowly running out.
        /// </summary>
        bool IsLow { get; }
    }
}
