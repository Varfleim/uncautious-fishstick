
using Leopotam.EcsLite;

namespace SO.Map.Events
{
    public readonly struct ERegionChangeOwner
    {
        public ERegionChangeOwner(
            EcsPackedEntity regionPE,
            EcsPackedEntity newOwnerCountryPE, EcsPackedEntity oldOwnerCountryPE)
        {
            this.regionPE = regionPE;

            this.newOwnerCountryPE = newOwnerCountryPE;
            this.oldOwnerCountryPE = oldOwnerCountryPE;
        }

        public readonly EcsPackedEntity regionPE;

        public readonly EcsPackedEntity newOwnerCountryPE;
        public readonly EcsPackedEntity oldOwnerCountryPE;
    }
}