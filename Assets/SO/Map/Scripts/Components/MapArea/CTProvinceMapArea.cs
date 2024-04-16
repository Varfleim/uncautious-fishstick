
using System.Collections.Generic;

using Leopotam.EcsLite;

namespace SO.Map.MapArea
{
    public struct CTProvinceMapArea
    {
        public CTProvinceMapArea(
            EcsPackedEntity selfPE)
        {
            this.selfPE = selfPE;

            parentMAPE = new();

            neighboursWithMACount = 0;

            neighbourMAPEs = new();
        }

        public readonly EcsPackedEntity selfPE;

        public EcsPackedEntity parentMAPE;

        public int neighboursWithMACount;

        public List<EcsPackedEntity> neighbourMAPEs;
    }
}