
using Leopotam.EcsLite;

namespace SO.Economy.RFO
{
    public struct CRegionFO
    {
        public CRegionFO(
            EcsPackedEntity selfPE)
        {
            this.selfPE = selfPE;

            ownerFactionPE = new();
        }

        public readonly EcsPackedEntity selfPE;

        public EcsPackedEntity ownerFactionPE;
    }
}