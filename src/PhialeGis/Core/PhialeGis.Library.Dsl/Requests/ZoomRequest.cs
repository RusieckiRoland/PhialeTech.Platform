namespace PhialeGis.Library.Dsl.Requests
{
    /// <summary>
    /// Represents a DSL command to change the zoom level of the map.
    /// </summary>
    public class ZoomRequest
    {
        public double Value { get; set; }

        public ZoomRequest(double value)
        {
            Value = value;
        }
    }
}
