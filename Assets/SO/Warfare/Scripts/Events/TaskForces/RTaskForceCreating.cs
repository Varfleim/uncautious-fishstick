
using Leopotam.EcsLite;

namespace SO.Warfare.Fleet.Events
{
    public readonly struct RTaskForceCreating
    {
        public RTaskForceCreating(
            EcsPackedEntity ownerFactionPE)
        {
            this.ownerFactionPE = ownerFactionPE;
        }

        public readonly EcsPackedEntity ownerFactionPE;
    }
}