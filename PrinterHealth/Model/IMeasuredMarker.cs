namespace PrinterHealth.Model
{
    /// <summary>
    /// A marker whose level is measured.
    /// </summary>
    public interface IMeasuredMarker : IMonitoredMarker
    {
        /// <summary>
        /// The current level of the marker, in percent (i.e. within [0; 100]).
        /// </summary>
        float LevelPercent { get; }
    }
}
