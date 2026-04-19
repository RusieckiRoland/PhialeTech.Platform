namespace PhialeGis.Library.Core.Styling
{
    public interface IStyleContrastPolicy
    {
        bool ShouldApplyHalo(int foregroundArgb, int backgroundArgb);

        int GetHaloColorArgb(int backgroundArgb);

        int GetBorderColorArgb(int backgroundArgb);
    }
}
