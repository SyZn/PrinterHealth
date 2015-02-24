namespace PrinterHealth.Model
{
    /// <summary>
    /// The severity level of a status information message.
    /// </summary>
    public enum StatusLevel
    {
        /// <summary>
        /// An informative message. No action need be taken.
        /// </summary>
        Info = 0,

        /// <summary>
        /// A soft warning message. No action need be taken immediately, but the situation may worsen in the future.
        /// </summary>
        SoftWarning = 1,

        /// <summary>
        /// A hard warning message. The printer might not be able to perform some of its tasks, and action should be
        /// taken.
        /// </summary>
        HardWarning = 2,

        /// <summary>
        /// An error message. The printer cannot perform its primary task and immediate action should be taken.
        /// </summary>
        Error = 3,
    }
}
