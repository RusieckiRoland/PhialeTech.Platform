using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using PhialeGis.Library.Abstractions.Interactions;
using PhialeGis.Library.Abstractions.Ui.Rendering;
using PhialeGis.Library.Actions.Ogc;
using PhialeGis.Library.Core.Interfaces;
using PhialeGis.Library.Domain.Map;
using PhialeGis.Library.Dsl.Requests;
using PhialeGis.Library.Geometry.IO.FlatGeobuf;

namespace PhialeGis.Library.Dsl.Api
{
    /// <summary>
    /// Executes DSL request DTOs against the GIS domain model and viewport.
    /// C# 7.3 compatible.
    /// </summary>
    public sealed class MapContext
    {
        private readonly PhGis _gis;
        private readonly IViewport _viewport;
        private readonly IGraphicsFacade _graphics;
        private IGisInteractionManager _manager;

        // Default zoom steps for ZOOMIN/ZOOMOUT.
        private const double DefaultZoomInFactor = 1.2;            // 20% in
        private const double DefaultZoomOutFactor = 1.0 / 1.2;     // ~16.7% out

        public MapContext(PhGis gis, IViewport viewport, IGraphicsFacade graphics)
        {
            _gis = gis ?? throw new ArgumentNullException(nameof(gis));
            _viewport = viewport ?? throw new ArgumentNullException(nameof(viewport));
            _graphics = graphics ?? throw new ArgumentNullException(nameof(graphics));
        }

        /// <summary>
        /// Dispatches a single request DTO.
        /// </summary>
        public void Execute(object request, object target, IEditorInteractive source)
        {
            // Layer creation (existing).
            var add = request as AddLayerRequest;
            if (add != null) { ExecuteAddLayer(add); return; }

            // Explicit vector import (existing).
            var imp = request as ImportLayerFromFileRequest;
            if (imp != null) { ExecuteImportLayerFromFile(imp); return; }

            // ---- Navigation commands ----

            // ZOOM numeric
            var zoom = request as ZoomRequest;
            if (zoom != null) { ExecuteZoom(zoom); return; }

            // ZOOMIN / ZOOMOUT / ZOOMALL
            if (request is ZoomInRequest) { ExecuteZoomIn(); return; }
            if (request is ZoomOutRequest) { ExecuteZoomOut(); return; }
            if (request is ZoomAllRequest) { ExecuteZoomAll(); return; }

            // PAN from (x0,y0) to (x1,y1) in screen space (pixels)
            var pan = request as PanRequest;
            if (pan != null) { ExecutePan(pan); return; }

            

            if (request is AddLinestringRequest) {
                _manager.StartInteractiveAction(new AddLineStringAction(), target, source);
                return;
            }

            Console.WriteLine("[MapContext] Unknown request type: " +
                              (request != null ? request.GetType().Name : "<null>"));
        }

        public void AttachManager(IGisInteractionManager manager)
        {
            if (manager == null) throw new ArgumentNullException(nameof(manager));
            _manager = manager;
                    
        }


        /* =======================
         * Navigation handlers
         * ======================= */

        /// <summary>
        /// Applies a relative zoom factor. Values &gt; 1 zoom in; 0 &lt; factor &lt; 1 zoom out.
        /// Negative or zero factors are rejected.
        /// </summary>
        private void ExecuteZoom(ZoomRequest r)
        {
            if (r == null) return;

            if (r.Value <= 0)
            {
                Console.WriteLine("[MapContext] Zoom skipped. Factor must be > 0.");
                return;
            }

            var applied = _viewport.Zoom(r.Value);
            if (!applied)
            {
                Console.WriteLine("[MapContext] Zoom not applied (factor invalid or clamped).");
                return;
            }

            _graphics.Invalidate();
        }

        /// <summary>Zooms in by a default step.</summary>
        private void ExecuteZoomIn()
        {
            var applied = _viewport.Zoom(DefaultZoomInFactor);
            if (!applied)
            {
                Console.WriteLine("[MapContext] ZoomIn not applied.");
                return;
            }
            _graphics.Invalidate();
        }

