
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Warfare.Fleet;
using SO.Warfare.Fleet.Missions;
using SO.Warfare.Fleet.Movement.Events;

namespace SO.Warfare.Fleet.Movement
{
    public class STaskForcePathfindingRequestAssign : IEcsRunSystem
    {
        //����
        readonly EcsWorldInject world = default;

        //������� ����
        readonly EcsPoolInject<CTaskForce> tFPool = default;
        
        readonly EcsPoolInject<CTFTargetMission> tFTargetMissionPool = default;

        readonly EcsPoolInject<CTaskForceMovement> taskForceMovementPool = default;

        public void Run(IEcsSystems systems)
        {
            //��������� ��� ������� ��������
            TaskForceMovementRequest();

            //��������� ����������� ������, ������� ������� ������
            TaskForceTargetMissionMovement();
        }

        readonly EcsFilterInject<Inc<RTaskForceMovement>> tFMovementRequestFilter = default;
        readonly EcsPoolInject<RTaskForceMovement> tFMovementRequestPool = default;
        void TaskForceMovementRequest()
        {
            //��� ������� ������� ����������� ����������� ������
            foreach(int requestEntity in tFMovementRequestFilter.Value)
            {
                //���� ������
                ref RTaskForceMovement requestComp = ref tFMovementRequestPool.Value.Get(requestEntity);

                //���� ������
                requestComp.tFPE.Unpack(world.Value, out int tFEntity);
                ref CTaskForce tF = ref tFPool.Value.Get(tFEntity);

                //���� ������ �� ����� ���������� ��������
                if(taskForceMovementPool.Value.Has(tFEntity) == false)
                {
                    //��������� ��������� �������� ������
                    TaskForceAssignMovement(
                        tFEntity,
                        requestComp.targetPE, requestComp.targetType);
                }

                tFMovementRequestPool.Value.Del(requestEntity);
            }
        }

        readonly EcsFilterInject<Inc<CTaskForce, CTFTargetMission>, Exc<CTaskForceMovement>> tFTargetMissionFilter = default;
        void TaskForceTargetMissionMovement()
        {
            //��� ������ ����������� ������ � ������� �������, �� ��� ���������� ��������
            foreach (int tFEntity in tFTargetMissionFilter.Value)
            {
                //���� ������ � ������� ������
                ref CTaskForce tF = ref tFPool.Value.Get(tFEntity);
                ref CTFTargetMission tFTargetMission = ref tFTargetMissionPool.Value.Get(tFEntity);

                //���� ������ ��������� � ������ ��������
                if (tFTargetMission.missionStatus == TaskForceMissionStatus.Movement)
                {
                    //���� ������ ����� ������������ ������ ���� ��������
                    if(TaskForceGetMovementTarget(
                        ref tF, tFEntity,
                        out EcsPackedEntity targetPE, out TaskForceMovementTargetType targetType))
                    {
                        //��������� ��������� �������� ������
                        TaskForceAssignMovement(
                            tFEntity,
                            targetPE, targetType);
                    }
                    //�����
                    else
                    {
                        UnityEngine.Debug.LogWarning("������ ��������� ������ ��� ������������!");
                    }
                }
            }
        }

        bool TaskForceGetMovementTarget(
            ref CTaskForce tF, int tFEntity,
            out EcsPackedEntity targetPE, out TaskForceMovementTargetType targetType)
        {
            //���������� �� ��������� �����
            targetPE = new();
            targetType = TaskForceMovementTargetType.None;

            return false;
        }

        void TaskForceAssignMovement(
            int tFEntity,
            EcsPackedEntity movementTargetPE, TaskForceMovementTargetType movementTargetType)
        {
            //���� ������
            ref CTaskForce tF = ref tFPool.Value.Get(tFEntity);

            //��������� ������ ��������� ��������
            ref CTaskForceMovement tFMovement = ref taskForceMovementPool.Value.Add(tFEntity);

            //��������� ������ ����������
            tFMovement = new(movementTargetPE, movementTargetType);

            //���� ���� �������� - ��� ���������
            if(movementTargetType == TaskForceMovementTargetType.Province)
            {
                //����������� ����� ���� �� ������� ���������
                TaskForceFindPathSelfRequest(tFEntity, movementTargetPE);
            }
            //�����, ���� ���� �������� - ����������� ������
            else if(movementTargetType == TaskForceMovementTargetType.TaskForce)
            {

            }
        }

        readonly EcsPoolInject<SRTaskForceFindPath> tFFindPathSelfRequestPool = default;
        void TaskForceFindPathSelfRequest(
            int tFEntity,
            EcsPackedEntity targetProvincePE)
        {
            //��������� �������� ����������� ������ ���������� ������ ����
            ref SRTaskForceFindPath selfRequestComp = ref tFFindPathSelfRequestPool.Value.Add(tFEntity);

            //��������� ������ �������
            selfRequestComp = new(targetProvincePE);
        }
    }
}