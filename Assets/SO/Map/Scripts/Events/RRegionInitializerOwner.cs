
using Leopotam.EcsLite;

namespace SO.Map.Events
{
    public readonly struct RRegionInitializerOwner
    {
        public RRegionInitializerOwner(
            EcsPackedEntity ownerFactionPE)
        {
            this.ownerFactionPE = ownerFactionPE;
        }

        public readonly EcsPackedEntity ownerFactionPE;
    }
}