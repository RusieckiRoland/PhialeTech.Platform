namespace PhialeGis.Library.Core.Interactions
{
    /// <summary>
    /// Sets short, user-facing status/prompt text (the command chip).
    /// Keep UI-agnostic; implement per platform.
    /// </summary>
    public interface IStatusPromptSink
    {
        /// <summary>Replace the prompt text shown to the user.</summary>
        void SetText(string text);
    }
}
