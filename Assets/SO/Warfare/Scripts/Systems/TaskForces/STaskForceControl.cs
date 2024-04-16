
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using SO.Country;
using SO.Warfare.Fleet.Events;
using SO.Map.Province;

namespace SO.Warfare.Fleet
{
    public class STaskForceControl : IEcsRunSystem
    {
        //Миры
        readonly EcsWorldInject world = default;


        //Карта
        readonly EcsPoolInject<CProvinceCore> pCPool = default;

        //Страны
        readonly EcsPoolInject<CCountry> countryPool = default;


        //Военное дело
        readonly EcsPoolInject<CTaskForce> tFPool = default;

        public void Run(IEcsSystems systems)
        {
            //Создаём оперативные группы
            TaskForceCreating();
        }

        readonly EcsFilterInject<Inc<RTaskForceCreating>> tFCreatingRequestFilter = default;
        readonly EcsPoolInject<RTaskForceCreating> tFCreatingRequestPool = default;
        void TaskForceCreating()
        {
            //Для каждого запроса создания оперативной группы
            foreach(int requestEntity in tFCreatingRequestFilter.Value)
            {
                //Берём запрос
                ref RTaskForceCreating requestComp = ref tFCreatingRequestPool.Value.Get(requestEntity);

                //Берём страну-владельца оперативной группы
                requestComp.ownerCountryPE.Unpack(world.Value, out int countryEntity);
                ref CCountry country = ref countryPool.Value.Get(countryEntity);

                //Создаём новую сущность и назначаем ей компонент оперативной группы
                int taskForceEntity = world.Value.NewEntity();
                ref CTaskForce tF = ref tFPool.Value.Add(taskForceEntity);

                //Заполняем основные данные группы
                tF = new(
                    world.Value.PackEntity(taskForceEntity),
                    country.selfPE);

                //ТЕСТ
                //Заносим PE группы в список групп страны
                country.ownedTaskForces.Add(tF.selfPE);

                //Берём стартовую провинцию страны
                country.ownedPCPEs[0].Unpack(world.Value, out int provinceEntity);
                ref CProvinceCore pC = ref pCPool.Value.Get(provinceEntity);

                //Размещаем оперативную группу в стартовй провинции страны
                tF.currentProvincePE = pC.selfPE;

                //Заносим группу в список групп в провинции
                pC.taskForcePEs.Add(tF.selfPE);

                UnityEngine.Debug.LogWarning(pC.Index);
                //ТЕСТ

                //Создаём событие, сообщающее о создании новой оперативной группы
                ObjectNewCreatedEvent(tF.selfPE, ObjectNewCreatedType.TaskForce);

                tFCreatingRequestPool.Value.Del(requestEntity);
            }
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