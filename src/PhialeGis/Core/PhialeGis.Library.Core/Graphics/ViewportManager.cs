using PhialeGis.Library.Abstractions.Ui.Rendering;
using PhialeGis.Library.Abstractions.Interactions.Input;
using PhialeGis.Library.Core.Models.Geometry;
using PhialeGis.Library.Core.Models.RenderSpace;

//using SkiaSharp;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

/// <summary>
/// Manages the conversion of points between the shape's coordinate system and the device coordinate system.
/// This class is typically used in graphical applications where points, such as geographical coordinates or other
/// spatial data, need to be accurately represented on a device interface, like a WPF control.
///
/// Naming Conventions:
/// - 'Translate' Prefix: Methods converting data from device coordinates back to the model's coordinate system
///   are prefixed with 'Translate'. This convention helps differentiate between methods handling various
///   coordinate transformations.
/// - 'Shape Points': Represents the actual points of a shape in its native coordinate system. These points
///   are the real-world or logical coordinates that need to be mapped to the device's coordinates.
/// - 'Canvas Points': Refers to points adjusted for rendering on the device's interface, such as screen or control
///   points in a WPF application. These points are adapted for the graphical representation of the shape.
/// - 'Api' Prefix: Used to indicate operations that are still within the scope of the device's coordinate system
///   but involve 'double' data types, usually as a result of calculations or transformations.
///
/// This class provides a crucial bridge between the abstract representation of shapes and their practical
/// display on devices, ensuring that spatial data is accurately and effectively rendered according to the
/// specific requirements of the device's display parameters.
/// </summary>
///

namespace PhialeGis.Library.Core.Graphics
{
    public class ViewportManager: IViewport
    {
        /// <summary>
        /// Point per inch
        /// </summary>
        private const int Ppi = 72;

        #region Event handling

        internal delegate void ChangedEventHandler(object sender);

        internal event ChangedEventHandler Changed;

        #endregion Event handling

        /// <summary>
        /// Parameters for transforming model coords to canvas coords
        /// Active window in model coords and scale

        #region Properties

        /// </summary>
        internal PhTransformParams Params { get; set; } = new PhTransformParams(new PhRect(0, 0, 100, 100), new PhPoint(50, 50), 1);

        internal PhRect ActiveWindow => Params.ActiveWindow;
        public double Scale => Params.Scale;

        /// <summary>
        /// Device area eg WPF attached control
        /// </summary>
        internal PhRect DeviceWindow { get; set; } = new PhRect(0, 0, 100, 100);

        /// <summary>
        /// Shortcut to DeviceWinodow
        /// </summary>
        internal PhPoint Dmin => DeviceWindow.Emin;

        internal PhPoint Dmax => DeviceWindow.Emax;

        internal IDevice Device { get; set; }

      

        #endregion Properties

        internal ViewportManager(IDevice device)
        {
            Device = device;

            // Fallback device window if device is null
            var width = (Device != null) ? Device.CurrentWidth : 100;
            var height = (Device != null) ? Device.CurrentHeight : 100;

            DeviceWindow = new PhRect(0, 0, width, height);

            if (Device != null)
            {
                // Subscribe to visual changes (size, DPI, etc.)
                Device.ChangeVisualParams += OnChangeVisualParams;
            }

            ApplyFirstSize(); 
        }


        private void ApplyFirstSize()
        {
            // Use DeviceWindow when device is null
            var w = (Device != null) ? Device.CurrentWidth : DeviceWindow.Width;
            var h = (Device != null) ? Device.CurrentHeight : DeviceWindow.Height;

            var firstTransform = new PhTransformParams();
            firstTransform.Scale = 1;

            // Model space assumed Cartesian (y-up). ActiveWindow covers current device extents.
            firstTransform.ActiveWindow.Emin = new PhPoint(0, 0);
            firstTransform.ActiveWindow.Emax = new PhPoint(w, h); // FIX: Emax, not Emin

            firstTransform.Centroid.X = w / 2.0;
            firstTransform.Centroid.Y = h / 2.0;

            Params = firstTransform;
        }


        private void OnChangeVisualParams(object sender, object e)
        {
            if (Device == null)
                return;

            // Update device window if size actually changed
            if (DeviceWindow.Width != Device.CurrentWidth || DeviceWindow.Height != Device.CurrentHeight)
            {
                DeviceWindow = new PhRect(0, 0, Device.CurrentWidth, Device.CurrentHeight);

                if (ActiveWindow.Width == 0)
                {
                    // First-time init (should not happen after ApplyFirstSize, but safe-guard)
                    ApplyFirstSize();
                }
                else
                {
                    RecalulateWindow();
                }
            }
        }


