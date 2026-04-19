namespace PhialeGis.Library.Core.Interfaces
{
    /// <summary>
    /// Abstraction for a graphics facade.
    /// Implementations are responsible for refreshing/redrawing
    /// the visual output when the model or viewport changes.
    /// </summary>
    public interface IGraphicsFacade
    {
        /// <summary>
        /// Forces the view to be redrawn.
        /// </summary>
        void Invalidate();
    }
}
