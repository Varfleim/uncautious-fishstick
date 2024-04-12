
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Country.Events;
using SO.Map.Generation;

namespace SO.Country
{
    public class SCountryControl : IEcsRunSystem
    {
        //Миры
        readonly EcsWorldInject world = default;


        //Страны
        readonly EcsPoolInject<CCountry> countryPool = default;


        //События стран
        readonly EcsFilterInject<Inc<RCountryCreating>> countryCreatingRequestFilter = default;
        readonly EcsPoolInject<RCountryCreating> countryCreatingRequestPool = default;


        //Данные
        readonly EcsCustomInject<UI.InputData> inputData = default;
        readonly EcsCustomInject<RuntimeData> runtimeData = default;

        public void Run(IEcsSystems systems)
        {
            //Для каждого запроса создания страны
            foreach (int requestEntity in countryCreatingRequestFilter.Value)
            {
                //Берём запрос
                ref RCountryCreating requestComp = ref countryCreatingRequestPool.Value.Get(requestEntity);

                //Создаём новую страну
                CountryCreating(
                    ref requestComp);

                countryCreatingRequestPool.Value.Del(requestEntity);
            }
        }

        void CountryCreating(
            ref RCountryCreating requestComp)
        {
            //Создаём новую сущность и назначаем ей компонент страны
            int countryEntity = world.Value.NewEntity();
            ref CCountry country = ref countryPool.Value.Add(countryEntity);

            //Заполняем основные данные страны
            country = new(
                world.Value.PackEntity(countryEntity), runtimeData.Value.countriesCount++,
                requestComp.countryName);

            //ТЕСТ
            inputData.Value.playerCountryPE = country.selfPE;

            //Запрашиваем инициализацию стартового региона страны
            CountryStartRegionInitializerRequest(country.selfPE);
            //ТЕСТ

            //Создаём событие, сообщающее о создании новой страны
            ObjectNewCreatedEvent(country.selfPE, ObjectNewCreatedType.Country);
        }

        readonly EcsPoolInject<RRegionInitializer> regionInitializerRequestPool = default;
        readonly EcsPoolInject<RRegionInitializerOwner> regionInitializerOwnerRequestPool = default;
        void CountryStartRegionInitializerRequest(
            EcsPackedEntity countryPE)
        {
            //Создаём новую сущность и назначаем ей запрос применения инициализатора
            int requestEntity = world.Value.NewEntity();
            ref RRegionInitializer requestCoreComp = ref regionInitializerRequestPool.Value.Add(requestEntity);

            //Заполняем данные запроса
            requestCoreComp = new();
            
            //Назначаем компонент владельца
            ref RRegionInitializerOwner requestOwnerComp = ref regionInitializerOwnerRequestPool.Value.Add(requestEntity);

            //Заполняем данные запроса
            requestOwnerComp = new(countryPE);
        }

        readonly EcsPoolInject<EObjectNewCreated> objectNewCreatedEventPool = default;
        void ObjectNewCreatedEvent(
            EcsPackedEntity objectPE, ObjectNewCreatedType objectType)
        {
            //Создаём новую сущность и назначаем ей событие создания нового объекта
            int eventEntity = world.Value.NewEntity();
            ref EObjectNewCreated eventComp = ref objectNewCreatedEventPool.Value.Add(eventEntity);

            //Заполняем данные события
            eventComp = new(objectPE, objectType);
        }
    }
}