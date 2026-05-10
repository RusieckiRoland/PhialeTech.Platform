namespace PhialeGrid.Core.Details
{
    public interface IGridRowDetailProvider
    {
        bool HasDetail(GridRowDetailRequest request);

        GridRowDetailDescriptor CreateDetail(GridRowDetailRequest request);
    }
}