        private void RecalulateWindow()
        {
            var s = Scale;
            if (s <= 0) s = 1.0;

            // aktualny środek świata
            var cx = (ActiveWindow.Emin.X + ActiveWindow.Emax.X) / 2.0;
            var cy = (ActiveWindow.Emin.Y + ActiveWindow.Emax.Y) / 2.0;

            // nowe wymiary urządzenia (DIP)
            var devW = Math.Abs(Dmax.X - Dmin.X);
            var devH = Math.Abs(Dmax.Y - Dmin.Y);

            // odpowiadające im wymiary w świecie przy niezmienionym Scale
            var halfW = (devW / s) / 2.0;
            var halfH = (devH / s) / 2.0;

            var rect = new PhRect(cx - halfW, cy - halfH, cx + halfW, cy + halfH);

            Params = new PhTransformParams
            {
                ActiveWindow = rect,
                Scale = s,
                Centroid = new PhPoint(cx, cy)
            };

            Debug.WriteLine($"[GEOM] RecalulateWindow → dev=({devW},{devH}) DIP, scale={s}, worldΔ=({rect.DeltaX},{rect.DeltaY})");
        }


        #region Methods for recalculation

        /// <summary>
        /// Translates a point from the model's coordinate system to the canvas (screen) coordinate system.
        /// This method applies scaling and translation based on the active window settings and the scale factor,
        /// effectively mapping a point from the geometrical model space to its corresponding position in the canvas space,
        /// which can be used for rendering in WPF controls.
        /// </summary>
        /// <param name="shapePoint">The point in the model's coordinate system, e.g., coordinates from a WKT (Well-Known Text) representation.</param>
        /// <returns>A Point in the canvas (screen) coordinate system, suitable for graphical representation in WPF controls.</returns>
        internal Point PointToDevice(PhPoint shapePoint)
        {
            double canvasX = Math.Round(Dmin.X + (shapePoint.X - ActiveWindow.Emin.X) * Scale);
            double canvasY = Math.Round(Dmin.Y + (ActiveWindow.Emax.Y - shapePoint.Y) * Scale);
            return new Point(canvasX, canvasY);
        }

        internal Point PointToDevice(double x, double y)
        {
            double canvasX = Math.Round(Dmin.X + (x - ActiveWindow.Emin.X) * Scale);
            double canvasY = Math.Round(Dmin.Y + (ActiveWindow.Emax.Y - y) * Scale);

            Point canvasPoint = new Point(canvasX, canvasY);
            return canvasPoint;
        }

        /// <summary>
        /// Converts a point from the model coordinate system (<see cref="PhPoint"/>) 
        /// to the canvas (device) coordinate system (<see cref="CanvasPoint"/>).
        /// This method applies the current viewport scale and translation so that
        /// the logical model coordinates are mapped precisely to pixel positions
        /// on the rendering surface.
        /// </summary>
        /// <param name="modelPoint">
        /// Point in the model coordinate system (e.g., real-world geometry or logical space).
        /// </param>
        /// <returns>
        /// A <see cref="CanvasPoint"/> representing the translated point 
        /// in device (canvas) coordinates.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal CanvasPoint PhPointToCanvasPoint(PhPoint modelPoint)
        {
            var canvasX = Dmin.X + (modelPoint.X - ActiveWindow.Emin.X) * Scale;
            var canvasY = Dmin.Y + (ActiveWindow.Emax.Y - modelPoint.Y) * Scale;

            CanvasPoint canvasPoint = new CanvasPoint((float)canvasX, (float)canvasY);
            return canvasPoint;
        }


        /// <summary>
        /// Translates a point from the canvas (screen) coordinate system, such as a point within a WPF control,
        /// to the model's coordinate system. This method accounts for the active window's scale and position,
        /// converting the screen-based point to its equivalent location in the model's geometric space.
        /// </summary>
        /// <param name="canvasPoint">The point in the canvas (screen) coordinate system, typically representing a
        /// location within a WPF control, such as the top-left corner.</param>
        /// <returns>A PhPoint representing the translated point in the model's coordinate system.</returns>
        internal PhPoint TranslatePoint(Point canvasPoint)
        {
            // Inverse of: sy = Dmin.Y + (Emax.Y - y) * Scale
            double x = ActiveWindow.Emin.X + (canvasPoint.X - Dmin.X) / Scale;
            double y = ActiveWindow.Emax.Y - (canvasPoint.Y - Dmin.Y) / Scale;
            return new PhPoint(x, y);
        }

