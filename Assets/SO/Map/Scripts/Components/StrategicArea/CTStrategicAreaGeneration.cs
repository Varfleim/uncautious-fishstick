
using System.Collections.Generic;

using Leopotam.EcsLite;

namespace SO.Map.StrategicArea
{
    public struct CTStrategicAreaGeneration
    {
        public CTStrategicAreaGeneration(
            int a)
        {
            regionWithoutFreeNeighboursCount = 0;
            tempRegionPEs = new();

            tempNeighbourSAPEs = new();
        }

        public int regionWithoutFreeNeighboursCount;
        public List<EcsPackedEntity> tempRegionPEs;

        public List<EcsPackedEntity> tempNeighbourSAPEs;
    }
}