        /// <summary>Zooms out by a default step.</summary>
        private void ExecuteZoomOut()
        {
            var applied = _viewport.Zoom(DefaultZoomOutFactor);
            if (!applied)
            {
                Console.WriteLine("[MapContext] ZoomOut not applied.");
                return;
            }
            _graphics.Invalidate();
        }

        /// <summary>
        /// Zooms to the full extent.
        /// NOTE: A robust implementation requires a world-extent provider (e.g., union bbox of visible layers).
        /// For now, this method logs an informative message unless a facade method is available.
        /// </summary>
        private void ExecuteZoomAll()
        {
            // If your IGraphicsFacade exposes ApplyVisualWindow(minX, minY, maxX, maxY),
            // you can compute a project bbox here and call it. Otherwise we log a message.
            Console.WriteLine("[MapContext] ZoomAll not implemented: no extent provider wired in MapContext.");
            // Optional: _graphics.Invalidate(); // only if viewport/window is actually changed
        }

        /// <summary>
        /// Pans using two screen-space points: delta = To - From (in pixels).
        /// Requires IViewport.PanByScreenOffset(dx, dy).
        /// </summary>
        private void ExecutePan(PanRequest p)
        {
            if (p == null) return;

            var dx = p.ToX - p.FromX;
            var dy = p.ToY - p.FromY;

            var applied = _viewport.PanByScreenOffset(dx, dy);
            if (!applied)
            {
                Console.WriteLine("[MapContext] Pan not applied.");
                return;
            }

            _graphics.Invalidate();
        }

        /* =======================
         * Import handler (existing)
         * ======================= */

        /// <summary>
        /// Creates a new layer and populates it from a file. Supported: .fgb (FlatGeobuf).
        /// Requirements:
        /// - Define conditional symbol PH_FGB in the project that compiles PhFlatGeobufStore.
        /// - Add NuGets: FlatGeobuf + NetTopologySuite to the Geometry project.
        /// </summary>
        private void ExecuteImportLayerFromFile(ImportLayerFromFileRequest imp)
        {
            if (imp == null || string.IsNullOrWhiteSpace(imp.Path)) return;

            var fullPath = Path.GetFullPath(imp.Path);
            if (!File.Exists(fullPath))
            {
                Console.WriteLine("[MapContext] Import skipped. File not found: " + fullPath);
                return;
            }

            var name = !string.IsNullOrWhiteSpace(imp.Name)
                ? imp.Name
                : Path.GetFileNameWithoutExtension(fullPath);

            var layer = new PhLayer(name, PhLayerType.Memory);
            layer.Visible = imp.Visible.HasValue ? imp.Visible.Value : true;
            layer.Opacity = imp.Opacity.HasValue ? imp.Opacity.Value : 1.0d;

            var ext = Path.GetExtension(fullPath).ToLowerInvariant();
            if (ext == ".fgb")
            {
                long nextId = 1;
                foreach (var f in PhFlatGeobufStore.Read(fullPath))
                {
                    if (f.Id == 0) f.Id = nextId++; // Ensure non-zero Ids for downstream systems
                    layer.AddFeature(f);
                }
            }
            else
            {
                throw new NotSupportedException("Unsupported vector format: " + ext);
            }

            TrySetMetadata(layer, "Source", fullPath);
            if (!string.IsNullOrWhiteSpace(imp.Crs)) TrySetMetadata(layer, "Crs", imp.Crs);

            _gis.AddLayer(layer);
            _graphics.Invalidate();
        }

        /* =======================
         * AddLayer handler (existing)
         * ======================= */

