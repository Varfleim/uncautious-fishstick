using System.Collections.Generic;

using Leopotam.EcsLite;

namespace SO.Character
{
    public struct CCharacter
    {
        public CCharacter(
            EcsPackedEntity selfPE, int selfIndex, 
            string selfName)
        {
            this.selfPE = selfPE;
            this.selfIndex = selfIndex;
            
            this.selfName = selfName;

            ownedRCPEs = new();

            ownedSAPEs = new();

            ownedTaskForces = new();
        }

        public readonly EcsPackedEntity selfPE;
        public readonly int selfIndex;
        
        public string selfName;

        public List<EcsPackedEntity> ownedRCPEs;

        public List<EcsPackedEntity> ownedSAPEs;

        public List<EcsPackedEntity> ownedTaskForces;
    }
}