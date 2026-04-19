using System;
using System.ComponentModel;
using PhialeTech.Components.Shared.Core;

namespace PhialeTech.Components.Shared.Model
{
    public sealed class DemoSalesRecordViewModel : BindableBase, ICloneable, IDataErrorInfo
    {
        private readonly string _id;
        private string _product;
        private decimal _year2018;
        private decimal _year2019;
        private decimal _year2020;
        private decimal _actual;
        private decimal _target;

        public DemoSalesRecordViewModel()
            : this(Guid.NewGuid().ToString("N"), string.Empty, 0m, 0m, 0m, 0m, 1m)
        {
        }

        public DemoSalesRecordViewModel(string product, decimal year2018, decimal year2019, decimal year2020, decimal actual, decimal target)
            : this(Guid.NewGuid().ToString("N"), product, year2018, year2019, year2020, actual, target)
        {
        }

        private DemoSalesRecordViewModel(string id, string product, decimal year2018, decimal year2019, decimal year2020, decimal actual, decimal target)
        {
            _id = string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString("N") : id;
            _product = product;
            _year2018 = year2018;
            _year2019 = year2019;
            _year2020 = year2020;
            _actual = actual;
            _target = target;
        }

        public string Id => _id;

        public string Product
        {
            get => _product;
            set
            {
                if (SetProperty(ref _product, value))
                {
                    OnPropertyChanged(nameof(Family));
                    OnPropertyChanged(nameof(Delta));
                    OnPropertyChanged(nameof(IsAboveTarget));
                }
            }
        }

        public string Family => ExtractFamily(Product);

        public decimal Year2018
        {
            get => _year2018;
            set => SetProperty(ref _year2018, value);
        }

        public decimal Year2019
        {
            get => _year2019;
            set => SetProperty(ref _year2019, value);
        }

        public decimal Year2020
        {
            get => _year2020;
            set => SetProperty(ref _year2020, value);
        }

        public decimal Actual
        {
            get => _actual;
            set
            {
                if (SetProperty(ref _actual, value))
                {
                    OnPropertyChanged(nameof(Delta));
                    OnPropertyChanged(nameof(IsAboveTarget));
                }
            }
        }

        public decimal Target
        {
            get => _target;
            set
            {
                if (SetProperty(ref _target, value))
                {
                    OnPropertyChanged(nameof(Delta));
                    OnPropertyChanged(nameof(IsAboveTarget));
                }
            }
        }

        public decimal Delta => Actual - Target;

        public bool IsAboveTarget => Delta >= 0m;

        public string Error => string.Empty;

        public string this[string columnName]
        {
            get
            {
                switch (columnName)
                {
                    case nameof(Product):
                        return string.IsNullOrWhiteSpace(Product) ? "Product is required." : string.Empty;
                    case nameof(Year2018):
                        return ValidateNumber(Year2018);
                    case nameof(Year2019):
                        return ValidateNumber(Year2019);
                    case nameof(Year2020):
                        return ValidateNumber(Year2020);
                    case nameof(Actual):
                        return ValidateNumber(Actual);
                    case nameof(Target):
                        return Target <= 0m ? "Target must be greater than zero." : string.Empty;
                    default:
                        return string.Empty;
                }
            }
        }

        public object Clone()
        {
            return new DemoSalesRecordViewModel(_id, Product, Year2018, Year2019, Year2020, Actual, Target);
        }

        private static string ValidateNumber(decimal value)
        {
            return value < 0m ? "Value cannot be negative." : string.Empty;
        }

        private static string ExtractFamily(string product)
        {
            if (string.IsNullOrWhiteSpace(product))
            {
                return string.Empty;
            }

            var separatorIndex = product.IndexOf(' ');
            return separatorIndex < 0 ? product : product.Substring(0, separatorIndex);
        }
    }
}
