namespace SCM.Map.Events
{
    public readonly struct RMapGeneration
    {
        public RMapGeneration(
            int subdivisions)
        {
            this.subdivisions = subdivisions;
        }

        public readonly int subdivisions;
    }
}