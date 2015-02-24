using System.Collections.Generic;

namespace PrinterHealth.Model
{
    /// <summary>
    /// A medium upon which a printer makes marks.
    /// </summary>
    public interface IMedium
    {
        /// <summary>
        /// Whether the medium is currently not available.
        /// </summary>
        bool IsEmpty { get; }

        /// <summary>
        /// The style classes to apply to this marker when visualizing it.
        /// </summary>
        IEnumerable<string> StyleClasses { get; }

        /// <summary>
        /// A short description of this medium.
        /// </summary>
        string Description { get; }
    }
}
