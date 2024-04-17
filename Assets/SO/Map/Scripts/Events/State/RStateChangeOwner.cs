
using Leopotam.EcsLite;

namespace SO.Map.Events.State
{
    public enum StateChangeOwnerType : byte
    {
        Initialization,
        Test
    }

    public readonly struct RStateChangeOwner
    {
        public RStateChangeOwner(
            EcsPackedEntity newOwnerCountryPE, 
            EcsPackedEntity statePE, 
            StateChangeOwnerType requestType)
        {
            this.newOwnerCountryPE = newOwnerCountryPE;
            
            this.statePE = statePE;
            
            this.requestType = requestType;
        }

        public readonly EcsPackedEntity newOwnerCountryPE;

        public readonly EcsPackedEntity statePE;

        public readonly StateChangeOwnerType requestType;
    }
}