        internal PhPoint TranslateUPoint(CorePoint canvasPoint)
        {
            // Keep inverse consistent with PointToDevice/PhPointToCanvasPoint
            double x = ActiveWindow.Emin.X + (canvasPoint.X - Dmin.X) / Scale;
            double y = ActiveWindow.Emax.Y - (canvasPoint.Y - Dmin.Y) / Scale;
            return new PhPoint(x, y);
        }

        /// <summary>
        /// Translates a rectangle from the canvas (screen) coordinate system to the model's coordinate system.
        /// This conversion is achieved by individually translating the top-left and bottom-right points of the
        /// rectangle from screen coordinates to model coordinates. The method is particularly useful for converting
        /// rectangular regions defined in a graphical interface (like a WPF control) into the corresponding geometrical
        /// space of the model. This allows for consistent mapping and manipulation of screen-based selections or areas
        /// into the model's coordinate system.
        /// </summary>
        /// <param name="canvasRect">The rectangle in the canvas (screen) coordinate system, typically representing a
        /// selected or defined area within a WPF control. It is defined by top, left, right, and bottom boundaries.</param>
        /// <returns>A PhRect object representing the translated rectangle in the model's coordinate system. The returned
        /// rectangle is normalized to ensure that the minimum point (Emin) and the maximum point (Emax) correctly represent
        /// the lower-left and upper-right corners of the rectangle, respectively.</returns>

        internal PhRect TranslateRect(Rect canvasRect)
        {
            PhPoint emin = TranslatePoint(new Point(canvasRect.Left, canvasRect.Top));
            PhPoint emax = TranslatePoint(new Point(canvasRect.Right, canvasRect.Bottom));

            PhRect shapeRect = new PhRect(emin, emax);
            return GeometryUtils.NormalizeRectangle(shapeRect);
        }

        /// <summary>
        /// Translates a rectangle from the model's coordinate system to the canvas (screen) coordinate system.
        /// This method first normalizes the given rectangle to ensure the correct orientation of corners.
        /// It then converts the normalized rectangle's minimum and maximum points (Emin and Emax) from model
        /// coordinates to screen coordinates. This is particularly useful for rendering or representing model-defined
        /// geometrical shapes or areas within a graphical interface, such as a WPF control. The conversion accounts
        /// for the current scale and positioning within the active window, ensuring an accurate representation
        /// of the model's geometrical space on the screen.
        /// </summary>
        /// <param name="shapeRect">The rectangle in the model's coordinate system, defined by minimum and maximum points.</param>
        /// <returns>A Rect object representing the translated rectangle in the canvas (screen) coordinate system.
        /// The Rect object is constructed using the top-left and bottom-right canvas points, ensuring accurate
        /// dimensions and positioning on the screen.</returns>
        internal Rect RectToDevice(PhRect shapeRect)
        {
            // Normalize in model space, then convert both corners to screen space.
            PhRect work = GeometryUtils.NormalizeRectangle(shapeRect);

            Point a = PointToDevice(work.Emin);
            Point b = PointToDevice(work.Emax);

            // In screen space (y-down), top = minY, bottom = maxY.
            double left = Math.Min(a.X, b.X);
            double right = Math.Max(a.X, b.X);
            double top = Math.Min(a.Y, b.Y);   // FIX: use top (minY)
            double bottom = Math.Max(a.Y, b.Y);

            return new Rect(
                left,
                top,                               
                right - left,
                bottom - top
            );
        }


