
using Leopotam.EcsLite;

namespace SO.Map.RFO
{
    public struct CExplorationFRFO
    {
        public CExplorationFRFO(
            EcsPackedEntity selfPE,
            EcsPackedEntity factionPE,
            EcsPackedEntity parentRFOPE)
        {
            this.selfPE = selfPE;

            this.factionPE = factionPE;

            this.parentRFOPE = parentRFOPE;

            explorationLevel = 0;
        }

        public readonly EcsPackedEntity selfPE;

        public readonly EcsPackedEntity factionPE;

        public readonly EcsPackedEntity parentRFOPE;

        public byte explorationLevel;
    }
}