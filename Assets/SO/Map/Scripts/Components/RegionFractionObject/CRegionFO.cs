
using Leopotam.EcsLite;

namespace SO.Map.RFO
{
    public struct CRegionFO
    {
        public CRegionFO(
            EcsPackedEntity selfPE)
        {
            this.selfPE = selfPE;

            ownerFactionPE = new();

            factionRFOs = new DFactionRFO[0];
        }

        public readonly EcsPackedEntity selfPE;

        public EcsPackedEntity ownerFactionPE;

        public DFactionRFO[] factionRFOs;
    }
}