        /// <summary>
        /// Converts a rectangle from the model coordinate system (<see cref="PhRect"/>)
        /// to the canvas (device) coordinate system (<see cref="CanvasRect"/>).
        /// The conversion normalizes the model rectangle, transforms its corners
        /// according to the current viewport scale and translation, and constructs
        /// a corresponding rectangle in device space (y-down coordinate system).
        /// </summary>
        /// <param name="modelRect">
        /// Rectangle in the model coordinate system (e.g., logical or geographical space).
        /// </param>
        /// <returns>
        /// A <see cref="CanvasRect"/> representing the rectangle translated
        /// into canvas (device) coordinates, ready for rendering.
        /// </returns>
        internal CanvasRect RectToCanvasRect(PhRect modelRect)
        {
            // Normalize rectangle in model space first (ensures Emin/Emax are ordered)
            PhRect work = GeometryUtils.NormalizeRectangle(modelRect);

            // Convert both corners from model space to canvas space
            CanvasPoint a = PhPointToCanvasPoint(work.Emin);
            CanvasPoint b = PhPointToCanvasPoint(work.Emax);

            // Compute final edges in device (y-down) coordinate system
            float left = (float)Math.Min(a.X, b.X);
            float right = (float)Math.Max(a.X, b.X);
            float top = (float)Math.Min(a.Y, b.Y);
            float bottom = (float)Math.Max(a.Y, b.Y);

            // CanvasRect(left, top, right, bottom) → edges, not width/height
            return new CanvasRect(left, top, right, bottom);
        }



        /// <summary>
        /// Converts a distance from screen units (pixels) to real-world or graph-specific units along the X-axis.
        /// This function is critical for translating measurements taken from a screen-based coordinate system (such as a GUI element)
        /// into the corresponding real-world or graph-specific units used in the model's coordinate system. The conversion process
        /// accounts for the scale and dimensions of the active window to ensure an accurate translation from pixels to the model's units.
        /// </summary>
        /// <param name="canvasDistXCoord">The distance to be converted, measured in screen units (pixels).</param>
        /// <returns>The converted distance in real-world or graph-specific units along the X-axis. The function ensures a valid conversion by
        /// adjusting the divisor (the difference between Dmax.X and Dmin.X) to avoid division by zero, defaulting to 1 if the divisor is zero.</returns>
        internal double TranslateDistXCoord(int canvasDistXCoord)
        {
            double divisor = Math.Abs(Dmax.X - Dmin.X);
            divisor = divisor == 0 ? 1 : divisor;
            return (ActiveWindow.Emax.X - ActiveWindow.Emin.X) / divisor * canvasDistXCoord;
        }

        /// <summary>
        /// Converts a distance measured in model units to screen units along the X-axis.
        /// This conversion takes into account the active window dimensions and the device window dimensions,
        /// scaling the input distance from the model coordinate system to the screen coordinate system.
        /// </summary>
        /// <param name="shapeDistXCoord">The distance in the model's coordinate system (model units) along the X-axis.</param>
        /// <returns>An integer representing the converted distance in screen units along the X-axis.</returns>
        internal int DistToDeviceXCoord(double shapeDistXCoord)
        {
            return Convert.ToInt32(DistToDeviceXCoordAsDouble(shapeDistXCoord));
        }

        /// <summary>
        /// This method is a floating-point variant of the previously implemented method, which returned an integer value.
        /// It offers more precision by returning a floating-point number.
        /// </summary>
        /// <param name="shapeDistXCoord"></param>
        /// <returns></returns>
        internal double DistToDeviceXCoordAsDouble(double shapeDistXCoord)
        {
            if (shapeDistXCoord == 0)
            {
                return 0;
            }
            return shapeDistXCoord / (ActiveWindow.Emax.X - ActiveWindow.Emin.X) * Math.Abs(Dmax.X - Dmin.X);
        }

        /// <summary>
        /// Converts a distance from screen units (pixels) to model units along the Y-axis.
        /// This conversion takes into account the active window dimensions and the device window dimensions,
        /// scaling the input distance from the model coordinate system to the screen coordinate system.
        /// </summary>
        /// <param name="shapeDistYCoord">The distance in the model's coordinate system (model units) along the Y-axis.</param>
        /// <returns>An integer representing the converted distance in screen units along the Y-axis.</returns>

        internal double TranslateDistYCoord(int canvasDistYCoord)
        {
            double divisor = Math.Abs(Dmax.Y - Dmin.Y);
            divisor = divisor == 0 ? 1 : divisor;
            return (ActiveWindow.Emax.Y - ActiveWindow.Emin.Y) / divisor * canvasDistYCoord;
        }

        /// <summary>
        /// Converts a distance measured in model units to screen units along the Y-axis.
        /// This conversion takes into account the active window dimensions and the device window dimensions,
        /// scaling the input distance from the model coordinate system to the screen coordinate system.
        /// </summary>
        /// <param name="shapeDistYCoord">The distance in the model's coordinate system (model units) along the Y-axis.</param>
        /// <returns>An integer representing the converted distance in screen units along the Y-axis.</returns>

