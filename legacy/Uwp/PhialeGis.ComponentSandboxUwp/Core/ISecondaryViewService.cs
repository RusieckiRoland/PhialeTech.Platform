using System.Threading.Tasks;

namespace PhialeGis.ComponentSandboxUwp.Core
{
    /// <summary>
    /// Abstraction for opening additional application views (windows).
    /// Keeps platform-specific windowing APIs out of ViewModels.
    /// </summary>
    public interface ISecondaryViewService
    {
        /// <summary>
        /// Opens a new window hosting another map view.
        /// Returns the new view Id (UWP-specific), or 0 if not applicable.
        /// </summary>
        Task<int> OpenMapWindowAsync();
    }
}
