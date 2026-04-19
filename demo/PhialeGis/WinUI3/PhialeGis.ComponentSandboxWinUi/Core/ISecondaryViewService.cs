using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhialeGis.ComponentSandboxWinUi.Core
{
    public interface ISecondaryViewService
    {
        /// <summary>
        /// Opens a new window hosting another map view.
        /// Returns the new view Id (UWP-specific), or 0 if not applicable.
        /// </summary>
        Task<int> OpenMapWindowAsync();
    }
}
