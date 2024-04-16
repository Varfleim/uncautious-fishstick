namespace SO.Map.Generation
{
    public struct DProvinceBiome
    {
        public DProvinceBiome(
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