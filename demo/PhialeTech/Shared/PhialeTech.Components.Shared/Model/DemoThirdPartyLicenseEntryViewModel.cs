using System;
using System.Collections.Generic;
using System.Linq;

namespace PhialeTech.Components.Shared.Model
{
    public sealed class DemoThirdPartyLicenseEntryViewModel
    {
        public DemoThirdPartyLicenseEntryViewModel(
            string componentName,
            string usedBy,
            string licenseName,
            string copyrightNotice,
            string requirementSummary,
            IEnumerable<string> localFiles)
        {
            ComponentName = componentName ?? string.Empty;
            UsedBy = usedBy ?? string.Empty;
            LicenseName = licenseName ?? string.Empty;
            CopyrightNotice = copyrightNotice ?? string.Empty;
            RequirementSummary = requirementSummary ?? string.Empty;
            LocalFiles = (localFiles ?? Array.Empty<string>()).ToArray();
        }

        public string ComponentName { get; }

        public string UsedBy { get; }

        public string LicenseName { get; }

        public string CopyrightNotice { get; }

        public string RequirementSummary { get; }

        public IReadOnlyList<string> LocalFiles { get; }
    }
}

