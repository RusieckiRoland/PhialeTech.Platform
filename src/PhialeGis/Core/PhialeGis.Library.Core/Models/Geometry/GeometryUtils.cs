using PhialeGis.Library.Core.Enums;
using System;

namespace PhialeGis.Library.Core.Models.Geometry
{
    /// <summary>
    /// This collection of functions represents .NET implementations of various algorithms.
    ///
    /// One key aspect of these implementations is the avoidance of direct memory manipulation techniques,
    /// like memory comparison, commonly used in lower-level programming languages. This approach aligns with the best
    /// practices of .NET development, emphasizing code safety and maintainability.
    /// While this might have implications for the raw execution speed compared to direct memory operations, the overall
    /// efficiency and integration within the .NET ecosystem are prioritized. These implementations aim to strike a balance
    /// between performance and the robust, high-level features of the .NET framework.
    /// </summary>

    internal static class GeometryUtils
    {
        internal static Rect ReorderRect(Rect R)
        {
            double left, right, top, bottom;

            left = Math.Min(R.Left, R.Right);
            right = Math.Max(R.Left, R.Right);

            top = Math.Min(R.Top, R.Bottom);
            bottom = Math.Max(R.Top, R.Bottom);

            return new Rect(new Point(left, top), new Point(right, bottom));
        }

        internal static void InflateRect2D(ref Rect R, double dx, double dy)
        {
            R = new Rect(R.Left - dx, R.Top - dy, R.Width + 2 * dx, R.Height + 2 * dy);
        }

        internal static Point SetRealToPoint(PhPoint p)
        {
            return new Point((int)Math.Round(p.X), (int)Math.Round(p.Y));
        }

        internal static PhPoint SetPointToReal(Point p)
        {
            return new PhPoint(p.X, p.Y);
        }

        internal static PhRect SetRectToReal(Rect rect)
        {
            return new PhRect
            {
                Emin = new PhPoint(rect.Left, rect.Top),
                Emax = new PhPoint(rect.Right, rect.Bottom)
            };
        }

        internal static bool EqualMatrix2D(PhMatrix m1, PhMatrix m2)
        {
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (m1[i, j] != m2[i, j])
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        internal static PhMatrix MultiplyMatrix2D(PhMatrix m, PhMatrix t)
        {
            if (EqualMatrix2D(m, GometryConstants.IdentityMatrix))
            {
                return t;
            }
            if (EqualMatrix2D(t, GometryConstants.IdentityMatrix))
            {
                return m;
            }

            PhMatrix result = new PhMatrix(new double[3, 3]);

            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    result[r, c] = m[r, 0] * t[0, c] + m[r, 1] * t[1, c] + m[r, 2] * t[2, c];
                }
            }

            return result;
        }

        internal static PhPoint TransformPoint2D(PhPoint p, PhMatrix t)
        {
            // Check if the transformation matrix is the identity matrix
            if (EqualMatrix2D(t, GometryConstants.IdentityMatrix))
            {
                return p; // No transformation needed
            }

            // Apply the matrix transformation
            double x = t[0, 0] * p.X + t[0, 1] * p.Y + t[0, 2];
            double y = t[1, 0] * p.X + t[1, 1] * p.Y + t[1, 2];

            return new PhPoint(x, y);
        }

        internal static PhRect TransformRect2D(PhRect r, PhMatrix t)
        {
            return new PhRect
            {
                Emin = TransformPoint2D(r.Emin, t),
                Emax = TransformPoint2D(r.Emax, t)
            };
        }

        internal static PhMatrix Translate2D(double tx, double ty)
        {
            PhMatrix result = GometryConstants.IdentityMatrix;
            result[0, 2] = tx;
            result[1, 2] = ty;

            return result;
        }

        internal static PhMatrix Rotate2D(double phi, PhPoint refPt)
        {
            PhMatrix result = GometryConstants.IdentityMatrix;
            result[0, 0] = Math.Cos(phi);
            result[0, 1] = -Math.Sin(phi);
            result[0, 2] = refPt.X * (1 - Math.Cos(phi)) + refPt.Y * Math.Sin(phi);
            result[1, 0] = Math.Sin(phi);
            result[1, 1] = Math.Cos(phi);
            result[1, 2] = refPt.Y * (1 - Math.Cos(phi)) - refPt.X * Math.Sin(phi);

            return result;
        }