        internal int DistToDeviceYCoord(double shapeDistYCoord)
        {
            return Convert.ToInt32(DistToDeviceYCoordAsDouble(shapeDistYCoord));
        }

        /// <summary>
        /// This method is a floating-point variant of the previously implemented method, which returned an integer value.
        /// It offers more precision by returning a floating-point number.
        /// </summary>
        /// <param name="shapeDistXCoord"></param>
        /// <returns></returns>
        ///
        internal double DistToDeviceYCoordAsDouble(double shapeDistYCoord)
        {
            if (shapeDistYCoord == 0)
            {
                return 0;
            }
            return shapeDistYCoord / (ActiveWindow.Emax.Y - ActiveWindow.Emin.Y) * Math.Abs(Dmax.Y - Dmin.Y);
        }

        /// <summary>
        /// Calculate distance in pt to Screen units
        /// </summary>
        /// <param name="apiDistanceX"></param>
        /// <returns></returns>
        internal double PtToDeviceXCoord(double apiDistanceX)
        {
            var dotPerInch = GetSafeDpiX(); // use helper
            return ConvertViewportDistanceToModelX(apiDistanceX * dotPerInch / Ppi);
        }

        /// <summary>
        /// Calculate distance in pt to Screen units
        /// </summary>
        /// <param name="apiDistanceY"></param>
        /// <returns></returns>
        internal double PtToDeviceYCoord(double apiDistanceY)
        {
            var dotPerInch = GetSafeDpiY(); // use helper
            return ConvertViewportDistanceToModelY(apiDistanceY * dotPerInch / Ppi);
        }


        /// <summary>
        /// Converts a horizontal distance from the viewport's coordinate system to the model's coordinate system.
        /// This function is essential for applications where precise mapping of on-screen distances to a data model is required.
        /// It scales the input distance (viewpDistX) from the viewport (such as a UI element) to the model's scale,
        /// based on the width ratio of the active window to the viewport dimensions.
        /// </summary>
        /// <param name="viewpDistX">The horizontal distance in the viewport's coordinate system.</param>
        /// <returns>The equivalent horizontal distance in the model's coordinate system.</returns>
        internal double ConvertViewportDistanceToModelX(double viewpDistX)
        {
            double scaleFactor = (ActiveWindow.Emax.X - ActiveWindow.Emin.X)
                        / Math.Abs(Dmax.X - Dmin.X);
            return viewpDistX * scaleFactor;
        }

        /// <summary>
        /// Converts a vertical distance from the viewport's coordinate system to the model's coordinate system.
        /// This function is crucial for applications that require accurate translation of vertical distances displayed on screen
        /// to their respective positions in a data model, such as in GIS or 3D modeling.
        /// It adjusts the input distance (viewpDistY) from the viewport (e.g., a UI control) to match the scale of the model,
        /// factoring in the height ratio between the active window and the viewport.
        /// </summary>
        /// <param name="viewpDistY">The vertical distance in the viewport's coordinate system.</param>
        /// <returns>The corresponding vertical distance in the model's coordinate system.</returns>
        internal double ConvertViewportDistanceToModelY(double viewpDistY)
        {
            double scaleFactor = (ActiveWindow.Emax.Y - ActiveWindow.Emin.Y)
                        / Math.Abs(Dmax.Y - Dmin.Y);
            return viewpDistY * scaleFactor;
        }

        #endregion Methods for recalculation

        /// <summary>
        /// Updates the active view to a new observation rectangle.
        /// The method normalizes the proposed new active view rectangle,
        /// calculates the proportional change from the current active view,
        /// and applies the necessary transformations to update the view.
        /// This includes adjusting the scale and re-centering the view
        /// based on the dimensions of the new rectangle.
        /// </summary>
        /// <param name="newActiveView">The proposed new active view rectangle.</param>

