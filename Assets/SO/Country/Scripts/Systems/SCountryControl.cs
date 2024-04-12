
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Country.Events;
using SO.Map.Generation;

namespace SO.Country
{
    public class SCountryControl : IEcsRunSystem
    {
        //����
        readonly EcsWorldInject world = default;


        //������
        readonly EcsPoolInject<CCountry> countryPool = default;


        //������� �����
        readonly EcsFilterInject<Inc<RCountryCreating>> countryCreatingRequestFilter = default;
        readonly EcsPoolInject<RCountryCreating> countryCreatingRequestPool = default;


        //������
        readonly EcsCustomInject<UI.InputData> inputData = default;
        readonly EcsCustomInject<RuntimeData> runtimeData = default;

        public void Run(IEcsSystems systems)
        {
            //��� ������� ������� �������� ������
            foreach (int requestEntity in countryCreatingRequestFilter.Value)
            {
                //���� ������
                ref RCountryCreating requestComp = ref countryCreatingRequestPool.Value.Get(requestEntity);

                //������ ����� ������
                CountryCreating(
                    ref requestComp);

                countryCreatingRequestPool.Value.Del(requestEntity);
            }
        }

        void CountryCreating(
            ref RCountryCreating requestComp)
        {
            //������ ����� �������� � ��������� �� ��������� ������
            int countryEntity = world.Value.NewEntity();
            ref CCountry country = ref countryPool.Value.Add(countryEntity);

            //��������� �������� ������ ������
            country = new(
                world.Value.PackEntity(countryEntity), runtimeData.Value.countriesCount++,
                requestComp.countryName);

            //����
            inputData.Value.playerCountryPE = country.selfPE;

            //����������� ������������� ���������� ������� ������
            CountryStartRegionInitializerRequest(country.selfPE);
            //����

            //������ �������, ���������� � �������� ����� ������
            ObjectNewCreatedEvent(country.selfPE, ObjectNewCreatedType.Country);
        }

        readonly EcsPoolInject<RRegionInitializer> regionInitializerRequestPool = default;
        readonly EcsPoolInject<RRegionInitializerOwner> regionInitializerOwnerRequestPool = default;
        void CountryStartRegionInitializerRequest(
            EcsPackedEntity countryPE)
        {
            //������ ����� �������� � ��������� �� ������ ���������� ��������������
            int requestEntity = world.Value.NewEntity();
            ref RRegionInitializer requestCoreComp = ref regionInitializerRequestPool.Value.Add(requestEntity);

            //��������� ������ �������
            requestCoreComp = new();
            
            //��������� ��������� ���������
            ref RRegionInitializerOwner requestOwnerComp = ref regionInitializerOwnerRequestPool.Value.Add(requestEntity);

            //��������� ������ �������
            requestOwnerComp = new(countryPE);
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