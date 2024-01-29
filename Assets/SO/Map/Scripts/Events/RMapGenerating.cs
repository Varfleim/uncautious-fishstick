namespace SO.Map.Events
{
    public readonly struct RMapGenerating
    {
        public RMapGenerating(
            int subdivisions)
        {
            this.subdivisions = subdivisions;
        }

        public readonly int subdivisions;
    }
}