        internal void ApplyActiveView(PhRect newActiveView)
        {
            try
            {
                Debug.WriteLine("[GEOM] ApplyActiveView START");

                Debug.WriteLine($"[GEOM] Input newActiveView: Emin=({newActiveView.Emin.X}, {newActiveView.Emin.Y}), " +
                                $"Emax=({newActiveView.Emax.X}, {newActiveView.Emax.Y})");

                var normalized = GeometryUtils.NormalizeRectangle(newActiveView);

                // Compute deltas explicitly to be immune to PhRect.DeltaX/DeltaY implementation
                var proposedX = normalized.Emax.X - normalized.Emin.X;
                var proposedY = normalized.Emax.Y - normalized.Emin.Y;

                Debug.WriteLine($"[GEOM] Normalized: Emin=({normalized.Emin.X}, {normalized.Emin.Y}), " +
                                $"Emax=({normalized.Emax.X}, {normalized.Emax.Y}), " +
                                $"Delta=({proposedX}, {proposedY})");

                var activeX = Math.Abs(ActiveWindow.Emax.X - ActiveWindow.Emin.X);
                var activeY = Math.Abs(ActiveWindow.Emax.Y - ActiveWindow.Emin.Y);
                Debug.WriteLine($"[GEOM] ActiveWindow: Delta=({activeX}, {activeY})");

                // Ratios (name them by axis for clarity)
                var ratioX = proposedX / activeX;
                var ratioY = proposedY / activeY;
                var proportion = (ratioX > ratioY) ? ratioX : ratioY;

                Debug.WriteLine($"[GEOM] Proportions: ratioX={ratioX}, ratioY={ratioY}, selected={proportion}");

                var parameters = new PhTransformParams();

                parameters.Scale = Scale / proportion;
                Debug.WriteLine($"[GEOM] Scale: current={Scale}, adjusted={parameters.Scale}");

                // Center horizontally, keep top aligned vertically (as in your original)
                parameters.ActiveWindow.Emin.X = normalized.Emin.X - (activeX * proportion - proposedX) / 2.0;
                parameters.ActiveWindow.Emax.Y = normalized.Emax.Y;

                parameters.ActiveWindow.Emax.X = parameters.ActiveWindow.Emin.X + activeX * proportion;
                parameters.ActiveWindow.Emin.Y = parameters.ActiveWindow.Emax.Y - activeY * proportion;

                parameters.Centroid.X = (parameters.ActiveWindow.Emin.X + parameters.ActiveWindow.Emax.X) / 2.0;
                parameters.Centroid.Y = (parameters.ActiveWindow.Emin.Y + parameters.ActiveWindow.Emax.Y) / 2.0;

                Debug.WriteLine($"[GEOM] Result ActiveWindow: Emin=({parameters.ActiveWindow.Emin.X}, {parameters.ActiveWindow.Emin.Y}), " +
                                $"Emax=({parameters.ActiveWindow.Emax.X}, {parameters.ActiveWindow.Emax.Y})");
                Debug.WriteLine($"[GEOM] Result Centroid=({parameters.Centroid.X}, {parameters.Centroid.Y})");

                Params = parameters;

                Debug.WriteLine("[GEOM] ApplyActiveView END");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GEOM] ApplyActiveView EX: {ex}");
                throw;
            }
        }



        /// <summary>
        /// Applies zoom to the current view based on a specified zoom parameter.
        /// </summary>
        /// <param name="factor">The zoom factor to apply. A value greater than 1 zooms in, less than 1 zooms out.</param>
        /// <returns>
        /// True if the zoom operation is successful (factor is greater than 0); otherwise, false.
        /// </returns>
        /// <remarks>
        /// This method calculates the new scale based on the zoom parameter and adjusts the active window
        /// accordingly to maintain the current view's centroid. The active window is recalculated to reflect
        /// the new zoom level, keeping the view centered around the existing centroid. The method updates the
        /// transformation parameters and triggers the 'Changed' event if the zoom operation is successful.
        /// </remarks>

        public bool Zoom(double factor)
        {
            if (factor <= 0) return false;

            // Current center
            var cx = (ActiveWindow.Emin.X + ActiveWindow.Emax.X) / 2.0;
            var cy = (ActiveWindow.Emin.Y + ActiveWindow.Emax.Y) / 2.0;

            // Compute current world width/height explicitly
            var aw = Math.Abs(ActiveWindow.Emax.X - ActiveWindow.Emin.X);
            var ah = Math.Abs(ActiveWindow.Emax.Y - ActiveWindow.Emin.Y);

            // New scale and corresponding new half-extents in world units
            var newScale = Scale * factor;
            var halfW = aw * Scale / newScale / 2.0;
            var halfH = ah * Scale / newScale / 2.0;

            var rect = new PhRect(cx - halfW, cy - halfH, cx + halfW, cy + halfH);

            var p = new PhTransformParams
            {
                ActiveWindow = rect,
                Scale = newScale,
                Centroid = new PhPoint(cx, cy)
            };

            Params = p;
            OnChanged();
            return true;
        }


