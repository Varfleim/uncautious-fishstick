
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Map;
using SO.Faction;
using SO.Warfare.Fleet.Events;

namespace SO.Warfare.Fleet
{
    public class STaskForceControl : IEcsRunSystem
    {
        //����
        readonly EcsWorldInject world = default;


        //�����
        readonly EcsPoolInject<CRegionCore> rCPool = default;

        //�������
        readonly EcsPoolInject<CFaction> factionPool = default;


        //������� ����
        readonly EcsPoolInject<CTaskForce> tFPool = default;

        public void Run(IEcsSystems systems)
        {
            //������ ����������� ������
            TaskForceCreating();
        }

        readonly EcsFilterInject<Inc<RTaskForceCreating>> tFCreatingRequestFilter = default;
        readonly EcsPoolInject<RTaskForceCreating> tFCreatingRequestPool = default;
        void TaskForceCreating()
        {
            //��� ������� ������� �������� ����������� ������
            foreach(int requestEntity in tFCreatingRequestFilter.Value)
            {
                //���� ������
                ref RTaskForceCreating requestComp = ref tFCreatingRequestPool.Value.Get(requestEntity);

                //���� �������-��������� ����������� ������
                requestComp.ownerFactionPE.Unpack(world.Value, out int factionEntity);
                ref CFaction faction = ref factionPool.Value.Get(factionEntity);

                //������ ����� �������� � ��������� �� ��������� ����������� ������
                int taskForceEntity = world.Value.NewEntity();
                ref CTaskForce tF = ref tFPool.Value.Add(taskForceEntity);

                //��������� �������� ������ ������
                tF = new(
                    world.Value.PackEntity(taskForceEntity),
                    faction.selfPE);

                //����
                //������� PE ������ � ������ ����� �������
                faction.ownedTaskForces.Add(tF.selfPE);

                //���� ��������� ������ �������
                faction.ownedRCPEs[0].Unpack(world.Value, out int regionEntity);
                ref CRegionCore rC = ref rCPool.Value.Get(regionEntity);

                //��������� ����������� ������ � ��������� ������� �������
                tF.currentRegionPE = rC.selfPE;

                //������� ������ � ������ ����� � �������
                rC.taskForcePEs.Add(tF.selfPE);

                UnityEngine.Debug.LogWarning(rC.Index);
                //����

                //������ �������, ���������� � �������� ����� ����������� ������
                ObjectNewCreatedEvent(tF.selfPE, ObjectNewCreatedType.TaskForce);

                tFCreatingRequestPool.Value.Del(requestEntity);
            }
        }

        readonly EcsPoolInject<EObjectNewCreated> objectNewCreatedEventPool = default;
        void ObjectNewCreatedEvent(
            EcsPackedEntity objectPE, ObjectNewCreatedType objectType)
        {
            //������ ����� �������� � ��������� �� ������� �������� ������ �������
            int eventEntity = world.Value.NewEntity();
            ref EObjectNewCreated eventComp = ref objectNewCreatedEventPool.Value.Add(eventEntity);

            //��������� ������ �������
            eventComp = new(objectPE, objectType);
        }
    }
}