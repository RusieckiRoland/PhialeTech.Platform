namespace PhialeGis.Library.Abstractions.Interactions
{
    public interface ISnapService
    {
        bool TrySnap(SnapRequest request, out SnapResult result);
    }
}
