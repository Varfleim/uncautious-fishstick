
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using SO.Country;
using SO.Warfare.Fleet.Events;
using SO.Map.Province;

namespace SO.Warfare.Fleet
{
    public class STaskForceControl : IEcsRunSystem
    {
        //����
        readonly EcsWorldInject world = default;


        //�����
        readonly EcsPoolInject<CProvinceCore> pCPool = default;

        //������
        readonly EcsPoolInject<CCountry> countryPool = default;


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

                //���� ������-��������� ����������� ������
                requestComp.ownerCountryPE.Unpack(world.Value, out int countryEntity);
                ref CCountry country = ref countryPool.Value.Get(countryEntity);

                //������ ����� �������� � ��������� �� ��������� ����������� ������
                int taskForceEntity = world.Value.NewEntity();
                ref CTaskForce tF = ref tFPool.Value.Add(taskForceEntity);

                //��������� �������� ������ ������
                tF = new(
                    world.Value.PackEntity(taskForceEntity),
                    country.selfPE);

                //����
                //������� PE ������ � ������ ����� ������
                country.ownedTaskForces.Add(tF.selfPE);

                //���� ��������� ��������� ������
                country.ownedPCPEs[0].Unpack(world.Value, out int provinceEntity);
                ref CProvinceCore pC = ref pCPool.Value.Get(provinceEntity);

                //��������� ����������� ������ � �������� ��������� ������
                tF.currentProvincePE = pC.selfPE;

                //������� ������ � ������ ����� � ���������
                pC.taskForcePEs.Add(tF.selfPE);

                UnityEngine.Debug.LogWarning(pC.Index);
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