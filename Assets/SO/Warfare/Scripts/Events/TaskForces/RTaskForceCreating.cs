
using Leopotam.EcsLite;

namespace SO.Warfare.Fleet.Events
{
    public readonly struct RTaskForceCreating
    {
        public RTaskForceCreating(
            EcsPackedEntity ownerCharacterPE)
        {
            this.ownerCharacterPE = ownerCharacterPE;
        }

        public readonly EcsPackedEntity ownerCharacterPE;
    }
}