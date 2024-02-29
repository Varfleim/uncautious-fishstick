
using Leopotam.EcsLite;

namespace SO.Map
{
    public struct CExplorationRegionFractionObject
    {
        public CExplorationRegionFractionObject(
            EcsPackedEntity selfPE,
            EcsPackedEntity characterPE,
            EcsPackedEntity parentRFOPE)
        {
            this.selfPE = selfPE;

            this.characterPE = characterPE;

            this.parentRFOPE = parentRFOPE;

            explorationLevel = 0;
        }

        public readonly EcsPackedEntity selfPE;

        public readonly EcsPackedEntity characterPE;

        public readonly EcsPackedEntity parentRFOPE;

        public byte explorationLevel;
    }
}