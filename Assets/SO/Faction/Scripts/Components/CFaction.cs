using System.Collections.Generic;

using Leopotam.EcsLite;

namespace SO.Faction
{
    public struct CFaction
    {
        public CFaction(
            EcsPackedEntity selfPE, int selfIndex, 
            string selfName)
        {
            this.selfPE = selfPE;
            this.selfIndex = selfIndex;
            
            this.selfName = selfName;

            ownedRFOPEs = new();

            observerPEs = new();
        }

        public readonly EcsPackedEntity selfPE;
        public readonly int selfIndex;
        
        public string selfName;

        public List<EcsPackedEntity> ownedRFOPEs;

        public List<EcsPackedEntity> observerPEs;
    }
}