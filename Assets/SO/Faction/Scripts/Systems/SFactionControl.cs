
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Map.Events;
using SO.Faction.Events;

namespace SO.Faction
{
    public class SFactionControl : IEcsRunSystem
    {
        //Миры
        readonly EcsWorldInject world = default;

        //Фракции
        readonly EcsPoolInject<CFaction> factionPool = default;


        //События фракций
        readonly EcsFilterInject<Inc<RFactionCreating>> factionCreatingRequestFilter = default;
        readonly EcsPoolInject<RFactionCreating> factionCreatingRequestPool = default;


        //Данные
        readonly EcsCustomInject<UI.InputData> inputData = default;
        readonly EcsCustomInject<RuntimeData> runtimeData = default;

        public void Run(IEcsSystems systems)
        {
            //Для каждого запроса создания фракции
            foreach (int requestEntity in factionCreatingRequestFilter.Value)
            {
                //Берём запрос
                ref RFactionCreating requestComp = ref factionCreatingRequestPool.Value.Get(requestEntity);

                //Создаём новую фракцию
                FactionCreating(
                    ref requestComp);

                factionCreatingRequestPool.Value.Del(requestEntity);
            }
        }

        void FactionCreating(
            ref RFactionCreating requestComp)
        {
            //Создаём новую сущность и назначаем ей компонент фракции
            int factionEntity = world.Value.NewEntity();
            ref CFaction faction = ref factionPool.Value.Add(factionEntity);

            //Заполняем основные данные фракции
            faction = new(
                world.Value.PackEntity(factionEntity), runtimeData.Value.factionsCount++,
                requestComp.factionName);

            //ТЕСТ
            inputData.Value.playerFactionPE = faction.selfPE;
            //ТЕСТ

            //Запрашиваем создание ExFRFO данной фракции
            FactionExFRFOsCreatingSelfRequest(factionEntity);

            //Запрашиваем инициализацию стартового региона фракции
            FactionStartRegionInitializerRequest(faction.selfPE);

            //Создаём событие, сообщающее о создании новой фракции
            ObjectNewCreatedEvent(faction.selfPE, ObjectNewCreatedType.Faction);
        }

        readonly EcsPoolInject<RRegionInitializer> regionInitializerRequestPool = default;
        readonly EcsPoolInject<RRegionInitializerOwner> regionInitializerOwnerRequestPool = default;
        void FactionStartRegionInitializerRequest(
            EcsPackedEntity factionPE)
        {
            //Создаём новую сущность и назначаем ей запрос применения инициализатора
            int requestEntity = world.Value.NewEntity();
            ref RRegionInitializer requestCoreComp = ref regionInitializerRequestPool.Value.Add(requestEntity);

            //Заполняем данные запроса
            requestCoreComp = new();
            
            //Назначаем компонент владельца
            ref RRegionInitializerOwner requestOwnerComp = ref regionInitializerOwnerRequestPool.Value.Add(requestEntity);

            //Заполняем данные запроса
            requestOwnerComp = new(factionPE);
        }

        readonly EcsPoolInject<SRExRFOsCreating> exFRFOsCreatingSelfRequestPool = default;
        void FactionExFRFOsCreatingSelfRequest(
            int factionEntity)
        {
            //Назначаем фракции самозапрос создания ExFRFO
            ref SRExRFOsCreating selfRequestComp = ref exFRFOsCreatingSelfRequestPool.Value.Add(factionEntity);

            //Заполняем данные запроса
            selfRequestComp = new();
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