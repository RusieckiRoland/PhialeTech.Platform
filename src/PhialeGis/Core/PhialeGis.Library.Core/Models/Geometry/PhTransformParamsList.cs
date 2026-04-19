using System;
using System.Collections.Generic;

namespace PhialeGis.Library.Core.Models.Geometry
{
    internal class PhTransformParamsList
    {
        private List<PhTransformParams> _list;

        internal PhTransformParamsList()
        {
            _list = new List<PhTransformParams>();
        }

        internal PhTransformParams this[int index]
        {
            get => _list[index];
            set => _list[index] = value;
        }

        internal int Capacity
        {
            get => _list.Capacity;
            set => _list.Capacity = value;
        }

        internal int Count => _list.Count;

        internal void Add(PhTransformParams item)
        {
            _list.Add(item);
        }

        internal void Clear()
        {
            _list.Clear();
        }

        internal void Delete(int index)
        {
            if (index >= 0 && index < _list.Count)
            {
                _list.RemoveAt(index);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index was out of range. Must be non-negative and less than the size of the collection.");
            }
        }

        // Additional methods as necessary...
    }
}