        private void ExecuteAddLayer(AddLayerRequest add)
        {
            // Canonical defaults per spec
            var id = string.IsNullOrWhiteSpace(add.Id) ? GenerateIdFromLabel(add.Label) : add.Id;
            var table = string.IsNullOrWhiteSpace(add.Table) ? id : add.Table;
            var geomCol = string.IsNullOrWhiteSpace(add.GeometryCol) ? "Geometry" : add.GeometryCol;
            var geomType = string.IsNullOrWhiteSpace(add.GeometryType) ? "Geometry" : add.GeometryType;
            var dim = string.IsNullOrWhiteSpace(add.Dim) ? "XY" : add.Dim;
            var source = string.IsNullOrWhiteSpace(add.Source) ? GetDefaultProjectSourceUri() : add.Source;
            var crs = add.Crs; // may be null; domain can validate

            var selectable = add.Selectable.HasValue ? add.Selectable.Value : true;
            var visible = add.Visible.HasValue ? add.Visible.Value : true;
            var opacity = add.Opacity.HasValue ? add.Opacity.Value : 1.0;

            var layerType = MapLayerType(add.Type);

            var layer = new PhLayer(id, layerType)
            {
                Visible = visible,
                Opacity = opacity
            };

            // Set metadata if your domain exposes such properties (reflection keeps coupling low).
            TrySetMetadata(layer, "Label", add.Label);
            TrySetMetadata(layer, "Table", table);
            TrySetMetadata(layer, "GeometryColumn", geomCol);
            TrySetMetadata(layer, "GeometryType", geomType);
            TrySetMetadata(layer, "Dimension", dim);
            TrySetMetadata(layer, "Source", source);
            TrySetMetadata(layer, "Crs", crs);
            TrySetFlag(layer, "Selectable", selectable);

            _gis.AddLayer(layer);
            _graphics.Invalidate();
        }

        /* =======================
         * Helpers (C# 7.3)
         * ======================= */

        private static PhLayerType MapLayerType(string t)
        {
            var s = (t ?? string.Empty).Trim().ToUpperInvariant();
            switch (s)
            {
                case "SPATIALITE": return PhLayerType.Spatialite;
                case "POSTGIS": return PhLayerType.Postgis;
                default: return PhLayerType.Memory;
            }
        }

        /// <summary>
        /// Generates a deterministic ID from a label:
        /// 1) strip diacritics, 2) keep letters/digits/space/underscore,
        /// 3) PascalCase, 4) prefix '_' if first char is not a letter/_.
        /// </summary>
        private static string GenerateIdFromLabel(string label)
        {
            if (string.IsNullOrWhiteSpace(label)) return "Layer";

            // 1) strip diacritics
            var normalized = label.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);
            for (int i = 0; i < normalized.Length; i++)
            {
                var c = normalized[i];
                var cat = CharUnicodeInfo.GetUnicodeCategory(c);
                if (cat != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            var noDiacritics = sb.ToString().Normalize(NormalizationForm.FormC);

            // 2) keep letters/digits/space/underscore
            var filtered = Regex.Replace(noDiacritics, @"[^A-Za-z0-9 _]+", " ").Trim();

            // 3) PascalCase
            var parts = Regex.Split(filtered, @"[\s_]+");
            var idBuilder = new StringBuilder();
            for (int i = 0; i < parts.Length; i++)
            {
                var p = parts[i];
                if (string.IsNullOrEmpty(p)) continue;
                var first = char.ToUpperInvariant(p[0]);
                idBuilder.Append(first);
                if (p.Length > 1) idBuilder.Append(p.Substring(1));
            }
            var id = idBuilder.Length > 0 ? idBuilder.ToString() : "Layer";

            // 4) ensure leading char is letter or '_'
            if (!(char.IsLetter(id[0]) || id[0] == '_'))
                id = "_" + id;

            return id;
        }

        /// <summary>
        /// Returns the default project SQLite URI (used when SOURCE is not provided).
        /// Replace with an injected provider in production.
        /// </summary>
        private static string GetDefaultProjectSourceUri()
        {
            // TODO: connect to your project settings and resolve a proper path.
            return "sqlite:///project.phiale";
        }

        /// <summary>
        /// Tries to set a string metadata property on the layer (via reflection).
        /// </summary>
        private static void TrySetMetadata(object layer, string propertyName, string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            var prop = layer.GetType().GetProperty(propertyName);
            if (prop != null && prop.CanWrite && prop.PropertyType == typeof(string))
            {
                prop.SetValue(layer, value, null);
            }
        }

        /// <summary>
        /// Tries to set a boolean metadata property on the layer (via reflection).
        /// </summary>
        private static void TrySetFlag(object layer, string propertyName, bool value)
        {
            var prop = layer.GetType().GetProperty(propertyName);
            if (prop != null && prop.CanWrite && prop.PropertyType == typeof(bool))
            {
                prop.SetValue(layer, value, null);
            }
        }
    }
}
