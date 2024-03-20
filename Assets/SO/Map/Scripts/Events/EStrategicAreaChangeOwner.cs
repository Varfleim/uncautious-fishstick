
using Leopotam.EcsLite;

namespace SO.Map.Events
{
    public readonly struct EStrategicAreaChangeOwner
    {
        public EStrategicAreaChangeOwner(
            EcsPackedEntity sAPE,
            EcsPackedEntity newOwnerCharacterPE, EcsPackedEntity oldOwnerCharacterPE)
        {
            this.sAPE = sAPE;

            this.newOwnerCharacterPE = newOwnerCharacterPE;
            this.oldOwnerCharacterPE = oldOwnerCharacterPE;
        }

        public readonly EcsPackedEntity sAPE;

        public readonly EcsPackedEntity newOwnerCharacterPE;
        public readonly EcsPackedEntity oldOwnerCharacterPE;
    }
}