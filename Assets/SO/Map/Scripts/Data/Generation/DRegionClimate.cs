namespace SO.Map.Generation
{
    public struct DRegionClimate
    {
        public DRegionClimate(float clouds, float moisture)
        {
            this.clouds = clouds;
            this.moisture = moisture;
        }

        public float clouds;
        public float moisture;
    }
}