
using System.Collections.Generic;

using Leopotam.EcsLite;

namespace SO.Map.MapArea
{
    public struct CTMapAreaGeneration
    {
        public CTMapAreaGeneration(
            int a)
        {
            provinceWithoutFreeNeighboursCount = 0;
            tempProvincePEs = new();

            tempNeighbourMAPEs = new();
        }

        public int provinceWithoutFreeNeighboursCount;
        public List<EcsPackedEntity> tempProvincePEs;

        public List<EcsPackedEntity> tempNeighbourMAPEs;
    }
}