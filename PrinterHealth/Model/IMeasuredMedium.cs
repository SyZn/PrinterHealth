namespace PrinterHealth.Model
{
    /// <summary>
    /// A medium whose level is measured.
    /// </summary>
    public interface IMeasuredMedium : IMonitoredMedium
    {
        /// <summary>
        /// The current level of the medium, in percent (i.e. within [0; 100]).
        /// </summary>
        float LevelPercent { get; }
    }
}
