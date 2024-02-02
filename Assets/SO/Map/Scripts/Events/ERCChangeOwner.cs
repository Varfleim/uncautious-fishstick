
using Leopotam.EcsLite;

namespace SO.Map
{
    public readonly struct ERCChangeOwner
    {
        public ERCChangeOwner(
            EcsPackedEntity regionPE,
            EcsPackedEntity newOwnerFactionPE, EcsPackedEntity oldOwnerFactionPE)
        {
            this.regionPE = regionPE;

            this.newOwnerFactionPE = newOwnerFactionPE;
            this.oldOwnerFactionPE = oldOwnerFactionPE;
        }

        public readonly EcsPackedEntity regionPE;

        public readonly EcsPackedEntity newOwnerFactionPE;
        public readonly EcsPackedEntity oldOwnerFactionPE;
    }
}