namespace PhialeTech.ComponentHost.Abstractions.State
{
    public interface IApplicationStateStore
    {
        void Save(string stateKey, string payload);

        string Load(string stateKey);

        void Delete(string stateKey);
    }
}
