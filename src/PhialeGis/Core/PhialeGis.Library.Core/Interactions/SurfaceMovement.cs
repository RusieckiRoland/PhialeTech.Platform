namespace PhialeGis.Library.Core.Interactions
{
    public sealed class SurfaceMovement
    {
        public double XMovement { get; private set; }
        public double YMovement { get; private set; }

        public SurfaceMovement(double xMovement, double yMovement)
        {
            XMovement = xMovement;
            YMovement = yMovement;
        }
    }
}