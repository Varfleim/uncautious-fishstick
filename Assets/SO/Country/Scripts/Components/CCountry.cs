using System.Collections.Generic;

using Leopotam.EcsLite;

namespace SO.Country
{
    public struct CCountry
    {
        public CCountry(
            EcsPackedEntity selfPE, int selfIndex, 
            string selfName)
        {
            this.selfPE = selfPE;
            this.selfIndex = selfIndex;
            
            this.selfName = selfName;

            ownedStatePEs = new();

            ownedTaskForces = new();
        }

        public readonly EcsPackedEntity selfPE;
        public readonly int selfIndex;
        
        public string selfName;

        public List<EcsPackedEntity> ownedStatePEs;

        public List<EcsPackedEntity> ownedTaskForces;
    }
}