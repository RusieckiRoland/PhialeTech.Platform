using System;
using System.ComponentModel;
using PhialeTech.Components.Shared.Core;

namespace PhialeTech.Components.Shared.Model
{
    public sealed class DemoGisRecordViewModel : BindableBase, ICloneable, IDataErrorInfo
    {
        private string _objectName;
        private string _status;
        private string _priority;
        private decimal _areaSquareMeters;
        private decimal _lengthMeters;
        private DateTime _lastInspection;
        private string _owner;
        private int _scaleHint;
        private bool _visible;
        private bool _editableFlag;

        public DemoGisRecordViewModel(
            string objectId,
            string category,
            string objectName,
            string geometryType,
            string crs,
            string municipality,
            string district,
            string status,
            decimal areaSquareMeters,
            decimal lengthMeters,
            DateTime lastInspection,
            string source,
            string priority,
            bool visible,
            bool editableFlag,
            string owner,
            int scaleHint,
            string tags)
        {
            Id = string.IsNullOrWhiteSpace(objectId) ? Guid.NewGuid().ToString("N") : objectId;
            Category = category ?? string.Empty;
            ObjectName = objectName ?? string.Empty;
            GeometryType = geometryType ?? string.Empty;
            Crs = crs ?? string.Empty;
            Municipality = municipality ?? string.Empty;
            District = district ?? string.Empty;
            Status = status ?? string.Empty;
            AreaSquareMeters = areaSquareMeters;
            LengthMeters = lengthMeters;
            LastInspection = lastInspection;
            Source = source ?? string.Empty;
            Priority = priority ?? string.Empty;
            Visible = visible;
            EditableFlag = editableFlag;
            Owner = owner ?? string.Empty;
            ScaleHint = scaleHint;
            Tags = tags ?? string.Empty;
        }

        public string Id { get; }

        public string Category { get; }

        public string ObjectId => Id;

        public string GeometryType { get; }

        public string Crs { get; }

        public string Municipality { get; }

        public string District { get; }

        public string Source { get; }

        public bool Visible
        {
            get => _visible;
            set => SetProperty(ref _visible, value);
        }

        public bool EditableFlag
        {
            get => _editableFlag;
            set => SetProperty(ref _editableFlag, value);
        }

        public string Tags { get; }

        public string ObjectName
        {
            get => _objectName;
            set => SetProperty(ref _objectName, value ?? string.Empty);
        }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value ?? string.Empty);
        }

        public string Priority
        {
            get => _priority;
            set => SetProperty(ref _priority, value ?? string.Empty);
        }

        public decimal AreaSquareMeters
        {
            get => _areaSquareMeters;
            set => SetProperty(ref _areaSquareMeters, value);
        }

        public decimal LengthMeters
        {
            get => _lengthMeters;
            set => SetProperty(ref _lengthMeters, value);
        }

        public DateTime LastInspection
        {
            get => _lastInspection;
            set => SetProperty(ref _lastInspection, value);
        }

        public DateTime UpdatedAt => LastInspection.Date
            .AddHours(8 + (ScaleHint % 9))
            .AddMinutes((ScaleHint * 7) % 60);

        public string Owner
        {
            get => _owner;
            set => SetProperty(ref _owner, value ?? string.Empty);
        }

        public int ScaleHint
        {
            get => _scaleHint;
            set => SetProperty(ref _scaleHint, value);
        }

        public decimal MaintenanceBudget => Math.Round(
            (AreaSquareMeters * 1.15m) +
            (LengthMeters * 18.75m) +
            (ScaleHint * 2.4m),
            2,
            MidpointRounding.AwayFromZero);

        public decimal CompletionPercent
        {
            get
            {
                switch (Status)
                {
                    case "Verified":
                        return 100m;
                    case "Active":
                        return 84m;
                    case "NeedsReview":
                        return 58m;
                    case "UnderMaintenance":
                        return 46m;
                    case "Planned":
                        return 35m;
                    case "Retired":
                        return 12m;
                    default:
                        return 50m;
                }
            }
        }

        public string Error => string.Empty;

        public string this[string columnName]
        {
            get
            {
                switch (columnName)
                {
                    case nameof(ObjectName):
                        return string.IsNullOrWhiteSpace(ObjectName) ? "Object name is required." : string.Empty;
                    case nameof(Status):
                        return string.IsNullOrWhiteSpace(Status) ? "Status is required." : string.Empty;
                    case nameof(Priority):
                        return string.IsNullOrWhiteSpace(Priority) ? "Priority is required." : string.Empty;
                    case nameof(Owner):
                        return string.IsNullOrWhiteSpace(Owner) ? "Owner is required." : string.Empty;
                    case nameof(AreaSquareMeters):
                        return AreaSquareMeters < 0m ? "Area cannot be negative." : string.Empty;
                    case nameof(LengthMeters):
                        return LengthMeters < 0m ? "Length cannot be negative." : string.Empty;
                    case nameof(ScaleHint):
                        return ScaleHint <= 0 ? "Scale hint must be greater than zero." : string.Empty;
                    default:
                        return string.Empty;
                }
            }
        }

        public object Clone()
        {
            return new DemoGisRecordViewModel(
                Id,
                Category,
                ObjectName,
                GeometryType,
                Crs,
                Municipality,
                District,
                Status,
                AreaSquareMeters,
                LengthMeters,
                LastInspection,
                Source,
                Priority,
                Visible,
                EditableFlag,
                Owner,
                ScaleHint,
                Tags);
        }
    }
}
