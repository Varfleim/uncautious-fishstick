
namespace SO.Warfare.Fleet.Missions
{
    public struct CTFTargetMission
    {
        public CTFTargetMission(
            TaskForceMissionStatus missionStatus)
        {
            this.missionStatus = missionStatus;
        }

        public TaskForceMissionStatus missionStatus;
    }
}