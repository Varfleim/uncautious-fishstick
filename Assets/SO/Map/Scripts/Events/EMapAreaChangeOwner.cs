
using Leopotam.EcsLite;

namespace SO.Map.Events
{
    public readonly struct EMapAreaChangeOwner
    {
        public EMapAreaChangeOwner(
            EcsPackedEntity mAPE,
            EcsPackedEntity newOwnerCountryPE, EcsPackedEntity oldOwnerCountryPE)
        {
            this.mAPE = mAPE;

            this.newOwnerCountryPE = newOwnerCountryPE;
            this.oldOwnerCountryPE = oldOwnerCountryPE;
        }

        public readonly EcsPackedEntity mAPE;

        public readonly EcsPackedEntity newOwnerCountryPE;
        public readonly EcsPackedEntity oldOwnerCountryPE;
    }
}