        //public void ZoomPoint(double scale, UniversalPoint point) -Roland
        //{
        //    PhPoint phpoint = TranslateUPoint(point);
        //    UpdateCentroid(phpoint);
        //    Zoom(scale);
        //    OnChanged();
        //}

        /// <summary>
        /// Updates the centroid of the current view to a new position.
        /// </summary>
        /// <param name="newCentroid">The new centroid position to set.</param>
        /// <remarks>
        /// This method recalculates the active window based on the new centroid while maintaining its current size.
        /// It creates a new rectangle (localRect) centered around the new centroid with the same width and height as the current view.
        /// The transformation parameters are then updated with this new rectangle, the current scale, and the new centroid.
        /// A 'Changed' event is triggered if it's assigned, indicating that the view has been updated.
        /// </remarks>

        internal void UpdateCentroid(PhPoint newCentroid)
        {
            // Use explicit width/height for robustness
            var halfW = Math.Abs(ActiveWindow.Emax.X - ActiveWindow.Emin.X) / 2.0;
            var halfH = Math.Abs(ActiveWindow.Emax.Y - ActiveWindow.Emin.Y) / 2.0;

            var rect = new PhRect(
                newCentroid.X - halfW,
                newCentroid.Y - halfH,
                newCentroid.X + halfW,
                newCentroid.Y + halfH);

            var p = new PhTransformParams
            {
                ActiveWindow = rect,
                Scale = Scale,
                Centroid = newCentroid
            };

            Params = p;
            OnChanged();
        }


        /// <summary>
        /// Scrolls the viewport vertically by the specified offset.
        /// </summary>
        /// <param name="verticalOffset">The vertical offset by which to scroll.</param>
        /// <remarks>
        /// This method calculates the vertical movement in the model's coordinate space and adjusts the active window's vertical position accordingly.
        /// It updates the vertical position of the centroid based on the new boundaries of the active window. After modifying the transformation parameters to reflect this vertical scroll, it triggers a 'Changed' event if assigned.
        /// </remarks>

        internal void ScrollVertically(double verticalOffset)
        {
            var movement = ConvertViewportDistanceToModelY(verticalOffset);
            var tmpParams = Params;
            GeometryUtils.AddOffsetY(ref tmpParams.ActiveWindow, movement);
            tmpParams.Centroid.Y = tmpParams.ActiveWindow.Emin.Y + (tmpParams.ActiveWindow.Emax.Y - tmpParams.ActiveWindow.Emin.Y) / 2;
            Params = tmpParams;
            OnChanged(); 
        }

        /// <summary>
        /// Scrolls the viewport horizontally by the specified offset.
        /// </summary>
        /// <param name="horizontalOffset">The horizontal offset by which to scroll.</param>
        /// <remarks>
        /// This method computes the horizontal movement in the model's coordinate space, modifying the active window's horizontal position.
        /// It recalculates the centroid's horizontal position to align with the new active window boundaries. The transformation parameters are then updated to reflect this horizontal movement. A 'Changed' event is invoked to signify that the viewport has been scrolled horizontally.
        /// </remarks>

        internal void ScrollHorizontally(double horizontalOffset)
        {
            var movement = ConvertViewportDistanceToModelX(horizontalOffset);
            var tmpParams = Params;
            GeometryUtils.AddOffsetX(ref tmpParams.ActiveWindow, movement);
            tmpParams.Centroid.X = tmpParams.ActiveWindow.Emin.X + (tmpParams.ActiveWindow.Emax.X - tmpParams.ActiveWindow.Emin.X) / 2;
            Params = tmpParams;
            OnChanged(); 
        }

        internal void ScrollByScreenOffset(double horizontalOffest, double verticalOffset)
        {
            MoveByScreenOffset(horizontalOffest, verticalOffset);
            OnChanged(); 
        }

        private void MoveByScreenOffset(double horizontalOffest, double verticalOffset)
        {
            // Convert screen-space delta to model-space delta (note: y-down on screen).
            var movementX = ConvertViewportDistanceToModelX(horizontalOffest);
            var movementY = ConvertViewportDistanceToModelY(verticalOffset);   // FIX: was using X

            var tmpParams = Params;
            GeometryUtils.AddOffset(ref tmpParams.ActiveWindow, movementX, movementY);
            tmpParams.Centroid.X = tmpParams.ActiveWindow.Emin.X + (tmpParams.ActiveWindow.Emax.X - tmpParams.ActiveWindow.Emin.X) / 2;
            tmpParams.Centroid.Y = tmpParams.ActiveWindow.Emin.Y + (tmpParams.ActiveWindow.Emax.Y - tmpParams.ActiveWindow.Emin.Y) / 2;
            Params = tmpParams;
        }

        

