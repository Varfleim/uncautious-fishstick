
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using Leopotam.EcsLite.ExtendedSystems;

using SO.UI.Game.Events;
using SO.Map;

namespace SO
{
    public class SEventControl : IEcsRunSystem
    {
        //Миры
        readonly EcsWorldInject world = default;

        //Общие события
        readonly EcsFilterInject<Inc<RStartNewGame>> startNewGameRequestFilter = default;
        readonly EcsPoolInject<RStartNewGame> startNewGameRequestPool = default;

        public void Run(IEcsSystems systems)
        {
            //Для каждого запроса начала новой игры
            foreach(int requestEntity in startNewGameRequestFilter.Value)
            {
                //Запрашиваем выключение группы систем "NewGame"
                EcsGroupSystemStateEvent("NewGame", false);

                UnityEngine.Debug.LogWarning("Окончание запуска новой игры");

                startNewGameRequestPool.Value.Del(requestEntity);
            }

            //Проверяем события создания новых объектов
            ObjectNewCreatedEvent();

            //Проверяем события смены владельца RFO
            RFOChangeOwnerEvent();
        }

        readonly EcsFilterInject<Inc<EObjectNewCreated>> objectNewCreatedEventFilter = default;
        readonly EcsPoolInject<EObjectNewCreated> objectNewCreatedEventPool = default;
        void ObjectNewCreatedEvent()
        {
            //Для каждого события создания нового объекта
            foreach(int eventEntity in objectNewCreatedEventFilter.Value)
            {
                //Берём событие
                ref EObjectNewCreated eventComp = ref objectNewCreatedEventPool.Value.Get(eventEntity);

                //Если событие сообщает о создании новой фракции
                if(eventComp.objectType == ObjectNewCreatedType.Faction)
                {
                    UnityEngine.Debug.LogWarning("Faction created!");
                }
            }
        }

        readonly EcsFilterInject<Inc<ERCChangeOwner>> rFOChangeOwnerEventFilter = default;
        readonly EcsPoolInject<ERCChangeOwner> rFOChangeOwnerEventPool = default;
        void RFOChangeOwnerEvent()
        {
            //Для каждого события смены владельца RFO
            foreach (int eventEntity in rFOChangeOwnerEventFilter.Value)
            {
                //Берём событие
                ref ERCChangeOwner eventComp = ref rFOChangeOwnerEventPool.Value.Get(eventEntity);

                //Если регион не принадлежал никому, то его сущность невозможно будет взять
                if(eventComp.oldOwnerFactionPE.Unpack(world.Value, out int oldOwnerFactionEntity) == false)
                {
                    //Запрашиваем создание главной панели карты региона
                    GameCreatePanelRequest(
                        eventComp.regionPE,
                        GamePanelType.RegionMainMapPanel);
                }
                //Иначе
                else
                {
                    //Запрашиваем обновление панелей региона
                    GameRefreshPanelsSelfRequest(eventComp.regionPE);
                }
            }
        }

        readonly EcsPoolInject<EcsGroupSystemState> ecsGroupSystemStatePool = default;
        void EcsGroupSystemStateEvent(
            string systemGroupName,
            bool systemGroupState)
        {
            //Создаём новую сущность и назначаем ей событие смены состояния группы систем
            int eventEntity = world.Value.NewEntity();
            ref EcsGroupSystemState eventComp = ref ecsGroupSystemStatePool.Value.Add(eventEntity);

            //Указываем название группы систем и нужное состояние
            eventComp.Name = systemGroupName;
            eventComp.State = systemGroupState;
        }

        readonly EcsPoolInject<RGameCreatePanel> gameCreatePanelRefreshPool = default;
        void GameCreatePanelRequest(
            EcsPackedEntity objectPE,
            GamePanelType panelType)
        {
            //Создаём новую сущность и назначаем ей запрос создания панели в игре
            int requestEntity = world.Value.NewEntity();
            ref RGameCreatePanel requestComp = ref gameCreatePanelRefreshPool.Value.Add(requestEntity);

            //Заполняем данные запроса
            requestComp = new(
                objectPE,
                panelType);
        }

        readonly EcsPoolInject<SRGameRefreshPanels> gameRefreshPanelsSelfRequestPool = default;
        void GameRefreshPanelsSelfRequest(
            EcsPackedEntity objectPE)
        {
            //Берём сущность объекта
            objectPE.Unpack(world.Value, out int objectEntity);

            //Если у объекта нет самозапроса обновления панелей
            if(gameRefreshPanelsSelfRequestPool.Value.Has(objectEntity) == false)
            {
                //Назначаем сущности самозапрос
                ref SRGameRefreshPanels selfRequestComp = ref gameRefreshPanelsSelfRequestPool.Value.Add(objectEntity);
            }
        }
    }
}