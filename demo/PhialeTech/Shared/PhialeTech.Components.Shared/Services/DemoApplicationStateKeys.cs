using System;

namespace PhialeTech.Components.Shared.Services
{
    public static class DemoApplicationStateKeys
    {
        public static string ForGridExample(string exampleId)
        {
            if (string.IsNullOrWhiteSpace(exampleId))
            {
                throw new ArgumentException("Example id is required.", nameof(exampleId));
            }

            return "Demo/Grid/" + exampleId.Trim();
        }
    }
}
