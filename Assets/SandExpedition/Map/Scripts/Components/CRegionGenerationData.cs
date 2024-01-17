namespace SCM.Map
{
    public struct CRegionGenerationData
    {
        public CRegionGenerationData(DRegionClimate currentClimate, DRegionClimate nextClimate)
        {
            this.currentClimate = currentClimate;
            this.nextClimate = nextClimate;
        }

        public DRegionClimate currentClimate;
        public DRegionClimate nextClimate;
    }
}