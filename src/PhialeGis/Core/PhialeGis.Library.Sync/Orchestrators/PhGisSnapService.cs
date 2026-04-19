using System;
using System.Collections.Generic;
using PhialeGis.Library.Abstractions.Interactions;
using PhialeGis.Library.Domain.Map;
using PhialeGis.Library.Geometry.Abstractions;
using PhialeGis.Library.Geometry.Spatial.Multi;
using PhialeGis.Library.Geometry.Spatial.Primitives;
using PhialeGis.Library.Geometry.Spatial.Single;

namespace PhialeGis.Library.Sync.Orchestrators
{
    internal sealed class PhGisSnapService : ISnapService
    {
        private readonly PhGis _gis;
        private readonly double _gridStep;

        public PhGisSnapService(PhGis gis, double gridStep = 10d)
        {
            _gis = gis ?? throw new ArgumentNullException(nameof(gis));
            _gridStep = gridStep <= 0d ? 10d : gridStep;
        }

        public bool TrySnap(SnapRequest request, out SnapResult result)
        {
            result = null;
            if (request == null)
                return false;

            var toleranceModel = ResolveModelTolerance(request);
            var best = FindBestGeometrySnap(request, toleranceModel);
            var grid = FindGridSnap(request, toleranceModel);

            if (IsBetter(grid, best))
                best = grid;

            result = best;
            return result != null && result.HasSnap;
        }

        private SnapResult FindBestGeometrySnap(SnapRequest request, double toleranceModel)
        {
            if (_gis.Layers == null || _gis.Layers.Count == 0)
                return null;

            SnapResult best = null;
            for (var i = 0; i < _gis.Layers.Count; i++)
            {
                var layer = _gis.Layers[i];
                if (layer == null || !layer.Visible || !layer.Snappable)
                    continue;

                var features = layer.Features;
                if (features == null)
                    continue;

                for (var f = 0; f < features.Count; f++)
                {
                    var feature = features[f];
                    var candidate = FindFeatureSnap(request, toleranceModel, layer.Name, feature);
                    if (IsBetter(candidate, best))
                        best = candidate;
                }
            }

            return best;
        }

        private SnapResult FindFeatureSnap(SnapRequest request, double toleranceModel, string layerName, IPhFeature feature)
        {
            if (feature?.Geometry == null)
                return null;

            switch (feature.Geometry.Kind)
            {
                case PhGeometryKind.Point:
                    return ConsiderPoint((PhPointEntity)feature.Geometry, request, toleranceModel, layerName, feature.Id);
                case PhGeometryKind.LineString:
                    return ConsiderLine(((PhPolyLine)feature.Geometry).Points, request, toleranceModel, layerName, feature.Id, closed: false);
                case PhGeometryKind.Polygon:
                    return ConsiderPolygon((PhPolygon)feature.Geometry, request, toleranceModel, layerName, feature.Id);
                case PhGeometryKind.MultiPoint:
                    return ConsiderMultiPoint((PhMultiPoint)feature.Geometry, request, toleranceModel, layerName, feature.Id);
                case PhGeometryKind.MultiLineString:
                    return ConsiderMultiLine((PhMultiLineString)feature.Geometry, request, toleranceModel, layerName, feature.Id);
                case PhGeometryKind.MultiPolygon:
                    return ConsiderMultiPolygon((PhMultiPolygon)feature.Geometry, request, toleranceModel, layerName, feature.Id);
                default:
                    return null;
            }
        }

        private SnapResult ConsiderPoint(PhPointEntity pointEntity, SnapRequest request, double toleranceModel, string layerName, long featureId)
        {
            return CreatePointCandidate(pointEntity.Point, SnapKind.Vertex, request, toleranceModel, layerName, featureId);
        }

        private SnapResult ConsiderMultiPoint(PhMultiPoint multiPoint, SnapRequest request, double toleranceModel, string layerName, long featureId)
        {
            SnapResult best = null;
            for (var i = 0; i < multiPoint.Points.Count; i++)
            {
                var candidate = CreatePointCandidate(multiPoint.Points[i], SnapKind.Vertex, request, toleranceModel, layerName, featureId);
                if (IsBetter(candidate, best))
                    best = candidate;
            }

            return best;
        }

        private SnapResult ConsiderMultiLine(PhMultiLineString multiLine, SnapRequest request, double toleranceModel, string layerName, long featureId)
        {
            SnapResult best = null;
            for (var i = 0; i < multiLine.Lines.Count; i++)
            {
                var candidate = ConsiderLine(multiLine.Lines[i].Points, request, toleranceModel, layerName, featureId, closed: false);
                if (IsBetter(candidate, best))
                    best = candidate;
            }

            return best;
        }

        private SnapResult ConsiderMultiPolygon(PhMultiPolygon multiPolygon, SnapRequest request, double toleranceModel, string layerName, long featureId)
        {
            SnapResult best = null;
            for (var i = 0; i < multiPolygon.Polygons.Count; i++)
            {
                var candidate = ConsiderPolygon(multiPolygon.Polygons[i], request, toleranceModel, layerName, featureId);
                if (IsBetter(candidate, best))
                    best = candidate;
            }

            return best;
        }

        private SnapResult ConsiderPolygon(PhPolygon polygon, SnapRequest request, double toleranceModel, string layerName, long featureId)
        {
            SnapResult best = ConsiderLine(polygon.Outer, request, toleranceModel, layerName, featureId, closed: true);
            if (polygon.Holes == null)
                return best;

            for (var i = 0; i < polygon.Holes.Count; i++)
            {
                var candidate = ConsiderLine(polygon.Holes[i], request, toleranceModel, layerName, featureId, closed: true);
                if (IsBetter(candidate, best))
                    best = candidate;
            }

            return best;
        }

