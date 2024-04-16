namespace SO.Map.Generation
{
    public struct DProvinceClimate
    {
        public DProvinceClimate(float clouds, float moisture)
        {
            this.clouds = clouds;
            this.moisture = moisture;
        }

        public float clouds;
        public float moisture;
    }
}