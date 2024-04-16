
using Leopotam.EcsLite;

using SO.Warfare.Fleet.Missions;

namespace SO.Warfare.Fleet.Missions
{
    public enum TaskForceMissionType : byte
    {
        None,
        Hold
    }

    public enum TaskForceMissionStatus : byte
    {
        None,
        Movement,
        Waiting
    }
}

namespace SO.Warfare.Fleet
{
    public struct CTaskForce
    {
        public CTaskForce(
            EcsPackedEntity selfPE, 
            EcsPackedEntity ownerCountryPE)
        {
            this.selfPE = selfPE;
            
            this.ownerCountryPE = ownerCountryPE;

            currentProvincePE = new();
            previousProvincePE = new();

            currentMissionType = TaskForceMissionType.None;
        }

        public readonly EcsPackedEntity selfPE;

        public EcsPackedEntity ownerCountryPE;

        public EcsPackedEntity currentProvincePE;
        public EcsPackedEntity previousProvincePE;

        public TaskForceMissionType currentMissionType;
    }
}