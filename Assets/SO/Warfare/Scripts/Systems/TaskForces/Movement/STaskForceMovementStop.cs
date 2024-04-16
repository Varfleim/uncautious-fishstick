
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Map.Province;
using SO.Warfare.Fleet.Missions.Events;

namespace SO.Warfare.Fleet.Movement
{
    public class STaskForceMovementStop : IEcsRunSystem
    {
        //����
        readonly EcsWorldInject world = default;


        //�����
        readonly EcsPoolInject<CProvinceCore> pCPool = default;

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

                    //���� ������ ����� ��������� ��������� � ��������
                    if (tFMovement.pathProvincePEs.Count > 0)
                    {
                        //���� ������� ��������� ������
                        tF.currentProvincePE.Unpack(world.Value, out int currentProvinceEntity);
                        ref CProvinceCore currentPC = ref pCPool.Value.Get(currentProvinceEntity);

                        //������� ������ �� ������ ����� � ���������
                        currentPC.taskForcePEs.Remove(tF.selfPE);

                        //���� ��������� ��������� � �������� ������
                        tFMovement.pathProvincePEs[tFMovement.pathProvincePEs.Count - 1].Unpack(world.Value, out int nextProvinceEntity);
                        ref CProvinceCore nextPC = ref pCPool.Value.Get(nextProvinceEntity);

                        //������� ������ � ������ ����� � ���������
                        nextPC.taskForcePEs.Add(tF.selfPE);

                        //������ ������� ��������� ������
                        tF.currentProvincePE = nextPC.selfPE;

                        //������� ��������� (��� �������) ��������� �� ��������
                        tFMovement.pathProvincePEs.RemoveAt(tFMovement.pathProvincePEs.Count - 1);

                        //��������� ���������� ��������� ������
                        tF.previousProvincePE = currentPC.selfPE;

                        //������ �������, ���������� � ����� ��������� ����������� �������
                        TaskForceChangeProvinceEvent(ref tF);
                    }

                    //���� � �������� ������ ������ �� �������� ���������
                    if (tFMovement.pathProvincePEs.Count == 0)
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

        readonly EcsPoolInject<ETaskForceChangeProvince> tFChangeProvinceEventPool = default;
        void TaskForceChangeProvinceEvent(
            ref CTaskForce tF)
        {
            //������ ����� �������� � ��������� �� ������� ����� ��������� ����������� �������
            int eventEntity = world.Value.NewEntity();
            ref ETaskForceChangeProvince eventComp = ref tFChangeProvinceEventPool.Value.Add(eventEntity);

            //��������� ������ �������
            eventComp = new(tF.selfPE);
        }
    }
}