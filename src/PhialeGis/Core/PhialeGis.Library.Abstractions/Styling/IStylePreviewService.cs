namespace PhialeGis.Library.Abstractions.Styling
{
    public interface IStylePreviewService
    {
        StylePreviewImage Render(StylePreviewRequest request);
    }
}
