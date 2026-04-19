using System;

namespace PhialeGrid.Core.Editing
{
    public sealed class CurrentRecordChangedEventArgs<TRecord> : EventArgs
    {
        public CurrentRecordChangedEventArgs(
            string previousRecordId,
            TRecord previousRecord,
            string currentRecordId,
            TRecord currentRecord)
        {
            PreviousRecordId = previousRecordId ?? string.Empty;
            PreviousRecord = previousRecord;
            CurrentRecordId = currentRecordId ?? string.Empty;
            CurrentRecord = currentRecord;
        }

        public string PreviousRecordId { get; }

        public TRecord PreviousRecord { get; }

        public string CurrentRecordId { get; }

        public TRecord CurrentRecord { get; }
    }
}
