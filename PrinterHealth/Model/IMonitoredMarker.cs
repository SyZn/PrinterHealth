namespace PrinterHealth.Model
{
    /// <summary>
    /// A marker that is monitored for being low.
    /// </summary>
    public interface IMonitoredMarker : IMarker
    {
        /// <summary>
        /// Whether the marker is still available, but slowly running out.
        /// </summary>
        bool IsLow { get; }
    }
}
