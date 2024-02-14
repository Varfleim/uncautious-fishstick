
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Map;
using SO.Warfare.Fleet.Missions.Events;

namespace SO.Warfare.Fleet.Movement
{
    public class STaskForceMovementStop : IEcsRunSystem
    {
        //����
        readonly EcsWorldInject world = default;


        //�����
        readonly EcsPoolInject<CRegionCore> rCPool = default;

        //������� ����
        readonly EcsPoolInject<CTaskForce> tFPool = default;

        readonly EcsFilterInject<Inc<CTaskForce, CTaskForceMovement>> tFMovementFilter = default;
        readonly EcsPoolInject<CTaskForceMovement> tFMovementPool = default;


        //������� �������� ����
        readonly EcsPoolInject<SRTaskForceTargetCheck> tFTargetCheckSelfRequestPool = default;

        public void Run(IEcsSystems systems)
        {
            //��������� ����������� ������, ������� ����� ��������� ��������
            TaskForceMovementStopCheck();
        }

        void TaskForceMovementStopCheck()
        {
            //��� ������ ����������� ������ � ����������� ��������
            foreach(int tFEntity in tFMovementFilter.Value)
            {
                //���� ��������� ��������
                ref CTaskForceMovement tFMovement = ref tFMovementPool.Value.Get(tFEntity);

                //���� ������ ���������� ��������
                if(tFMovement.isTraveled == true)
                {
                    //��������, ��� ������ ����� ���������� ��������
                    tFMovement.isTraveled = false;

                    //���� ������
                    ref CTaskForce tF = ref tFPool.Value.Get(tFEntity);

                    //���� ������ ����� ��������� ������ � ��������
                    if (tFMovement.pathRegionPEs.Count > 0)
                    {
                        //���� ������� ������ ������
                        tF.currentRegionPE.Unpack(world.Value, out int currentRegionEntity);
                        ref CRegionCore currentRC = ref rCPool.Value.Get(currentRegionEntity);

                        //������� ������ �� ������ ����� � �������
                        currentRC.taskForcePEs.Remove(tF.selfPE);

                        //���� ��������� ������ � �������� ������
                        tFMovement.pathRegionPEs[tFMovement.pathRegionPEs.Count - 1].Unpack(world.Value, out int nextRegionEntity);
                        ref CRegionCore nextRC = ref rCPool.Value.Get(nextRegionEntity);

                        //������� ������ � ������ ����� � �������
                        nextRC.taskForcePEs.Add(tF.selfPE);

                        //������ ������� ������ ������
                        tF.currentRegionPE = nextRC.selfPE;

                        //������� ��������� (��� �������) ������ �� ��������
                        tFMovement.pathRegionPEs.RemoveAt(tFMovement.pathRegionPEs.Count - 1);

                        //��������� ���������� ������ ������
                        tF.previousRegionPE = currentRC.selfPE;

                        //������ �������, ���������� � ����� ������� ����������� �������
                        TaskForceChangeRegionEvent(ref tF);
                    }

                    //���� � �������� ������ ������ �� �������� ��������
                    if(tFMovement.pathRegionPEs.Count == 0)
                    {
                        //����������� �������� ����
                        //TaskForceTargetCheckSelfRequest(tFEntity);

                        //������� ��������� ��������
                        tFMovementPool.Value.Del(tFEntity);

                        //���� ������ ����� ��������� �������� � ���������� ����, ������� � ���

                    }
                }
            }
        }

        void TaskForceTargetCheckSelfRequest(
            int tFEntity)
        {
            //��������� ����������� ������ ���������� �������� ����
            ref SRTaskForceTargetCheck selfRequestComp = ref tFTargetCheckSelfRequestPool.Value.Add(tFEntity);

            //��������� ������ �����������
            selfRequestComp = new(0);
        }

        readonly EcsPoolInject<ETaskForceChangeRegion> tFChangeOwnerEventPool = default;
        void TaskForceChangeRegionEvent(
            ref CTaskForce tF)
        {
            //������ ����� �������� � ��������� �� ������� ����� ������� ����������� �������
            int eventEntity = world.Value.NewEntity();
            ref ETaskForceChangeRegion eventComp = ref tFChangeOwnerEventPool.Value.Add(eventEntity);

            //��������� ������ �������
            eventComp = new(tF.selfPE);
        }
    }
}