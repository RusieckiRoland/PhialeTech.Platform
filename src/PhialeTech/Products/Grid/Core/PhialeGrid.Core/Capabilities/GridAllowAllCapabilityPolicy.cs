using PhialeGrid.Core.Regions;

namespace PhialeGrid.Core.Capabilities
{
    public sealed class GridAllowAllCapabilityPolicy : IGridCapabilityPolicy
    {
        public static GridAllowAllCapabilityPolicy Instance { get; } = new GridAllowAllCapabilityPolicy();

        private GridAllowAllCapabilityPolicy()
        {
        }

        public bool CanViewRegion(GridRegionKind regionKind)
        {
            return true;
        }

        public bool CanExecuteCommand(string commandKey)
        {
            return true;
        }
    }
}
