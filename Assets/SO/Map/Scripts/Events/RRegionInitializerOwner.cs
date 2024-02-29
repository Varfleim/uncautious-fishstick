
using Leopotam.EcsLite;

namespace SO.Map.Events
{
    public readonly struct RRegionInitializerOwner
    {
        public RRegionInitializerOwner(
            EcsPackedEntity ownerCharacterPE)
        {
            this.ownerCharacterPE = ownerCharacterPE;
        }

        public readonly EcsPackedEntity ownerCharacterPE;
    }
}