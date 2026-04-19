namespace PhialeTech.Components.Shared.Model
{
    public sealed class DemoSalesRowViewModel
    {
        public DemoSalesRowViewModel(
            string product,
            string year2018,
            string year2019,
            string year2020,
            string actual,
            string target,
            bool isAboveTarget,
            string actualForegroundHex,
            string targetForegroundHex)
        {
            Product = product;
            Year2018 = year2018;
            Year2019 = year2019;
            Year2020 = year2020;
            Actual = actual;
            Target = target;
            IsAboveTarget = isAboveTarget;
            ActualForegroundHex = actualForegroundHex;
            TargetForegroundHex = targetForegroundHex;
        }

        public string Product { get; }

        public string Year2018 { get; }

        public string Year2019 { get; }

        public string Year2020 { get; }

        public string Actual { get; }

        public string Target { get; }

        public bool IsAboveTarget { get; }

        public string ActualForegroundHex { get; }

        public string TargetForegroundHex { get; }
    }
}
