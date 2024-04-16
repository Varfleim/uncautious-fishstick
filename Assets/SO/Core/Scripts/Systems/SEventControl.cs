
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using Leopotam.EcsLite.ExtendedSystems;

using SO.UI.Game.Events;
using SO.UI.Game.Map.Events;
using SO.Warfare.Fleet.Movement;
using SO.Map.Events;

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
            ProvinceCoreChangeOwnerEvent();

            //Проверяем события смены владельца областей карты
            MapAreaChangeOwnerEvent();

            //Проверяем события смены провинции оперативной группой
            TaskForceChangeProvinceEvent();
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

                //Если событие сообщает о создании новой страны
                if (eventComp.objectType == ObjectNewCreatedType.Country)
                {
                    UnityEngine.Debug.LogWarning("Country created!");
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
                //Иначе, если событие сообщает о создании новой группы населения
                if(eventComp.objectType == ObjectNewCreatedType.Population)
                {
                    UnityEngine.Debug.LogWarning("Population created!");
                }
            }
        }

        readonly EcsFilterInject<Inc<EProvinceChangeOwner>> provinceChangeOwnerEventFilter = default;
        readonly EcsPoolInject<EProvinceChangeOwner> provinceChangeOwnerEventPool = default;
        void ProvinceCoreChangeOwnerEvent()
        {
            //Для каждого события смены владельца провинции
            foreach (int eventEntity in provinceChangeOwnerEventFilter.Value)
            {
                //Берём событие
                ref EProvinceChangeOwner eventComp = ref provinceChangeOwnerEventPool.Value.Get(eventEntity);

                //Если провинция не принадлежала никому, то сущность владельца невозможно будет взять
                if (eventComp.oldOwnerCountryPE.Unpack(world.Value, out int oldOwnerCountryEntity) == false)
                {
                    //Запрашиваем создание главной панели карты
                    GameCreatePanelRequest(
                        eventComp.provincePE,
                        GamePanelType.ProvinceMainMapPanel);
                }
                //Иначе
                else
                {
                    //Запрашиваем обновление панелей
                    GameRefreshPanelsSelfRequest(eventComp.provincePE);
                }
            }
        }

        readonly EcsFilterInject<Inc<EMapAreaChangeOwner>> mAChangeOwnerEventFilter = default;
        readonly EcsPoolInject<EMapAreaChangeOwner> mAChangeOwnerEventPool = default;
        void MapAreaChangeOwnerEvent()
        {
            //Для каждого события смены владельца области карты
            foreach (int eventEntity in mAChangeOwnerEventFilter.Value)
            {
                //Берём событие 
                ref EMapAreaChangeOwner eventComp = ref mAChangeOwnerEventPool.Value.Get(eventEntity);

                //Если область не принадлежала никому, то сущность страны невозможно будет взять
                if(eventComp.oldOwnerCountryPE.Unpack(world.Value, out int oldOwnerCountryEntity) == false)
                {
                    //Запрашиваем создание главной панели интерфейса области
                }
                //Иначе
                else
                {
                    //Запрашиваем обновление панелей области
                }
            }
        }

        readonly EcsFilterInject<Inc<ETaskForceChangeProvince>> tFChangeProvinceEventFilter = default;
        readonly EcsPoolInject<ETaskForceChangeProvince> tFChangeProvinceEventPool = default;
        void TaskForceChangeProvinceEvent()
        {
            //Для каждого события смены провинции оперативной группой
            foreach (int eventEntity in tFChangeProvinceEventFilter.Value)
            {
                //Берём событие 
                ref ETaskForceChangeProvince eventComp = ref tFChangeProvinceEventPool.Value.Get(eventEntity);

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