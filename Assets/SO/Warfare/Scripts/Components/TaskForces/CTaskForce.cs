
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

            currentRegionPE = new();
            previousRegionPE = new();

            currentMissionType = TaskForceMissionType.None;
        }

        public readonly EcsPackedEntity selfPE;

        public EcsPackedEntity ownerCountryPE;

        public EcsPackedEntity currentRegionPE;
        public EcsPackedEntity previousRegionPE;

        public TaskForceMissionType currentMissionType;
    }
}