        public bool PanByScreenOffset(double dx, double dy)
        {
            double invertedHorizontalOffset = dx * -1;
            ScrollByScreenOffset(invertedHorizontalOffset, dy);
            return true;
        }

        internal virtual void OnChanged()
        {
            ChangedEventHandler handler = Changed;
            if (handler != null)
            {
                handler(this);
            }
        }

        
        private double GetSafeDpiX()
        {
            if (Device == null) return 96.0;
            var dpi = Device.GetDpiX();
            return dpi > 0 ? dpi : 96.0;
        }

        private double GetSafeDpiY()
        {
            if (Device == null) return 96.0;
            var dpi = Device.GetDpiY();
            return dpi > 0 ? dpi : 96.0;
        }

        
        internal void Detach()
        {
            if (Device != null)
                Device.ChangeVisualParams -= OnChangeVisualParams;
        }

        public double GetDpiX()
        {
            return GetSafeDpiX();
        }

        public double GetDpiY()
        {
            return GetSafeDpiY();
        }

        // Model → Screen wrapper used by retained-mode renderer.
        // Uses the same math as PointToDevice/PhPointToCanvasPoint (y inversion included).
        public void ModelToScreen(double modelX, double modelY, out float screenX, out float screenY)
        {
            var sx = Dmin.X + (modelX - ActiveWindow.Emin.X) * Scale;
            var sy = Dmin.Y + (ActiveWindow.Emax.Y - modelY) * Scale;
            screenX = (float)sx;
            screenY = (float)sy;
        }

        /// <summary>
        /// Returns model→screen affine transform coefficients for the current viewport.
        /// Formula (Y-up model → Y-down device):
        ///   sx = m11 * x + m12 * y + tx
        ///   sy = m21 * x + m22 * y + ty
        /// which corresponds to:
        ///   sx = Dmin.X + (x - Emin.X) * Scale
        ///   sy = Dmin.Y + (Emax.Y - y) * Scale
        /// ⇒ m11 =  s, m12 =  0, tx = Dmin.X - Emin.X * s
        ///    m21 =  0, m22 = -s, ty = Dmin.Y + Emax.Y * s
        /// </summary>
        public void GetModelToScreenAffine(
            out double m11, out double m12,
            out double m21, out double m22,
            out double tx, out double ty)
        {
            var s = Scale;
            var ex0 = ActiveWindow.Emin.X;
            var ey1 = ActiveWindow.Emax.Y;
            var dx0 = Dmin.X;
            var dy0 = Dmin.Y;

            m11 = s; m12 = 0.0;
            m21 = 0.0; m22 = -s;
            tx = dx0 - ex0 * s;
            ty = dy0 + ey1 * s;
        }

        /// <summary>
        /// Returns screen→model affine transform coefficients (inverse of GetModelToScreenAffine).
        /// For non-zero Scale:
        ///   x = (sx - tx) / m11  when m12=0
        ///   y = (sy - ty) / m22  when m21=0
        /// Derived inverse (Y-down device → Y-up model):
        ///   x = (sx - Dmin.X) / s + Emin.X
        ///   y =  Emax.Y - (sy - Dmin.Y) / s
        /// ⇒ m11 =  1/s, m12 =    0,  tx = -Dmin.X/s + Emin.X
        ///    m21 =    0, m22 = -1/s, ty =  Emax.Y + Dmin.Y/s
        /// If Scale == 0, returns a safe no-op mapping (all zeros) to avoid division by zero.
        /// </summary>
        public void GetScreenToModelAffine(
            out double m11, out double m12,
            out double m21, out double m22,
            out double tx, out double ty)
        {
            var s = Scale;
            if (Math.Abs(s) < 1e-12)
            {
                // Safe guard: degenerate scale → return zeroed coefficients
                m11 = m12 = m21 = m22 = tx = ty = 0.0;
                return;
            }

            var invS = 1.0 / s;

            var ex0 = ActiveWindow.Emin.X;
            var ey1 = ActiveWindow.Emax.Y;
            var dx0 = Dmin.X;
            var dy0 = Dmin.Y;

            m11 = invS; m12 = 0.0;
            m21 = 0.0; m22 = -invS;
            tx = -dx0 * invS + ex0;
            ty = ey1 + dy0 * invS;
        }



    }
}
