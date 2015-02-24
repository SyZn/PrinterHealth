using System.Collections.Generic;

namespace PrinterHealth.Model
{
    /// <summary>
    /// A marker used by a printer to make marks on a medium.
    /// </summary>
    public interface IMarker
    {
        /// <summary>
        /// Whether the marker is currently not available.
        /// </summary>
        bool IsEmpty { get; }

        /// <summary>
        /// The style classes to apply to this marker when visualizing it.
        /// </summary>
        IEnumerable<string> StyleClasses { get; }

        /// <summary>
        /// A short description of this marker.
        /// </summary>
        string Description { get; }
    }
}
