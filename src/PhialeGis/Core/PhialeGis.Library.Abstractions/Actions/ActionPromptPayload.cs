namespace PhialeGis.Library.Abstractions.Actions
{
    /// <summary>
    /// Lightweight change payload emitted by actions whenever the editor prompt should change.
    /// The FSM re-emits this via its OnChange(object payload) event.
    /// </summary>
    public sealed class ActionPromptPayload
    {
        public DslPromptDto Prompt { get; set; }
    }
}