        private SnapResult ConsiderLine(IList<PhPoint> points, SnapRequest request, double toleranceModel, string layerName, long featureId, bool closed)
        {
            if (points == null || points.Count == 0)
                return null;

            SnapResult best = null;
            var segmentCount = closed ? points.Count : points.Count - 1;
            if (segmentCount < 0)
                segmentCount = 0;

            for (var i = 0; i < points.Count; i++)
            {
                var kind = ResolveVertexKind(points, i, closed);
                if (kind == SnapKind.Endpoint && !HasMode(request.Modes, SnapModes.Endpoint))
                    kind = SnapKind.Vertex;

                if ((kind == SnapKind.Vertex && HasMode(request.Modes, SnapModes.Vertex)) ||
                    (kind == SnapKind.Endpoint && HasMode(request.Modes, SnapModes.Endpoint)))
                {
                    var candidate = CreatePointCandidate(points[i], kind, request, toleranceModel, layerName, featureId);
                    if (IsBetter(candidate, best))
                        best = candidate;
                }
            }

            for (var i = 0; i < segmentCount; i++)
            {
                var a = points[i];
                var b = points[(i + 1) % points.Count];

                if (HasMode(request.Modes, SnapModes.Midpoint))
                {
                    var midpoint = new PhPoint((a.X + b.X) * 0.5d, (a.Y + b.Y) * 0.5d);
                    var midpointCandidate = CreatePointCandidate(midpoint, SnapKind.Midpoint, request, toleranceModel, layerName, featureId);
                    if (IsBetter(midpointCandidate, best))
                        best = midpointCandidate;
                }

                if (HasMode(request.Modes, SnapModes.NearestOnSegment))
                {
                    var nearestCandidate = CreateNearestOnSegmentCandidate(a, b, request, toleranceModel, layerName, featureId);
                    if (IsBetter(nearestCandidate, best))
                        best = nearestCandidate;
                }
            }

            return best;
        }

        private SnapResult FindGridSnap(SnapRequest request, double toleranceModel)
        {
            if (!HasMode(request.Modes, SnapModes.Grid))
                return null;

            var x = Math.Round(request.ModelX / _gridStep) * _gridStep;
            var y = Math.Round(request.ModelY / _gridStep) * _gridStep;
            var dx = request.ModelX - x;
            var dy = request.ModelY - y;
            var dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist > toleranceModel)
                return null;

            return new SnapResult
            {
                HasSnap = true,
                X = x,
                Y = y,
                Kind = SnapKind.Grid,
                DistanceModel = dist
            };
        }

        private static bool HasMode(SnapModes current, SnapModes required)
        {
            return (current & required) == required;
        }

        private static SnapKind ResolveVertexKind(IList<PhPoint> points, int index, bool closed)
        {
            if (!closed && (index == 0 || index == points.Count - 1))
                return SnapKind.Endpoint;

            return SnapKind.Vertex;
        }

        private static SnapResult CreatePointCandidate(PhPoint point, SnapKind kind, SnapRequest request, double toleranceModel, string layerName, long featureId)
        {
            var dx = request.ModelX - point.X;
            var dy = request.ModelY - point.Y;
            var dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist > toleranceModel)
                return null;

            return new SnapResult
            {
                HasSnap = true,
                X = point.X,
                Y = point.Y,
                Kind = kind,
                LayerId = layerName,
                FeatureId = featureId,
                DistanceModel = dist
            };
        }

        private static SnapResult CreateNearestOnSegmentCandidate(PhPoint a, PhPoint b, SnapRequest request, double toleranceModel, string layerName, long featureId)
        {
            var abx = b.X - a.X;
            var aby = b.Y - a.Y;
            var len2 = abx * abx + aby * aby;
            if (len2 <= double.Epsilon)
                return null;

            var apx = request.ModelX - a.X;
            var apy = request.ModelY - a.Y;
            var t = (abx * apx + aby * apy) / len2;
            if (t < 0d)
                t = 0d;
            else if (t > 1d)
                t = 1d;

            var x = a.X + abx * t;
            var y = a.Y + aby * t;
            var dx = request.ModelX - x;
            var dy = request.ModelY - y;
            var dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist > toleranceModel)
                return null;

            return new SnapResult
            {
                HasSnap = true,
                X = x,
                Y = y,
                Kind = SnapKind.NearestOnSegment,
                LayerId = layerName,
                FeatureId = featureId,
                DistanceModel = dist
            };
        }

        private static bool IsBetter(SnapResult candidate, SnapResult current)
        {
            if (candidate == null || !candidate.HasSnap)
                return false;

            if (current == null || !current.HasSnap)
                return true;

            var candidatePriority = GetPriority(candidate.Kind);
            var currentPriority = GetPriority(current.Kind);
            if (candidatePriority != currentPriority)
                return candidatePriority < currentPriority;

            return candidate.DistanceModel < current.DistanceModel;
        }

        private static int GetPriority(SnapKind kind)
        {
            switch (kind)
            {
                case SnapKind.Endpoint:
                    return 0;
                case SnapKind.Vertex:
                    return 1;
                case SnapKind.Midpoint:
                    return 2;
                case SnapKind.NearestOnSegment:
                    return 3;
                case SnapKind.Grid:
                    return 4;
                default:
                    return 100;
            }
        }

        private static double ResolveModelTolerance(SnapRequest request)
        {
            var scale = request.Viewport?.Scale ?? 1d;
            if (scale <= double.Epsilon)
                scale = 1d;

            return request.TolerancePx / scale;
        }
    }
}
