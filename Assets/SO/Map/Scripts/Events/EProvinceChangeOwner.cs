
using Leopotam.EcsLite;

namespace SO.Map.Events
{
    public readonly struct EProvinceChangeOwner
    {
        public EProvinceChangeOwner(
            EcsPackedEntity provincePE,
            EcsPackedEntity newOwnerCountryPE, EcsPackedEntity oldOwnerCountryPE)
        {
            this.provincePE = provincePE;

            this.newOwnerCountryPE = newOwnerCountryPE;
            this.oldOwnerCountryPE = oldOwnerCountryPE;
        }

        public readonly EcsPackedEntity provincePE;

        public readonly EcsPackedEntity newOwnerCountryPE;
        public readonly EcsPackedEntity oldOwnerCountryPE;
    }
}