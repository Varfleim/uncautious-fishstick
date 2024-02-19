
using System.Collections.Generic;

using Leopotam.EcsLite;

namespace SO.Map.StrategicArea
{
    public struct CTRegionStrategicArea
    {
        public CTRegionStrategicArea(
            EcsPackedEntity selfPE)
        {
            this.selfPE = selfPE;

            parentSAPE = new();

            neighboursWithSACount = 0;

            neighbourSAPEs = new();
        }

        public readonly EcsPackedEntity selfPE;

        public EcsPackedEntity parentSAPE;

        public int neighboursWithSACount;

        public List<EcsPackedEntity> neighbourSAPEs;
    }
}