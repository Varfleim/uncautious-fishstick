
using Leopotam.EcsLite;

namespace SO.Map.Generation
{
    public readonly struct RProvinceInitializerOwner
    {
        public RProvinceInitializerOwner(
            EcsPackedEntity ownerCountryPE)
        {
            this.ownerCountryPE = ownerCountryPE;
        }

        public readonly EcsPackedEntity ownerCountryPE;
    }
}