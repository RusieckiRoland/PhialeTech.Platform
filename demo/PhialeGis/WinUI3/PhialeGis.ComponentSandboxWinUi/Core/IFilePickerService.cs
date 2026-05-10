using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhialeGis.ComponentSandboxWinUi.Core
{
    public interface IFilePickerService
    {
        Task<Stream?> PickFgbAsync(Window owner); 
    }
}

