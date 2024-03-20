
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Map.Region;
using SO.Warfare.Fleet.Missions.Events;

namespace SO.Warfare.Fleet.Missions
{
    public class STaskForceTargetCheck : IEcsRunSystem
    {
        //����
        //readonly EcsWorldInject world = default;


        //������� ����
        readonly EcsPoolInject<CTaskForce> tFPool = default;

        readonly EcsPoolInject<CTFTargetMission> tFTargetMissionPool = default;


        //������� �������� ����
        readonly EcsPoolInject<SRTaskForceTargetCheck> tFTargetCheckSelfRequestPool = default;

        public void Run(IEcsSystems systems)
        {
            //��������� ����������� ������ � �������� ��������
            TaskForceTargetMissionTargetCheck();
        }

        readonly EcsFilterInject<Inc<CTaskForce, CTFTargetMission, SRTaskForceTargetCheck>> tFTargetMissionTargetCheckSelfRequestFilter = default;
        void TaskForceTargetMissionTargetCheck()
        {
            //��� ������ ����������� ������ � ������� ������� � ������������ �������� ����
            foreach(int tFEntity in tFTargetMissionTargetCheckSelfRequestFilter.Value)
            {
                //���� ������, ������� ������ � ��������� ��������
                ref CTaskForce tF = ref tFPool.Value.Get(tFEntity);
                ref CTFTargetMission tFTargetMission = ref tFTargetMissionPool.Value.Get(tFEntity);

                //���� ������ ��������� � ������ ��������
                if(tFTargetMission.missionStatus == TaskForceMissionStatus.Movement)
                {

                }

                tFTargetCheckSelfRequestPool.Value.Del(tFEntity);
            }
        }
    }
}