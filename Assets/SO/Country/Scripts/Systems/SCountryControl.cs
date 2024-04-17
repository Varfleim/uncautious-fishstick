
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Map.Generation;
using SO.Country.Events;

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
                CountryCreating(ref requestComp);

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

            //����������� ������������� ��������� ��������� ������
            //CountryStartProvinceInitializerRequest(country.selfPE);
            //����

            //������ �������, ���������� � �������� ����� ������
            ObjectNewCreatedEvent(country.selfPE, ObjectNewCreatedType.Country);
        }

        readonly EcsPoolInject<RProvinceInitializer> provinceInitializerRequestPool = default;
        readonly EcsPoolInject<RProvinceInitializerOwner> provinceInitializerOwnerRequestPool = default;
        void CountryStartProvinceInitializerRequest(
            EcsPackedEntity countryPE)
        {
            //������ ����� �������� � ��������� �� ������ ���������� ��������������
            int requestEntity = world.Value.NewEntity();
            ref RProvinceInitializer requestCoreComp = ref provinceInitializerRequestPool.Value.Add(requestEntity);

            //��������� ������ �������
            requestCoreComp = new();
            
            //��������� ��������� ���������
            ref RProvinceInitializerOwner requestOwnerComp = ref provinceInitializerOwnerRequestPool.Value.Add(requestEntity);

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