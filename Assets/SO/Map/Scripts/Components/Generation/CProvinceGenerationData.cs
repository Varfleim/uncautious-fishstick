namespace SO.Map.Generation
{
    public struct CProvinceGenerationData
    {
        public CProvinceGenerationData(
            DProvinceClimate currentClimate, DProvinceClimate nextClimate)
        {
            this.currentClimate = currentClimate;
            this.nextClimate = nextClimate;
        }

        public DProvinceClimate currentClimate;
        public DProvinceClimate nextClimate;
    }
}