// PhialeGis.Library.Sync/Io/FgbLayerLoader.cs
using System;
using System.IO;
using PhialeGis.Library.Domain.Map;
using PhialeGis.Library.Geometry.Abstractions;
using PhialeGis.Library.Geometry.IO.FlatGeobuf;

namespace PhialeGis.Library.Sync.Io
{
    /// <summary>
    /// Creates a new GIS layer from a FlatGeobuf (.fgb) file and adds it to a PhGis instance.
    /// This is a thin composition over PhFlatGeobufStore.Read(...).
    /// </summary>
    public static class FgbLayerLoader
    {
        /// <summary>
        /// Reads a FlatGeobuf file and appends a new layer to the given PhGis model.
        /// </summary>
        /// <param name="gis">GIS model that will receive the new layer.</param>
        /// <param name="path">Absolute or relative path to a .fgb file.</param>
        /// <returns>The created layer instance.</returns>
        public static PhLayer AddLayerFromFgb(PhGis gis, string path)
        {
            if (gis == null) throw new ArgumentNullException(nameof(gis));
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentNullException(nameof(path));
            if (!File.Exists(path)) 
                throw new FileNotFoundException("FGB file not found.", path);

            // Layer name from file name (without extension)
            var layerName = Path.GetFileNameWithoutExtension(path);

            // Pick the most appropriate type for your app; Memory is simple & safe.
            var layer = new PhLayer(layerName, PhLayerType.Memory)
            {
                Visible = true,
                Opacity = 1.0
            };

            // Read PhFeature objects from FGB and add them to the layer.
            // PhFlatGeobufStore.Read(...) yields IPhFeature with Geometry (Single/Multi/Collection)
            foreach (IPhFeature f in PhFlatGeobufStore.Read(path))
            {
                layer.AddFeature(f);
            }

            // Attach to the GIS model
            gis.AddLayer(layer);
            return layer;
        }
        

        /// <summary>
        /// Reads features from a FlatGeobuf stream and creates a new layer.
        /// Caller owns the stream lifetime (this method does NOT dispose the stream).
        /// </summary>
        /// <param name="gis">Target GIS model.</param>
        /// <param name="stream">Readable stream positioned at the beginning of a FlatGeobuf file.</param>
        /// <param name="layerName">Name for the new layer (fallbacks to "FGB Layer").</param>
        /// <returns>The created and attached layer.</returns>
        public static PhLayer AddLayerFromFgb(PhGis gis, Stream stream, string layerName)
        {
            if (gis == null) throw new ArgumentNullException(nameof(gis));
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (!stream.CanRead) throw new ArgumentException("Stream must be readable.", nameof(stream));
            if (string.IsNullOrWhiteSpace(layerName)) layerName = "FGB Layer";

            // Ensure the stream is at the beginning if seeking is supported.
            if (stream.CanSeek && stream.Position != 0)
                stream.Seek(0, SeekOrigin.Begin);

            var layer = new PhLayer(layerName, PhLayerType.Memory)
            {
                Visible = true,
                Opacity = 1.0
            };

            try
            {
                // Enumerate features eagerly so the caller can safely dispose the stream afterwards.
                foreach (IPhFeature f in PhFlatGeobufStore.Read(stream))
                {
                    if (f != null) layer.AddFeature(f);
                }
            }
            catch (NotSupportedException nse)
            {
                // Typical when PH_FGB symbol is not defined or required packages are missing.
                throw new InvalidOperationException(
                    "FlatGeobuf reading is not enabled. Ensure the PH_FGB compile symbol is defined " +
                    "and the FlatGeobuf + NetTopologySuite packages are referenced in this project.", nse);
            }
            catch (Exception ex)
            {
                // Provide context without swallowing the original exception.
                throw new IOException("Failed to read FlatGeobuf data from the provided stream.", ex);
            }

            gis.AddLayer(layer);
            return layer;
        }

    }
}
