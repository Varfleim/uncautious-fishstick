
using Leopotam.EcsLite;

namespace SO.Map.Generation
{
    public readonly struct RRegionInitializerOwner
    {
        public RRegionInitializerOwner(
            EcsPackedEntity ownerCountryPE)
        {
            this.ownerCountryPE = ownerCountryPE;
        }

        public readonly EcsPackedEntity ownerCountryPE;
    }
}