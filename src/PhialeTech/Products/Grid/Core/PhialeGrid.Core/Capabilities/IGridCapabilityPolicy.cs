using PhialeGrid.Core.Regions;

namespace PhialeGrid.Core.Capabilities
{
    public interface IGridCapabilityPolicy
    {
        bool CanViewRegion(GridRegionKind regionKind);

        bool CanExecuteCommand(string commandKey);
    }
}
