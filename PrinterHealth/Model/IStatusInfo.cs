namespace PrinterHealth.Model
{
    /// <summary>
    /// Status information provided by the printer.
    /// </summary>
    public interface IStatusInfo
    {
        /// <summary>
        /// The severity level of this status message.
        /// </summary>
        StatusLevel Level { get; }

        /// <summary>
        /// A short description of this status message.
        /// </summary>
        string Description { get; }
    }
}
