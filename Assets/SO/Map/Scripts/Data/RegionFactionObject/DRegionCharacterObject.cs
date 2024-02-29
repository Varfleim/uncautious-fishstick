
using Leopotam.EcsLite;

namespace SO.Map
{
    public struct DRegionCharacterObject
    {
        public DRegionCharacterObject(
            EcsPackedEntity rFOPE)
        {
            this.rFOPE = rFOPE;
        }

        public EcsPackedEntity rFOPE;
    }
}