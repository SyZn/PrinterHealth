using System;
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
        /// The date/time when the information about this printer was last updated, or <c>null</c> if it hasn't been
        /// fetched (yet).
        /// </summary>
        DateTimeOffset? LastUpdated { get; }

        /// <summary>
        /// The number of currently queued jobs.
        /// </summary>
        int JobCount { get; }

        /// <summary>
        /// Whether the printer currently accepts new jobs through its primary submission channel.
        /// </summary>
        bool ReadyForSubmission { get; }

        /// <summary>
        /// The URI to this printer's web interface, or <c>null</c> if the printer has none.
        /// </summary>
        string WebInterfaceUri { get; }

        /// <summary>
        /// Performs an update of the cached values.
        /// </summary>
        void Update();
    }
}
