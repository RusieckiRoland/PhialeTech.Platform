namespace PhialeGis.Library.Abstractions.Actions
{
    /// <summary>
    /// Result for AddLineStringAction without re-parsing.
    /// Flattened model-space points: [x1,y1,x2,y2,...].
    /// </summary>
    public sealed class LineStringActionResult
    {
        public double[] Points { get; set; } = new double[0];

        public object TargetDraw { get; set; }

        public string LayerId { get; set; }

        public string LayerHint { get; set; }
    }
}
