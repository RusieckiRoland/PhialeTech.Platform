using System.Runtime.CompilerServices;

namespace UniversalInput.Contracts
{
    public class UniversalEventArgs<T>
    {
        private T _event;

        public UniversalEventArgs(T @event, [CallerMemberName] string name = "")
        {
            _event = @event;
            EventName = name;
        }

        public string EventName { get; set; }

        public T Event => _event;
    }
}