        internal static PhMatrix Scale2D(double sx, double sy, PhPoint refPt)
        {
            PhMatrix result = GometryConstants.IdentityMatrix;
            result[0, 0] = sx;
            result[0, 2] = (1 - sx) * refPt.X;
            result[1, 1] = sy;
            result[1, 2] = (1 - sy) * refPt.Y;

            return result;
        }

        internal static void Matrix3x3PreMultiply(PhMatrix m, ref PhMatrix t)
        {
            PhMatrix tmp = new PhMatrix(new double[3, 3]);

            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    tmp[r, c] = m[r, 0] * t[0, c] + m[r, 1] * t[1, c] + m[r, 2] * t[2, c];
                }
            }

            t = tmp;
        }

        internal static PhMatrix BuildTransformationMatrix(double sx, double sy, double phi, double tx, double ty, PhPoint refPt)
        {
            PhMatrix result = GometryConstants.IdentityMatrix;

            if (sx != 1 || sy != 1)
            {
                Matrix3x3PreMultiply(Scale2D(sx, sy, refPt), ref result);
            }

            if (phi != 0)
            {
                Matrix3x3PreMultiply(Rotate2D(phi, refPt), ref result);
            }

            if (tx != 0 || ty != 0)
            {
                Matrix3x3PreMultiply(Translate2D(tx, ty), ref result);
            }

            return result;
        }

        internal static double Angle2D(PhPoint p1, PhPoint p2)
        {
            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;

            if (dx == 0 && dy == 0) return 0;
            if (dx == 0) return dy > 0 ? Math.PI / 2 : -Math.PI / 2;

            return Math.Atan2(dy, dx);
        }

        internal static bool EqualPoint(PhPoint p1, PhPoint p2)
        {
            return p1.Equals(p2);
        }

        internal static bool EqualRect2D(PhRect r1, PhRect r2)
        {
            return r1.Equals(r2);
        }

        internal static double Dist2D(PhPoint pt1, PhPoint pt2)
        {
            return Math.Sqrt(Math.Pow(pt1.X - pt2.X, 2) + Math.Pow(pt1.Y - pt2.Y, 2));
        }

        internal static bool FuzzEqualPoint2D(PhPoint p1, PhPoint p2)
        {
            return Math.Abs(Dist2D(p1, p2)) < GometryConstants.FuzzValue;
        }

        internal static int BoxFilling2D(PhRect box1, PhRect box2)
        {
            int tmp1, tmp2;

            if (box2.Emax.X != box2.Emin.X)
                tmp1 = (int)Math.Round((box1.Emax.X - box1.Emin.X) / (box2.Emax.X - box2.Emin.X) * 1000);
            else
                tmp1 = 0;

            if (box2.Emax.Y != box2.Emin.Y)
                tmp2 = (int)Math.Round((box1.Emax.Y - box1.Emin.Y) / (box2.Emax.Y - box2.Emin.Y) * 1000);
            else
                tmp2 = 0;

            return Math.Max(tmp1, tmp2);
        }

        internal static bool IsRectVisible(PhRect aRect, PhRect aClip)
        {
            if (aRect.Emin.X > aClip.Emax.X || aRect.Emax.X < aClip.Emin.X ||
                aRect.Emin.Y > aClip.Emax.Y || aRect.Emax.Y < aClip.Emin.Y)
            {
                return false;
            }

            if (aRect.Emin.Equals(aRect.Emax) && IsPointInBox2D(aRect.Emin, aClip))
            {
                return true;
            }

            // Check if the filling of aRect in aClip meets the minimum draw limit
            return BoxFilling2D(aRect, aClip) >= GometryConstants.MinDrawLimit;
        }

        internal static bool IsPointInBox2D(PhPoint pt, PhRect box)
        {
            return PositionCode2D(box, pt) == RelativePosition.None;
        }

        internal static bool IsRectVisibleForPlace(PhRect aRect, PhRect aClip)
        {
            if (aRect.Emin.X > aClip.Emax.X || aRect.Emax.X < aClip.Emin.X ||
                aRect.Emin.Y > aClip.Emax.Y || aRect.Emax.Y < aClip.Emin.Y)
            {
                return false;
            }

            if (aRect.Emin.Equals(aRect.Emax) && IsPointInBox2D(aRect.Emin, aClip))
            {
                return true;
            }

            return true;
        }

        internal static RelativePosition PositionCode2D(PhRect clip, PhPoint p)
        {
            RelativePosition result = RelativePosition.None;

            if (clip.Emin.X < clip.Emax.X)
            {
                if (p.X < clip.Emin.X)
                    result = RelativePosition.Left;
                else if (p.X > clip.Emax.X)
                    result = RelativePosition.Right;
            }
            else
            {
                if (p.X > clip.Emin.X)
                    result = RelativePosition.Left;
                else if (p.X < clip.Emax.X)
                    result = RelativePosition.Right;
            }

            if (clip.Emin.Y < clip.Emax.Y)
            {
                if (p.Y < clip.Emin.Y)
                    result |= RelativePosition.Bottom;
                else if (p.Y > clip.Emax.Y)
                    result |= RelativePosition.Top;
            }
            else
            {
                if (p.Y > clip.Emin.Y)
                    result |= RelativePosition.Bottom;
                else if (p.Y < clip.Emax.Y)
                    result |= RelativePosition.Top;
            }

            return result;
        }

        internal static bool ClipPt(double denom, double num, ref double tE, ref double tL)
        {
            if (denom > 0)
            {
                double t = num / denom;
                if (t > tL)
                    return false;
                if (t > tE)
                    tE = t;
            }
            else if (denom < 0)
            {
                double t = num / denom;
                if (t < tE)
                    return false;
                if (t < tL)
                    tL = t;
            }
            else if (num > 0)
                return false;

            return true;
        }

        internal static ClipCodes ClipLine2D(PhRect clip, ref double x1, ref double y1, ref double x2, ref double y2)
        {
            double dx = x2 - x1;
            double dy = y2 - y1;
            ClipCodes result = ClipCodes.ccNotVisible;

            if (dx == 0 && dy == 0 && IsPointInBox2D(new PhPoint(x1, y1), clip))
            {
                result = ClipCodes.ccVisible;
                return result;
            }

            double tE = 0.0;
            double tL = 1.0;

            if (ClipPt(dx, clip.Emin.X - x1, ref tE, ref tL))
            {
                if (ClipPt(-dx, x1 - clip.Emax.X, ref tE, ref tL))
                {
                    if (ClipPt(dy, clip.Emin.Y - y1, ref tE, ref tL))
                    {
                        if (ClipPt(-dy, y1 - clip.Emax.Y, ref tE, ref tL))
                        {
                            result = 0; // Equivalent to an empty set
                            if (tL < 1)
                            {
                                x2 = x1 + tL * dx;
                                y2 = y1 + tL * dy;
                                result |= ClipCodes.ccSecond;
                            }
                            if (tE > 0)
                            {
                                x1 = x1 + tE * dx;
                                y1 = y1 + tE * dy;
                                result |= ClipCodes.ccFirst;
                            }
                            if (result == 0)
                            {
                                result = ClipCodes.ccVisible;
                            }
                        }
                    }
                }
            }

            return result;
        }

        internal static double Defuzz(double x)
        {
            if (Math.Abs(x) < GometryConstants.FuzzValue)
                return 0.0;
            else
                return x;
        }

        internal static double DeOne(double x)
        {
            const double Tolerance = GometryConstants.FuzzValue;

            if (Math.Abs(x - 1) < Tolerance)
                return 1.0;
            else if (Math.Abs(x + 1) < Tolerance)
                return -1.0;
            else
                return x;
        }

        internal static ClipCodes ClipLineLeftRight2D(PhRect clip, ref double x1, ref double y1, ref double x2, ref double y2)
        {
            double dx = x2 - x1;
            double dy = y2 - y1;
            ClipCodes result = ClipCodes.ccNotVisible;

            if (dx == 0 && dy == 0 && IsPointInBox2D(new PhPoint(x1, y1), clip))
            {
                result = ClipCodes.ccVisible;
                return result;
            }

            double tE = 0.0;
            double tL = 1.0;

            if (ClipPt(dx, clip.Emin.X - x1, ref tE, ref tL))
            {
                if (ClipPt(-dx, x1 - clip.Emax.X, ref tE, ref tL))
                {
                    result = 0; // Equivalent to an empty set
                    if (tL < 1)
                    {
                        x2 = x1 + tL * dx;
                        y2 = y1 + tL * dy;
                        result |= ClipCodes.ccSecond;
                    }
                    if (tE > 0)
                    {
                        x1 = x1 + tE * dx;
                        y1 = y1 + tE * dy;
                        result |= ClipCodes.ccFirst;
                    }
                    if (result == 0)
                    {
                        result = ClipCodes.ccVisible;
                    }
                }
            }

            return result;
        }

        internal static ClipCodes ClipLineUpBottom2D(PhRect clip, ref double x1, ref double y1, ref double x2, ref double y2)
        {
            double dx = x2 - x1;
            double dy = y2 - y1;
            ClipCodes result = ClipCodes.ccNotVisible;

            if (dx == 0 && dy == 0 && IsPointInBox2D(new PhPoint(x1, y1), clip))
            {
                result = ClipCodes.ccVisible;
                return result;
            }

            double tE = 0.0;
            double tL = 1.0;

            if (ClipPt(dy, clip.Emin.Y - y1, ref tE, ref tL))
            {
                if (ClipPt(-dy, y1 - clip.Emax.Y, ref tE, ref tL))
                {
                    result = 0; // Equivalent to an empty set
                    if (tL < 1)
                    {
                        x2 = x1 + tL * dx;
                        y2 = y1 + tL * dy;
                        result |= ClipCodes.ccSecond;
                    }
                    if (tE > 0)
                    {
                        x1 = x1 + tE * dx;
                        y1 = y1 + tE * dy;
                        result |= ClipCodes.ccFirst;
                    }
                    if (result == 0)
                    {
                        result = ClipCodes.ccVisible;
                    }
                }
            }

            return result;
        }

        internal static bool IsBoxInBox2D(PhRect box1, PhRect box2)
        {
            RelativePosition fCode = PositionCode2D(box2, box1.Emin);
            RelativePosition sCode = PositionCode2D(box2, box1.Emax);
            return ((int)fCode & (int)sCode) == 0;
        }

        internal static bool IsBoxFullInBox2D(PhRect box1, PhRect box2)
        {
            RelativePosition fCode = PositionCode2D(box2, box1.Emin);
            RelativePosition sCode = PositionCode2D(box2, box1.Emax);
            return fCode == 0 && sCode == 0;
        }

        internal static PhRect ReOrderRect2D(PhRect R)
        {
            double minX, maxX, minY, maxY;

            if (R.Emin.X < R.Emax.X)
            {
                minX = R.Emin.X;
                maxX = R.Emax.X;
            }
            else
            {
                minX = R.Emax.X;
                maxX = R.Emin.X;
            }

            if (R.Emin.Y < R.Emax.Y)
            {
                minY = R.Emin.Y;
                maxY = R.Emax.Y;
            }
            else
            {
                minY = R.Emax.Y;
                maxY = R.Emin.Y;
            }

            PhRect result = new PhRect
            {
                Emin = new PhPoint(minX, minY),
                Emax = new PhPoint(maxX, maxY)
            };

            return result;
        }

        internal static PhRect BoxOutBox2D(PhRect box1, PhRect box2)
        {
            box1 = ReOrderRect2D(box1);
            box2 = ReOrderRect2D(box2);
            var result = box1;

            if (box2.Emin.X < box1.Emin.X)
                result.Emin.X = box2.Emin.X;
            if (box2.Emax.X > box1.Emax.X)
                result.Emax.X = box2.Emax.X;
            if (box2.Emin.Y < box1.Emin.Y)
                result.Emin.Y = box2.Emin.Y;
            if (box2.Emax.Y > box1.Emax.Y)
                result.Emax.Y = box2.Emax.Y;

            return result;
        }

        internal static bool IsNearPoint2D(PhPoint rp, PhPoint p, double aperture, out double dist)
        {
            PhRect tmpBox = new PhRect(
                new PhPoint(rp.X - aperture, rp.Y - aperture),
                new PhPoint(rp.X + aperture, rp.Y + aperture)
            );

            bool result = PositionCode2D(tmpBox, p) == 0;
            dist = result ? Math.Sqrt(Math.Pow(p.X - rp.X, 2) + Math.Pow(p.Y - rp.Y, 2)) : 0;
            return result;
        }

        internal static PhRect Rect2D(double axMin, double ayMin, double axMax, double ayMax)
        {
            return new PhRect(
                new PhPoint(axMin, ayMin), // Emin
                new PhPoint(axMax, ayMax)  // Emax
            );
        }

        internal static Rect SetRealToRect(PhRect r)
        {
            int left = (int)Math.Round(r.Emin.X);
            int right = (int)Math.Round(r.Emax.X);
            int top = (int)Math.Round(Math.Min(r.Emin.Y, r.Emax.Y));
            int bottom = (int)Math.Round(Math.Max(r.Emin.Y, r.Emax.Y));

            return new Rect(left, top, right, bottom);
        }

        internal static PhMatrix MirrorAroundX()
        {
            double[,] identityMatrixValues = new double[,]
            {
                 { 1, 0, 0 },
                 { 0, 1, 0 },  //I love this stuff
                 { 0, 0, 1 }
            };

            PhMatrix result = new PhMatrix(identityMatrixValues);
            result[1, 1] = -1;

            return result;
        }

        internal static bool RightIntersection2D(PhPoint p, PhPoint p1, PhPoint p2)
        {
            bool isBetweenY = (p.Y >= p1.Y && p.Y < p2.Y) || (p.Y < p1.Y && p.Y >= p2.Y);
            if (!isBetweenY)
                return false;

            double r = (p.Y - p1.Y) * (p2.X - p1.X) / (p2.Y - p1.Y) + p1.X;
            return p.X <= r;
        }

        internal static PhRect TransformBoundingBox2D(PhRect box, PhMatrix matrix)
        {
            PhRect box1 = TransformRect2D(box, matrix);
            PhRect box2 = TransformRect2D(new PhRect(new PhPoint(box.Emin.X, box.Emax.Y),
                new PhPoint(box.Emax.X, box.Emin.Y)), matrix);
            return BoxOutBox2D(box1, box2);
        }

        internal static PhPoint ChangeToOrtogonal(PhPoint pt1, PhPoint pt2)
        {
            PhPoint result = pt2;
            if (Math.Abs(pt2.X - pt1.X) > Math.Abs(pt2.Y - pt1.Y))
                result.Y = pt1.Y;
            else
                result.X = pt1.X;

            return result;
        }

        internal static bool EQ(double a, double b)
        {
            return Math.Abs(a - b) <= GometryConstants.DOUBLE_EPSILON;
        }

        /// <summary>
        /// Determines the relationship between two lines (partition line from P1 to P2 and division line from D1 to D2).
        /// If the lines are not parallel, calculates their intersection point.
        /// The intersection point might not be within the actual segments of the lines.
        /// </summary>
        /// <param name="P1">Start point of the partition line.</param>
        /// <param name="P2">End point of the partition line.</param>
        /// <param name="D1">Start point of the division line.</param>
        /// <param name="D2">End point of the division line.</param>
        /// <param name="P">Intersection point, if lines are not parallel.</param>
        /// <returns>LineRelations indicating the relationship between the two lines.</returns>
        internal static LineRelations LineRel(PhPoint P1, PhPoint P2, PhPoint D1, PhPoint D2, out PhPoint P)
        {
            P = new PhPoint();
            double denominator, numerator, fraction;
            double dxPar, dyPar, dxDiv, dyDiv;

            dxPar = P2.X - P1.X;
            dyPar = P2.Y - P1.Y;
            dxDiv = D2.X - D1.X;
            dyDiv = D2.Y - D1.Y;

            LineRelations result = LineRelations.None;

            // Compute for division line to partition line
            denominator = (dyPar * dxDiv) - (dxPar * dyDiv);
            numerator = ((P1.X - D1.X) * dyPar) + ((D1.Y - P1.Y) * dxPar);

            if (EQ(denominator, 0.0))  // Parallel
            {
                result |= LineRelations.Parallel;
                if (EQ(numerator, 0.0))
                {
                    // Coincident lines
                }
                else if (numerator > 0.0)
                {
                    result |= LineRelations.DivToRight;
                }
                else if (numerator < 0.0)
                {
                    result |= LineRelations.DivToLeft;
                }
            }
            else  // Not parallel, compute intersection
            {
                fraction = numerator / denominator;
                double xInt = D1.X + dxDiv * fraction;
                double yInt = D1.Y + dyDiv * fraction;
                P = new PhPoint(xInt, yInt);

                // Checks for intersection point's position relative to the division line
                // ...

                if (fraction < 0.0) result |= LineRelations.OffDivStart;
                else if (EQ(fraction, 0.0)) result |= LineRelations.AtDivStart;
                else if (fraction > 1.0) result |= LineRelations.OffDivEnd;
                else if (EQ(fraction, 1.0)) result |= LineRelations.AtDivEnd;
                else result |= LineRelations.BetweenDiv;
            }

            // Compute for partition line to division line
            denominator = (dyDiv * dxPar) - (dxDiv * dyPar);
            numerator = ((D1.X - P1.X) * dyDiv) + ((P1.Y - D1.Y) * dxDiv);

            if (EQ(denominator, 0.0))  // Parallel
            {
                if (EQ(numerator, 0.0))
                {
                    // Coincident lines
                }
                else if (numerator > 0.0)
                {
                    result |= LineRelations.ParToRight;
                }
                else if (numerator < 0.0)
                {
                    result |= LineRelations.ParToLeft;
                }
            }
            else  // Not parallel
            {
                fraction = numerator / denominator;

                // Checks for intersection point's position relative to the partition line
                // ...

                if (fraction < 0.0) result |= LineRelations.OffParStart;
                else if (EQ(fraction, 0.0)) result |= LineRelations.AtParStart;
                else if (fraction > 1.0) result |= LineRelations.OffParEnd;
                else if (EQ(fraction, 1.0)) result |= LineRelations.AtParEnd;
                else result |= LineRelations.BetweenPar;
            }

            return result;
        }

        internal static PhRect NormalizeRectangle(PhRect rect)
        {
            double minX, minY, maxX, maxY;

            if (rect.Emin.X < rect.Emax.X)
            {
                minX = rect.Emin.X;
                maxX = rect.Emax.X;
            }
            else
            {
                minX = rect.Emax.X;
                maxX = rect.Emin.X;
            }

            if (rect.Emin.Y < rect.Emax.Y)
            {
                minY = rect.Emin.Y;
                maxY = rect.Emax.Y;
            }
            else
            {
                minY = rect.Emax.Y;
                maxY = rect.Emin.Y;
            }

            PhPoint minPoint = new PhPoint(minX, minY);
            PhPoint maxPoint = new PhPoint(maxX, maxY);

            return new PhRect(minPoint, maxPoint);
        }

        internal static void AddOffsetX(ref PhRect phRect, double horizontalOffset)
        {
            phRect.Emin.X += horizontalOffset;
            phRect.Emax.X += horizontalOffset;
        }

        internal static void AddOffsetY(ref PhRect phRect, double verticallOffset)
        {
            phRect.Emin.Y += verticallOffset;
            phRect.Emax.Y += verticallOffset;
        }

        internal static void AddOffset(ref PhRect phRect, double horizontalOffset, double verticallOffset)
        {
            AddOffsetX(ref phRect, horizontalOffset);
            AddOffsetY(ref phRect, verticallOffset);
        }

        internal static Point CalculateRectCenter(Rect rect)
        {
            double centerX = rect.Left + rect.Width / 2;
            double centerY = rect.Top + rect.Height / 2;
            return new Point(centerX, centerY);
        }
    }
}