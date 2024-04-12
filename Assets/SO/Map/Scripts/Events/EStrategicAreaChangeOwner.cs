
using Leopotam.EcsLite;

namespace SO.Map.Events
{
    public readonly struct EStrategicAreaChangeOwner
    {
        public EStrategicAreaChangeOwner(
            EcsPackedEntity sAPE,
            EcsPackedEntity newOwnerCountryPE, EcsPackedEntity oldOwnerCountryPE)
        {
            this.sAPE = sAPE;

            this.newOwnerCountryPE = newOwnerCountryPE;
            this.oldOwnerCountryPE = oldOwnerCountryPE;
        }

        public readonly EcsPackedEntity sAPE;

        public readonly EcsPackedEntity newOwnerCountryPE;
        public readonly EcsPackedEntity oldOwnerCountryPE;
    }
}