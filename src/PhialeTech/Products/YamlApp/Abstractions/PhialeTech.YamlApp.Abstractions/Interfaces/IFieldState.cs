namespace PhialeTech.YamlApp.Abstractions.Interfaces
{
    public interface IFieldState
    {
        bool IsTouched { get; }
        bool IsPristine { get; }
        bool IsDirty { get; }
        bool IsValid { get; }

        string ErrorCode { get; }
        string ErrorMessage { get; }
    }
}
