
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Map;
using SO.Warfare.Fleet.Missions.Events;
using SO.Warfare.Fleet.Movement;

namespace SO.Warfare.Fleet.Missions
{
    public class STaskForceMissionsControl : IEcsRunSystem
    {
        //����
        readonly EcsWorldInject world = default;


        //�����
        readonly EcsPoolInject<CRegionCore> rCPool = default;

        //������� ����
        readonly EcsPoolInject<CTaskForce> tFPool = default;
        readonly EcsPoolInject<CTFTargetMission> tFTargetMissionPool = default;

        public void Run(IEcsSystems systems)
        {
            //������ ������ ����������� �����
            TaskForceChangeMission();
        }

        readonly EcsFilterInject<Inc<RTaskForceChangeMission>> tFChangeMissionRequestFilter = default;
        readonly EcsPoolInject<RTaskForceChangeMission> tFChangeMissionRequestPool = default;
        void TaskForceChangeMission()
        {
            //��� ������� ������� ����� ������ ����������� ������
            foreach (int requestEntity in tFChangeMissionRequestFilter.Value)
            {
                //���� ������
                ref RTaskForceChangeMission requestComp = ref tFChangeMissionRequestPool.Value.Get(requestEntity);

                //���� ������
                requestComp.tFPE.Unpack(world.Value, out int tFEntity);
                ref CTaskForce tF = ref tFPool.Value.Get(tFEntity);

                //������� ������� ������
                TaskForceClearMission(ref tF);

                //���� ������������� ����� ������ �� ���������
                if(requestComp.requestType == TaskForceChangeMissionType.MissionHold)
                {
                    //������ ������ ������ �� ���������
                    TaskForceChangeMissionHold(ref tF);
                }

                tFChangeMissionRequestPool.Value.Del(tFEntity);
            }
        }

        void TaskForceClearMission(
            ref CTaskForce tF)
        {
            //���� �������� ������
            tF.selfPE.Unpack(world.Value, out int tFEntity);

            //���� � ������ ���� ��������� ������� ������
            if (tFTargetMissionPool.Value.Has(tFEntity)) 
            {
                //������� ���
                tFTargetMissionPool.Value.Del(tFEntity);
            }

            //������� ������� ������ ������
            tF.currentMissionType = TaskForceMissionType.None;
        }

        void TaskForceChangeMissionHold(
            ref CTaskForce tF)
        {
            //��������� ��� ������� ������ ������
            tF.currentMissionType = TaskForceMissionType.Hold;
        }
    }
}