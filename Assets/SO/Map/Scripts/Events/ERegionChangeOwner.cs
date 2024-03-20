
using Leopotam.EcsLite;

namespace SO.Map.Events
{
    public readonly struct ERegionChangeOwner
    {
        public ERegionChangeOwner(
            EcsPackedEntity regionPE,
            EcsPackedEntity newOwnerCharacterPE, EcsPackedEntity oldOwnerCharacterPE)
        {
            this.regionPE = regionPE;

            this.newOwnerCharacterPE = newOwnerCharacterPE;
            this.oldOwnerCharacterPE = oldOwnerCharacterPE;
        }

        public readonly EcsPackedEntity regionPE;

        public readonly EcsPackedEntity newOwnerCharacterPE;
        public readonly EcsPackedEntity oldOwnerCharacterPE;
    }
}