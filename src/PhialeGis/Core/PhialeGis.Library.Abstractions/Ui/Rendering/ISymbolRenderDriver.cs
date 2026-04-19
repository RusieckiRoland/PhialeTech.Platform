namespace PhialeGis.Library.Abstractions.Ui.Rendering
{
    public interface ISymbolRenderDriver : IPhRenderDriver
    {
        void DrawSymbol(SymbolRenderRequest request);
    }
}
