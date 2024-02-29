
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using Leopotam.EcsLite.ExtendedSystems;

using SO.UI.Game.Events;
using SO.UI.Game.Map.Events;
using SO.Map;
using SO.Warfare.Fleet.Movement;

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
            RegionCoreChangeOwnerEvent();

            //Проверяем события смены региона оперативной группой
            TaskForceChangeRegionEvent();
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

                //Если событие сообщает о создании нового персонажа
                if (eventComp.objectType == ObjectNewCreatedType.Character)
                {
                    UnityEngine.Debug.LogWarning("Character created!");
                }
                //Иначе, если событие сообщает о создании новой оперативной группы
                else if(eventComp.objectType == ObjectNewCreatedType.TaskForce)
                {
                    UnityEngine.Debug.LogWarning("Task Force created!");

                    //Запрашиваем создание обзорной панели группы во вкладке флотов
                    GameCreatePanelRequest(
                        eventComp.objectPE,
                        GamePanelType.TaskForceSummaryPanelFMSbpnFleetsTab);

                    //Запрашиваем создание главной панели карты оперативной группы
                    GameCreatePanelRequest(
                        eventComp.objectPE,
                        GamePanelType.TaskForceMainMapPanel);
                }
            }
        }

        readonly EcsFilterInject<Inc<ERegionCoreChangeOwner>> rFOChangeOwnerEventFilter = default;
        readonly EcsPoolInject<ERegionCoreChangeOwner> rFOChangeOwnerEventPool = default;
        void RegionCoreChangeOwnerEvent()
        {
            //Для каждого события смены владельца RC
            foreach (int eventEntity in rFOChangeOwnerEventFilter.Value)
            {
                //Берём событие
                ref ERegionCoreChangeOwner eventComp = ref rFOChangeOwnerEventPool.Value.Get(eventEntity);

                //Если регион не принадлежал никому, то его сущность невозможно будет взять
                if(eventComp.oldOwnerCharacterPE.Unpack(world.Value, out int oldOwnerCharacterEntity) == false)
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

        readonly EcsFilterInject<Inc<ETaskForceChangeRegion>> tFChangeRegionEventFilter = default;
        readonly EcsPoolInject<ETaskForceChangeRegion> tFChangeRegionEventPool = default;
        void TaskForceChangeRegionEvent()
        {
            //Для каждого события смены региона оперативной группой
            foreach(int eventEntity in tFChangeRegionEventFilter.Value)
            {
                //Берём событие 
                ref ETaskForceChangeRegion eventComp = ref tFChangeRegionEventPool.Value.Get(eventEntity);

                //Запрашиваем обновление родителя панелей карты
                GameRefreshMapPanelParentSelfRequest(eventComp.tFPE);
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

        readonly EcsPoolInject<SRRefreshMapPanelsParent> refreshMapPanelsParentSelfRequestPool = default;
        void GameRefreshMapPanelParentSelfRequest(
            EcsPackedEntity objectPE)
        {
            //Берём сущность объекта
            objectPE.Unpack(world.Value, out int objectEntity);

            //Если у объекта нет самозапроса обновления родителя панелей карты
            if (refreshMapPanelsParentSelfRequestPool.Value.Has(objectEntity) == false)
            {
                //Назначаем сущности самозапрос
                ref SRRefreshMapPanelsParent selfRequestComp = ref refreshMapPanelsParentSelfRequestPool.Value.Add(objectEntity);
            }
        }
    }
}