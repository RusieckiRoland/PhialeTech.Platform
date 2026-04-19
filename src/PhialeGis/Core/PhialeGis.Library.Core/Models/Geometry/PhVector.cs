using PhialeGis.Library.Core.Models.Geometry.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;

namespace PhialeGis.Library.Core.Models.Geometry
{
    internal class PhVector : IGeometry
    {
        private List<int> _parts;
        private int _last, _count;
        private bool _canGrow;
        private bool _disableEvents;
        private List<PhPoint> _points;

        internal delegate void PhChangeVectorEvent(PhVector sender);

        internal event PhChangeVectorEvent OnChange;

        internal int PartCount => _parts.Count;

        internal PhVector(int size)
        {
            _count = 0;
            _last = size;
            _points = new List<PhPoint>(size);
            _disableEvents = false;
            _canGrow = true;
            _parts = new List<int>();
        }

        internal PhVector(List<PhPoint> points, List<int> parts)
        {
            _count = 0;
            _last = points.Count; //-Roland
            _points = points;
            _disableEvents = false;
            _canGrow = true;
            _parts = parts;
        }

        public void LoadFromStream(Stream stream)
        {
            throw new NotImplementedException();
        }

        // Properties and indexers
        internal int Count => _count;

        internal bool CanGrow { get => _canGrow; set => _canGrow = value; }

        internal PhPoint this[int index]
        {
            get => Get(index);
            set => Put(index, value);
        }

        // Basic methods
        private void Put(int index, PhPoint item)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index cannot be negative.");
            }

            if (index < _points.Count)
            {
                _points[index] = item;
            }
            else
            {
                if (index >= _points.Capacity)
                {
                    if (_canGrow)
                    {
                        _points.Capacity = index + 1;
                    }
                    else
                    {
                        throw new InvalidOperationException("Cannot add more points, vector cannot grow.");
                    }
                }

                while (_points.Count < index)
                {
                    _points.Add(default);
                }

                _points.Add(item);
            }

            _count = _points.Count;
            _last = _count - 1;

            OnChanged();
        }

        private void OnChanged()
        {
            if (!_disableEvents && OnChange != null)
            {
                OnChange(this);
            }
        }

        private PhPoint Get(int index)
        {
            return _points[index];
        }

        internal void Add(PhPoint item)
        {
            _points.Add(item);
        }

        internal void LoadFromWKBStream(MemoryStream stream, int endianess)
        {
            _disableEvents = true;
            Clear();

            // Read the number of parts (rings)
            int numberOfRings = ReadIntFromStream(stream, endianess);

            if (numberOfRings > 1)
            {
                // Handle geometries with multiple rings.
                // For simplicity, this example assumes a single ring geometry.
                // Extend this as per your requirements.
                _disableEvents = false;
                return;
            }

            // Read the number of points
            int numberOfPoints = ReadIntFromStream(stream, endianess);
            SetCapacity(numberOfPoints);

            for (int i = 0; i < numberOfPoints; i++)
            {
                // Read each point
                double x = ReadDoubleFromStream(stream, endianess);
                double y = ReadDoubleFromStream(stream, endianess);
                Add(new PhPoint(x, y));
            }

            _disableEvents = false;
        }

        private int ReadIntFromStream(MemoryStream stream, int endianess)
        {
            byte[] buffer = new byte[4];
            stream.Read(buffer, 0, 4);
            if (endianess == 0)
            {
                Array.Reverse(buffer);
            }
            return BitConverter.ToInt32(buffer, 0);
        }

        private double ReadDoubleFromStream(MemoryStream stream, int endianess)
        {
            byte[] buffer = new byte[8];
            stream.Read(buffer, 0, 8);
            if (endianess == 0)
            {
                Array.Reverse(buffer);
            }
            return BitConverter.ToDouble(buffer, 0);
        }

        private void SetCapacity(int value)
        {
            // Implementation of SetCapacity method
        }

        private void Clear()
        {
            // Implementation of Clear method
        }

        public PhRect GetBoundingBox()
        {
            if (_points.Count == 0)
            {
                // Return an invalid extension or throw an exception if there are no points.
                return GometryConstants.InvalidRect; // Assuming PhRect has a static property for invalid rectangles
            }

            double minX = _points[0].X;
            double minY = _points[0].Y;
            double maxX = minX;
            double maxY = minY;

            foreach (var point in _points)
            {
                if (point.X < minX) minX = point.X;
                if (point.X > maxX) maxX = point.X;
                if (point.Y < minY) minY = point.Y;
                if (point.Y > maxY) maxY = point.Y;
            }

            return new PhRect(minX, minY, maxX, maxY);
        }

        // Other methods and classes...
    }
}
