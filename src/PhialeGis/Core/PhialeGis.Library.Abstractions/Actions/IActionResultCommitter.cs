namespace PhialeGis.Library.Abstractions.Actions
{
    public interface IActionResultCommitter
    {
        void Commit(LineStringActionResult result);
    }
}
