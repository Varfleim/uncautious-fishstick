namespace SCM.Map
{
    public struct DRegionBiome
    {
        public DRegionBiome(
            int terrainTypeIndex,
            int plant)
        {
            this.terrainTypeIndex = terrainTypeIndex;

            this.plant = plant;
        }

        public int terrainTypeIndex;

        public int plant;
    }
}