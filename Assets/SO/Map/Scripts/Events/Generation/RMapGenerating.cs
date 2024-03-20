namespace SO.Map.Generation
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