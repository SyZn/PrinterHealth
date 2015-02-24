using System.Collections.Generic;

namespace PrinterHealth.Model
{
    /// <summary>
    /// A device for making marks on a medium.
    /// </summary>
    public interface IPrinter
    {
        /// <summary>
        /// The markers available in the printer, and their status.
        /// </summary>
        IReadOnlyCollection<IMarker> Markers { get; }

        /// <summary>
        /// The media available in the printer, and their status.
        /// </summary>
        IReadOnlyCollection<IMedium> Media { get; }

        /// <summary>
        /// A collection of problems that are currently preventing the printer from performing one of its core tasks.
        /// </summary>
        IReadOnlyCollection<IStatusInfo> CurrentStatusMessages { get; }

        /// <summary>
        /// The number of currently queued jobs.
        /// </summary>
        int JobCount { get; }

        /// <summary>
        /// Performs an update of the cached values.
        /// </summary>
        void Update();
    }
}
