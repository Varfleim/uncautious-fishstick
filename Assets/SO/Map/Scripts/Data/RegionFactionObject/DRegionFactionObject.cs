
using Leopotam.EcsLite;

namespace SO.Map
{
    public struct DRegionFactionObject
    {
        public DRegionFactionObject(
            EcsPackedEntity rFOPE)
        {
            this.rFOPE = rFOPE;
        }

        public EcsPackedEntity rFOPE;
    }
}