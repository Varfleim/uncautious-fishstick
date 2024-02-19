using System.Collections.Generic;

using UnityEngine;

using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using Leopotam.EcsLite.Unity.Ugui;

using SO.UI.Events;
using SO.UI.MainMenu.Events;
using SO.UI.Game.Events;
using SO.UI.Game.Map.Events;
using SO.UI.Game.GUI;
using SO.UI.Game.GUI.Object;
using SO.UI.Game.GUI.Object.Events;
using SO.Map;
using SO.Map.Hexasphere;
using SO.Map.Events;
using SO.Faction;
using SO.Warfare.Fleet;
using SO.Warfare.Fleet.Events;
using SO.Warfare.Fleet.Missions.Events;
using SO.Warfare.Fleet.Movement;
using SO.Warfare.Fleet.Movement.Events;

namespace SO.UI
{
    public class SUIInput : IEcsInitSystem, IEcsRunSystem
    {
        //Миры
        readonly EcsWorldInject world = default;
        EcsWorld uguiMapWorld;
        EcsWorld uguiUIWorld;


        //Карта
        readonly EcsFilterInject<Inc<CRegionHexasphere>> regionFilter = default;
        readonly EcsPoolInject<CRegionHexasphere> rHSPool = default;
        readonly EcsPoolInject<CRegionCore> rCPool = default;

        //Фракции
        readonly EcsPoolInject<CFaction> factionPool = default;

        //Военное дело
        readonly EcsPoolInject<CTaskForce> taskForcePool = default;
        readonly EcsPoolInject<CTaskForceDisplayedGUIPanels> taskForceDisplayedGUIPanelsPool = default;


        EcsFilter clickEventMapFilter;
        EcsPool<EcsUguiClickEvent> clickEventMapPool;

        EcsFilter clickEventUIFilter;
        EcsPool<EcsUguiClickEvent> clickEventUIPool;

        //Данные
        readonly EcsCustomInject<UIData> uIData = default;
        readonly EcsCustomInject<MapGenerationData> mapGenerationData = default;
        readonly EcsCustomInject<RegionsData> regionsData = default;
        readonly EcsCustomInject<InputData> inputData = default;
        readonly EcsCustomInject<RuntimeData> runtimeData = default;

        readonly EcsCustomInject<SOUI> sOUI = default;

        public void Init(IEcsSystems systems)
        {
            uguiMapWorld = systems.GetWorld("uguiMapEventsWorld");

            clickEventMapPool = uguiMapWorld.GetPool<EcsUguiClickEvent>();
            clickEventMapFilter = uguiMapWorld.Filter<EcsUguiClickEvent>().End();

            uguiUIWorld = systems.GetWorld("uguiUIEventsWorld");

            clickEventUIPool = uguiUIWorld.GetPool<EcsUguiClickEvent>();
            clickEventUIFilter = uguiUIWorld.Filter<EcsUguiClickEvent>().End();
        }

        public void Run(IEcsSystems systems)
        {
            //Определяем состояние клавиш
            CheckButtons();

            //Для каждого события клика по интерфейсу
            foreach (int clickEventUIEntity in clickEventUIFilter)
            {
                //Берём событие
                ref EcsUguiClickEvent clickEvent = ref clickEventUIPool.Get(clickEventUIEntity);

                Debug.LogWarning("Click! " + clickEvent.WidgetName);

                //Если активно окно игры
                if(sOUI.Value.activeMainWindowType == MainWindowType.Game)
                {
                    //Проверяем клики в окне игры
                    GameClickAction(ref clickEvent);
                }
                //Иначе, если активно окно главного меню
                else if(sOUI.Value.activeMainWindowType == MainWindowType.MainMenu)
                {
                    MainMenuClickAction(ref clickEvent);
                }

                uguiUIWorld.DelEntity(clickEventUIEntity);
            }

            //Для каждого события клика по карте
            foreach (int clickEventMapEntity in clickEventMapFilter)
            {
                //Берём событие
                ref EcsUguiClickEvent clickEvent = ref clickEventMapPool.Get(clickEventMapEntity);

                Debug.LogWarning("Click! " + clickEvent.WidgetName);

                uguiMapWorld.DelEntity(clickEventMapEntity);
            }

            //Если активен какой-либо режим карты
            if (inputData.Value.mapMode != MapMode.None)
            {
                //Ввод поворота камеры
                CameraInputRotating();

                //Ввод приближения камеры
                CameraInputZoom();
            }

            //Прочий ввод
            InputOther();

            //Ввод мыши
            InputMouse();

            foreach (int regionEntity in regionFilter.Value)
            {
                ref CRegionHexasphere rHS = ref rHSPool.Value.Get(regionEntity);
                ref CRegionCore rC = ref rCPool.Value.Get(regionEntity);

                //if (region.Elevation < mapGenerationData.Value.waterLevel)
                //{
                //    RegionSetColor(ref region, Color.blue);
                //}
                //else if (region.TerrainTypeIndex == 0)
                //{
                //    RegionSetColor(ref region, Color.yellow);
                //}
                //else if (region.TerrainTypeIndex == 1)
                //{
                //    RegionSetColor(ref region, Color.green);
                //}
                //else if (region.TerrainTypeIndex == 2)
                //{
                //    RegionSetColor(ref region, Color.gray);
                //}
                //else if (region.TerrainTypeIndex == 3)
                //{
                //    RegionSetColor(ref region, Color.red);
                //}
                //else if (region.TerrainTypeIndex == 4)
                //{
                //    RegionSetColor(ref region, Color.white);
                //}

                //for (int a = 0; a < rFO.factionRFOs.Length; a++)
                //{
                //    rFO.factionRFOs[a].fRFOPE.Unpack(world.Value, out int fRFOEntity);
                //    ref CExplorationFRFO exFRFO = ref exFRFOPool.Value.Get(fRFOEntity);

                //    if (exFRFO.factionPE.EqualsTo(inputData.Value.playerFactionPE))
                //    {
                //        RegionSetColor(
                //            ref region,
                //            new Color32(
                //                exFRFO.explorationLevel, exFRFO.explorationLevel, exFRFO.explorationLevel,
                //                255));
                //    }
                //}

                //if (rFO.ownerFactionPE.Unpack(world.Value, out int factionEntity))
                //{
                //    RegionSetColor(ref region, Color.magenta);

                //    List<int> regions = regionsData.Value.GetRegionIndicesWithinSteps(
                //        world.Value,
                //        regionFilter.Value, regionPool.Value,
                //        ref region, 10);

                //    for(int a = 0; a < regions.Count; a++)
                //    {
                //        regionsData.Value.regionPEs[regions[a]].Unpack(world.Value, out int neighbourRegionEntity);
                //        ref CRegion neighbourRegion = ref regionPool.Value.Get(neighbourRegionEntity);

                //        RegionSetColor(ref neighbourRegion, Color.magenta);
                //    }
                //}
            }
        }

        void CheckButtons()
        {
            //Определяем состояние ЛКМ
            inputData.Value.leftMouseButtonClick = Input.GetMouseButtonDown(0);
            inputData.Value.leftMouseButtonPressed = inputData.Value.leftMouseButtonClick || Input.GetMouseButton(0);
            inputData.Value.leftMouseButtonRelease = Input.GetMouseButtonUp(0);

            //Определяем состояние ПКМ
            inputData.Value.rightMouseButtonClick = Input.GetMouseButtonDown(1);
            inputData.Value.rightMouseButtonPressed = inputData.Value.leftMouseButtonClick || Input.GetMouseButton(1);
            inputData.Value.rightMouseButtonRelease = Input.GetMouseButtonUp(1);

            //Определяем состояние LeftShift
            inputData.Value.leftShiftKeyPressed = Input.GetKey(KeyCode.LeftShift);
        }

        void CameraInputRotating()
        {
            //Берём Y-вращение
            float yRotationDelta = Input.GetAxis("Horizontal");
            //Если оно не равно нулю, то применяем
            if (yRotationDelta != 0f)
            {
                CameraAdjustYRotation(yRotationDelta);
            }

            //Берём X-вращение
            float xRotationDelta = Input.GetAxis("Vertical");
            //Если оно не равно нулю, то применяем
            if (xRotationDelta != 0f)
            {
                CameraAdjustXRotation(xRotationDelta);
            }
        }

        void CameraAdjustYRotation(
            float rotationDelta)
        {
            //Рассчитываем угол вращения
            inputData.Value.rotationAngleY -= rotationDelta * inputData.Value.rotationSpeed * UnityEngine.Time.deltaTime;

            //Выравниваем угол
            if (inputData.Value.rotationAngleY < 0f)
            {
                inputData.Value.rotationAngleY += 360f;
            }
            else if (inputData.Value.rotationAngleY >= 360f)
            {
                inputData.Value.rotationAngleY -= 360f;
            }

            //Применяем вращение
            inputData.Value.mapCamera.localRotation = Quaternion.Euler(
                0f, inputData.Value.rotationAngleY, 0f);
        }

        void CameraAdjustXRotation(
            float rotationDelta)
        {
            //Рассчитываем угол вращения
            inputData.Value.rotationAngleX += rotationDelta * inputData.Value.rotationSpeed * UnityEngine.Time.deltaTime;

            //Выравниваем угол
            inputData.Value.rotationAngleX = Mathf.Clamp(
                inputData.Value.rotationAngleX, inputData.Value.minAngleX, inputData.Value.maxAngleX);

            //Применяем вращение
            inputData.Value.swiwel.localRotation = Quaternion.Euler(
                inputData.Value.rotationAngleX, 0f, 0f);
        }

        void CameraInputZoom()
        {
            //Берём вращение колёсика мыши
            float zoomDelta = Input.GetAxis("Mouse ScrollWheel");

            //Если оно не равно нулю
            if (zoomDelta != 0)
            {
                //Применяем приближение
                CameraAdjustZoom(zoomDelta);
            }
        }

        void CameraAdjustZoom(
            float zoomDelta)
        {
            //Рассчитываем приближение камеры
            inputData.Value.zoom = Mathf.Clamp01(inputData.Value.zoom + zoomDelta);

            //Рассчитываем расстояние приближения и применяем его
            float zoomDistance = Mathf.Lerp(
                inputData.Value.stickMinZoom, inputData.Value.stickMaxZoom, inputData.Value.zoom);
            inputData.Value.stick.localPosition = new(0f, 0f, zoomDistance);
        }

        void InputOther()
        {
            //Пауза тиков
            if(Input.GetKeyUp(KeyCode.Space))
            {
                //Если игра активна
                if (runtimeData.Value.isGameActive == true)
                {
                    //Запрашиваем включение паузы
                    GameActionRequest(GameActionType.PauseOn);
                }
                //Иначе
                else
                {
                    //Запрашиваем выключение паузы
                    GameActionRequest(GameActionType.PauseOff);
                }
            }

            //Закрытие игры
            if(Input.GetKeyUp(KeyCode.Escape))
            {
                //Запрашиваем выход из игры
                GeneralActionRequest(GeneralActionType.QuitGame);
            }

            if(Input.GetKeyUp(KeyCode.Y))
            {
                Debug.LogWarning("Y!");

                MapChangeMapModeRequest(
                    ChangeMapModeRequestType.Exploration);
            }
        }

        void InputMouse()
        {
            //Если курсор не находится над объектом интерфейса
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject() == false)
            {
                //Проверяем взаимодействие мыши со сферой
                HexasphereCheckMouseInteraction();
            }
            //Иначе
            else
            {
                //Курсор точно не находится над гексасферой
                inputData.Value.isMouseOver = false;
            }
        }

        #region MainMenu
        void MainMenuClickAction(
            ref EcsUguiClickEvent clickEvent)
        {
            //Если запрашивается открытие окна игры
            if(clickEvent.WidgetName == "MainMenuNewGame")
            {
                //Создаём запрос открытия окна игры
                MainMenuActionRequest(MainMenuActionType.OpenGame);
            }
            //Иначе, если запрашивается выход из игры
            else if(clickEvent.WidgetName == "QuitGame")
            {
                //Создаём запрос выхода из игры
                GeneralActionRequest(GeneralActionType.QuitGame);
            }
        }

        readonly EcsPoolInject<RMainMenuAction> mainMenuActionRequestPool = default;
        void MainMenuActionRequest(
            MainMenuActionType actionType)
        {
            //Создаём новую сущность и назначаем ей запрос действия в главном меню
            int requestEntity = world.Value.NewEntity();
            ref RMainMenuAction requestComp = ref mainMenuActionRequestPool.Value.Add(requestEntity);

            //Заполняем данные запроса
            requestComp = new(actionType);
        }
        #endregion

        #region Game
        void GameClickAction(
            ref EcsUguiClickEvent clickEvent)
        {
            //Берём окно игры
            UIGameWindow gameWindow = sOUI.Value.gameWindow;

            //Берём фракцию игрока
            inputData.Value.playerFactionPE.Unpack(world.Value, out int factionEntity);
            ref CFaction faction = ref factionPool.Value.Get(factionEntity);

            //Если клик по флагу фракции игрока
            if (clickEvent.WidgetName == "PlayerFactionFlag")
            {
                //Запрашиваем отображение панели фракции
                ObjectPnActionRequest(
                    uIData.Value.factionSubpanelDefaultTab,
                    faction.selfPE);
            }
            //Иначе, если клик по кнопке менеджера флотов на панели главных кнопок
            else if(clickEvent.WidgetName == "FleetManagerMBtnPn")
            {
                //Запрашиваем отображение менеджера флотов
                ObjectPnActionRequest(
                    uIData.Value.fleetManagerSubpanelDefaultTab,
                    faction.selfPE);
            }

            //Иначе, если активна панель объекта
            else if (gameWindow.activeMainPanelType == MainPanelType.Object)
            {
                //Проверяем клики в панели объекта
                ObjectPnClickAction(ref clickEvent);
            }
        }

        readonly EcsPoolInject<RGameAction> gameActionRequestPool = default;
        void GameActionRequest(
            GameActionType actionType)
        {
            //Создаём новую сущность и назначаем ей запрос действия в игре
            int requestEntity = world.Value.NewEntity();
            ref RGameAction requestComp = ref gameActionRequestPool.Value.Add(requestEntity);

            //Заполняем данные запроса
            requestComp = new(actionType);
        }

        readonly EcsPoolInject<RChangeMapMode> changeMapModeRequestTypePool = default;
        void MapChangeMapModeRequest(
            ChangeMapModeRequestType requestType)
        {
            //Создаём новую сущность и назначаем ей запрос смены режима карты
            int requestEntity = world.Value.NewEntity();
            ref RChangeMapMode requestComp = ref changeMapModeRequestTypePool.Value.Add(requestEntity);

            //Заполняем данные запроса
            requestComp = new(
                requestType);
        }

        readonly EcsPoolInject<RTaskForceChangeMission> taskForceChangeMissionRequestPool = default;
        void TaskForceChangeMissionRequest(
            ref CTaskForce tF,
            TaskForceChangeMissionType requestType)
        {
            //Создаём новую сущность и назначаем ей запрос смены миссии оперативной группы
            int requestEntity = world.Value.NewEntity();
            ref RTaskForceChangeMission requestComp = ref taskForceChangeMissionRequestPool.Value.Add(requestEntity);

            //Заполняем данные запроса
            requestComp = new(
                tF.selfPE,
                requestType);
        }

        readonly EcsPoolInject<RTaskForceMovement> tFMovementRequestPool = default;
        void TaskForceMovementRequest(
            ref CTaskForce tF,
            EcsPackedEntity targetPE,
            TaskForceMovementTargetType targetType)
        {
            //Создаём новую сущность и назначаем ей запрос движения оперативной группы
            int requestEntity = world.Value.NewEntity();
            ref RTaskForceMovement requestComp = ref tFMovementRequestPool.Value.Add(requestEntity);

            //Заполняем данные запроса
            requestComp = new(
                tF.selfPE,
                targetPE, targetType);
        }

        #region ObjectPanel
        void ObjectPnClickAction(
            ref EcsUguiClickEvent clickEvent)
        {
            //Берём фракцию игрока
            inputData.Value.playerFactionPE.Unpack(world.Value, out int factionEntity);
            ref CFaction faction = ref factionPool.Value.Get(factionEntity);

            //Берём панель объекта
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //Если нажата кнопка закрытия панели объекта
            if(clickEvent.WidgetName == "ClosePanelObjPn")
            {
                //Запрашиваем открытие панели объекта
                ObjectPnActionRequest(ObjectPanelActionRequestType.Close);
            }

            //Иначе, если активна подпанель фракции
            else if(objectPanel.activeSubpanelType == ObjectSubpanelType.Faction)
            {
                //Проверяем клики в подпанели фракции
                FactionSbpnClickAction(ref clickEvent);
            }
            //Иначе, если активна подпанель региона
            else if(objectPanel.activeSubpanelType == ObjectSubpanelType.Region)
            {
                //Проверяем клики в подпанели региона
                RegionSbpnClickAction(ref clickEvent);
            }
            //Иначе, если активна подпанель менеджера флотов
            else if (objectPanel.activeSubpanelType == ObjectSubpanelType.FleetManager)
            {
                //Проверяем клики в подпанели менеджера флотов
                FleetManagerSbpnClickAction(ref clickEvent);
            }
        }

        readonly EcsPoolInject<RGameObjectPanelAction> gameObjectPanelRequestPool = default;
        void ObjectPnActionRequest(
            ObjectPanelActionRequestType requestType,
            EcsPackedEntity objectPE = new())
        {
            //Создаём новую сущность и назначаем ей запрос действия панели объекта
            int requestEntity = world.Value.NewEntity();
            ref RGameObjectPanelAction requestComp = ref gameObjectPanelRequestPool.Value.Add(requestEntity);

            //Заполняем данные запроса
            requestComp = new(
                requestType,
                objectPE);
        }

        #region FactionSubpanel
        void FactionSbpnClickAction(
            ref EcsUguiClickEvent clickEvent)
        {
            //Берём панель объекта
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //Берём подпанель фракции
            UIFactionSubpanel factionSubpanel = objectPanel.factionSubpanel;

            //Если нажата кнопка обзорной вкладки
            if(clickEvent.WidgetName == "OverviewTabFactionSbpn")
            {
                //Запрашиваем отображение обзорной вкладки
                ObjectPnActionRequest(
                    ObjectPanelActionRequestType.FactionOverview,
                    factionSubpanel.activeTab.objectPE);
            }
        }
        #endregion

        #region RegionSubpanel
        void RegionSbpnClickAction(
            ref EcsUguiClickEvent clickEvent)
        {
            //Берём панель объекта
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //Берём подпанель региона
            UIRegionSubpanel regionSubpanel = objectPanel.regionSubpanel;

            //Если нажата кнопка обзорной вкладки
            if (clickEvent.WidgetName == "OverviewTabRegionSbpn")
            {
                //Запрашиваем отображение обзорной вкладки
                ObjectPnActionRequest(
                    ObjectPanelActionRequestType.RegionOverview,
                    regionSubpanel.activeTab.objectPE);
            }
        }
        #endregion

        #region FleetManagerSubpanel
        void FleetManagerSbpnClickAction(
            ref EcsUguiClickEvent clickEvent)
        {
            //Берём панель объекта
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //Берём подпанель менеджера флотов
            UIFleetManagerSubpanel fleetManagerSubpanel = objectPanel.fleetManagerSubpanel;

            //Если нажата кнопка вкладки флотов
            if (clickEvent.WidgetName == "FleetsTabFleetManagerSbpn")
            {
                //Запрашиваем отображение вкладки флотов
                ObjectPnActionRequest(
                    ObjectPanelActionRequestType.FleetManagerFleets,
                    fleetManagerSubpanel.activeTab.objectPE);
            }

            //Иначе, если активна вкладка флотов
            else if (fleetManagerSubpanel.activeTab == fleetManagerSubpanel.fleetsTab)
            {
                //Проверяем клики во вкладке флотов
                FMSbpnFleetsTabClickAction(ref clickEvent);
            }
        }

        void FMSbpnFleetsTabClickAction(
            ref EcsUguiClickEvent clickEvent)
        {
            //Берём фракцию игрока
            inputData.Value.playerFactionPE.Unpack(world.Value, out int factionEntity);
            ref CFaction faction = ref factionPool.Value.Get(factionEntity);

            //Берём панель объекта
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //Берём подпанель менеджера флотов
            UIFleetManagerSubpanel fleetManagerSubpanel = objectPanel.fleetManagerSubpanel;

            //Если источник события имеет компонент TaskForceSummaryPanel
            if(clickEvent.Sender.TryGetComponent(out Game.GUI.Object.FleetManager.Fleets.UITaskForceSummaryPanel taskForceSummaryPanel))
            {
                //Если нажат LShift
                if(inputData.Value.leftShiftKeyPressed == true)
                {
                    //То эта оперативная группа будет добавлена к выбранным или удалена 

                    //Если панель неактивна
                    if(taskForceSummaryPanel.toggle.isOn == false)
                    {
                        //Делаем её активной и заносим в список
                        taskForceSummaryPanel.toggle.isOn = true;
                        inputData.Value.activeTaskForcePEs.Add(taskForceSummaryPanel.selfPE);
                    }
                    //Иначе
                    else
                    {
                        //Делаем её неактивной и удаляем из списка
                        taskForceSummaryPanel.toggle.isOn = false;
                        inputData.Value.activeTaskForcePEs.Remove(taskForceSummaryPanel.selfPE);
                    }
                }
                //Иначе только ЛКМ
                else
                {
                    //Если панель была активна и в списке было больше одной панели, то очищаем список и активируем панель
                    //Иначе отключаем панель
                    bool isActivation
                        = taskForceSummaryPanel.toggle.isOn && (inputData.Value.activeTaskForcePEs.Count > 1)
                        || (taskForceSummaryPanel.toggle.isOn == false);

                    //Для каждой выбранной группы
                    for (int a = 0; a < inputData.Value.activeTaskForcePEs.Count; a++)
                    {
                        //Берём компонент панелей GUI
                        inputData.Value.activeTaskForcePEs[a].Unpack(world.Value, out int tFEntity);
                        ref CTaskForceDisplayedGUIPanels tFDisplayedGUIPanels = ref taskForceDisplayedGUIPanelsPool.Value.Get(tFEntity);

                        //Делаем панель списка флотов неактивной
                        tFDisplayedGUIPanels.fMSbpnFleetsTabSummaryPanel.toggle.isOn = false;
                    }
                    //Очищаем список выбранных групп
                    inputData.Value.activeTaskForcePEs.Clear();

                    //Если требуется активировать панель
                    if (isActivation == true)
                    {
                        //Делаем её активной и заносим в список активных
                        taskForceSummaryPanel.toggle.isOn = true;
                        inputData.Value.activeTaskForcePEs.Add(taskForceSummaryPanel.selfPE);
                    }
                }
            }
            //Иначе, если нажата кнопка создания новой оперативной группы
            else if(clickEvent.WidgetName == "CreateNewTaskForceFMSbpnFleetsTab")
            {
                //Запрашиваем создание новой группы
                TaskForceCreatingRequest(ref faction);
            }
        }

        readonly EcsPoolInject<RTaskForceCreating> taskForceCreatingRequestPool = default;
        void TaskForceCreatingRequest(
            ref CFaction ownerFaction)
        {
            //Создаём новую сущность и назначаем ей запрос создания оперативной группы
            int requestEntity = world.Value.NewEntity();
            ref RTaskForceCreating requestComp = ref taskForceCreatingRequestPool.Value.Add(requestEntity);

            //Заполняем данные запроса
            requestComp = new(ownerFaction.selfPE);
        }
        #endregion
        #endregion

        #region Hexasphere
        void HexasphereCheckMouseInteraction()
        {
            //Если курсор находится над гексасферой
            if(HexasphereCheckMousePosition(out Vector3 position, out Ray ray, out EcsPackedEntity regionPE))
            {
                //Берём регион под курсором
                regionPE.Unpack(world.Value, out int regionEntity);
                ref CRegionHexasphere currentRHS = ref rHSPool.Value.Get(regionEntity);

                //Если нажата ЛКМ
                if(inputData.Value.leftMouseButtonClick)
                {
                    //Запрашиваем отображение подпанели региона
                    ObjectPnActionRequest(
                        uIData.Value.regionSubpanelDefaultTab,
                        currentRHS.selfPE);
                }
                //Иначе, если нажата ПКМ
                else if(inputData.Value.rightMouseButtonClick)
                {
                    //Если открыт менеджер флотов
                    if(sOUI.Value.gameWindow.objectPanel.activeSubpanelType == ObjectSubpanelType.FleetManager)
                    {
                        //Для каждой активной оперативной группы
                        for(int a = 0; a < inputData.Value.activeTaskForcePEs.Count; a++)
                        {
                            //Берём группу
                            inputData.Value.activeTaskForcePEs[a].Unpack(world.Value, out int tFEntity);
                            ref CTaskForce tF = ref taskForcePool.Value.Get(tFEntity);

                            //Запрашиваем перемещение группы
                            TaskForceMovementRequest(
                                ref tF,
                                currentRHS.selfPE, TaskForceMovementTargetType.Region);
                        }
                    }
                }
            }
        }

        bool HexasphereCheckMousePosition(
            out Vector3 position, out Ray ray,
            out EcsPackedEntity regionPE)
        {
            //Проверяем, находится ли курсор над гексасферой
            inputData.Value.isMouseOver = HexasphereGetHitPoint(out position, out ray);

            regionPE = new();

            //Если курсор находится над гексасферой
            if(inputData.Value.isMouseOver == true)
            {
                //Определяем индекс региона, над которым находится курсор
                int regionIndex = RegionGetInRayDirection(
                    ray, position,
                    out Vector3 hitPosition);

                //Если индекс региона больше нуля
                if (regionIndex >= 0)
                {
                    //Берём регион
                    regionsData.Value.regionPEs[regionIndex].Unpack(world.Value, out int regionEntity);
                    ref CRegionHexasphere rHS = ref rHSPool.Value.Get(regionEntity);
                    ref CRegionCore rC = ref rCPool.Value.Get(regionEntity);

                    //Если сущность региона не равна сущности последнего подсвеченного
                    if(inputData.Value.lastHighlightedRegionPE.EqualsTo(rC.selfPE) == false)
                    {
                        //Если последний подсвеченный регион существует
                        if(inputData.Value.lastHighlightedRegionPE.Unpack(world.Value, out int lastHighlightRegionEntity))
                        {
                            //Запрашиваем отключение подсветки для него
                            RegionHideHighlightRequest(
                                inputData.Value.lastHighlightedRegionPE,
                                RegionHighlightRequestType.Hover);
                        }

                        //Обновляем подсвеченный регион
                        inputData.Value.lastHighlightedRegionPE = rC.selfPE;

                        //Запрашиваем включение подсветки для него
                        RegionShowHighlightRequest(
                            rC.selfPE,
                            RegionHighlightRequestType.Hover);
                    }

                    regionPE = rC.selfPE;

                    return true;
                }
                //Иначе, если индекс региона меньше нуля и последний подсвеченный регион существует
                else if(regionIndex < 0 && inputData.Value.lastHighlightedRegionPE.Unpack(world.Value, out int lastHighlightRegionEntity))
                {
                    //Запрашиваем отключение подсветки для него
                    RegionHideHighlightRequest(
                        inputData.Value.lastHighlightedRegionPE,
                        RegionHighlightRequestType.Hover);

                    //Удаляем последний подсвеченный регион
                    inputData.Value.lastHighlightedRegionPE = new();
                }
            }

            return false;
        }

        bool HexasphereGetHitPoint(
            out Vector3 position,
            out Ray ray)
        {
            //Берём луч из положения мыши
            ray = inputData.Value.camera.ScreenPointToRay(Input.mousePosition);

            //Вызываем функцию
            return HexapshereGetHitPoint(ray, out position);
        }

        bool HexapshereGetHitPoint(
            Ray ray,
            out Vector3 position)
        {
            //Если луч касается объекта
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                //Если луч касается объекта гексасферы
                if (hit.collider.gameObject == SceneData.HexashpereGO)
                {
                    //Определяем точку касания
                    position = hit.point;

                    //И возвращаем true
                    return true;
                }
            }

            position = Vector3.zero;
            return false;
        }

        int RegionGetInRayDirection(
            Ray ray,
            Vector3 worldPosition,
            out Vector3 hitPosition)
        {
            hitPosition = worldPosition;

            //Определяем итоговую точку касания
            Vector3 minPoint = worldPosition;
            Vector3 maxPoint = worldPosition + 0.5f * mapGenerationData.Value.hexasphereScale * ray.direction;

            float rangeMin = mapGenerationData.Value.hexasphereScale * 0.5f;
            rangeMin *= rangeMin;

            float rangeMax = worldPosition.sqrMagnitude;

            float distance;
            Vector3 bestPoint = maxPoint;

            //Уточняем точку
            for (int a = 0; a < 10; a++)
            {
                Vector3 midPoint = (minPoint + maxPoint) * 0.5f;

                distance = midPoint.sqrMagnitude;

                if (distance < rangeMin)
                {
                    maxPoint = midPoint;
                    bestPoint = midPoint;
                }
                else if (distance > rangeMax)
                {
                    maxPoint = midPoint;
                }
                else
                {
                    minPoint = midPoint;
                }
            }

            //Берём индекс региона 
            int nearestRegionIndex = RegionGetAtLocalPosition(SceneData.HexashpereGO.transform.InverseTransformPoint(worldPosition));
            //Если индекс меньше нуля, выходим из функции
            if (nearestRegionIndex < 0)
            {
                return -1;
            }

            //Определяем индекс региона
            Vector3 currentPoint = worldPosition;

            //Берём ближайший регион
            regionsData.Value.regionPEs[nearestRegionIndex].Unpack(world.Value, out int nearestRegionEntity);
            ref CRegionHexasphere nearestRHS = ref rHSPool.Value.Get(nearestRegionEntity);

            //Определяем верхнюю точку региона
            Vector3 regionTop = SceneData.HexashpereGO.transform.TransformPoint(
                nearestRHS.center * (1.0f + nearestRHS.ExtrudeAmount * mapGenerationData.Value.extrudeMultiplier));

            //Определяем высоту региона и высоту луча
            float regionHeight = regionTop.sqrMagnitude;
            float rayHeight = currentPoint.sqrMagnitude;

            float minDistance = 1e6f;
            distance = minDistance;

            //Определяем индекс региона-кандидата
            int candidateRegionIndex = -1;

            const int NUM_STEPS = 10;
            //Уточняем точку
            for (int a = 1; a <= NUM_STEPS; a++)
            {
                distance = Mathf.Abs(rayHeight - regionHeight);

                //Если расстояние меньше минимального
                if (distance < minDistance)
                {
                    //Обновляем минимальное расстояние и кандидата
                    minDistance = distance;
                    candidateRegionIndex = nearestRegionIndex;
                    hitPosition = currentPoint;
                }

                if (rayHeight < regionHeight)
                {
                    return candidateRegionIndex;
                }

                float t = a / (float)NUM_STEPS;

                currentPoint = worldPosition * (1f - t) + bestPoint * t;

                nearestRegionIndex = RegionGetAtLocalPosition(SceneData.HexashpereGO.transform.InverseTransformPoint(currentPoint));

                if (nearestRegionIndex < 0)
                {
                    break;
                }

                //Обновляем ближайший регион
                regionsData.Value.regionPEs[nearestRegionIndex].Unpack(world.Value, out nearestRegionEntity);
                nearestRHS = ref rHSPool.Value.Get(nearestRegionEntity);

                regionTop = SceneData.HexashpereGO.transform.TransformPoint(
                    nearestRHS.center * (1.0f + nearestRHS.ExtrudeAmount * mapGenerationData.Value.extrudeMultiplier));

                //Определяем высоту региона и высоту луча
                regionHeight = regionTop.sqrMagnitude;
                rayHeight = currentPoint.sqrMagnitude;
            }

            //Если расстояние меньше минимального
            if (distance < minDistance)
            {
                //Обновляем минимальное расстояние и кандидата
                minDistance = distance;
                candidateRegionIndex = nearestRegionIndex;
                hitPosition = currentPoint;
            }

            if (rayHeight < regionHeight)
            {
                return candidateRegionIndex;
            }
            else
            {
                return -1;
            }
        }

        int RegionGetAtLocalPosition(
            Vector3 localPosition)
        {
            //Проверяем, не последний ли это регион
            if (inputData.Value.lastHitRegionIndex >= 0 && inputData.Value.lastHitRegionIndex < regionsData.Value.regionPEs.Length)
            {
                //Берём последний регион
                regionsData.Value.regionPEs[inputData.Value.lastHitRegionIndex].Unpack(world.Value, out int lastHitRegionEntity);
                ref CRegionCore lastHitRC = ref rCPool.Value.Get(lastHitRegionEntity);

                //Определяем расстояние до центра региона
                float distance = Vector3.SqrMagnitude(lastHitRC.center - localPosition);

                bool isValid = true;

                //Для каждого соседнего региона
                for (int a = 0; a < lastHitRC.neighbourRegionPEs.Length; a++)
                {
                    //Берём соседний регион
                    lastHitRC.neighbourRegionPEs[a].Unpack(world.Value, out int neighbourRegionEntity);
                    ref CRegionHexasphere neighbourRHS = ref rHSPool.Value.Get(neighbourRegionEntity);

                    //Определяем расстояние до центре региона
                    float otherDistance = Vector3.SqrMagnitude(neighbourRHS.center - localPosition);

                    //Если оно меньше расстояния до последнего региона
                    if (otherDistance < distance)
                    {
                        //Отмечаем это и выходим из цикла
                        isValid = false;
                        break;
                    }
                }

                //Если это последний регион
                if (isValid == true)
                {
                    return inputData.Value.lastHitRegionIndex;
                }
            }
            //Иначе
            else
            {
                //Обнуляем индекс последнего региона
                inputData.Value.lastHitRegionIndex = 0;
            }

            //Следуем кратчайшему пути к минимальному расстоянию
            regionsData.Value.regionPEs[inputData.Value.lastHitRegionIndex].Unpack(world.Value, out int nearestRegionEntity);
            ref CRegionCore nearestRC = ref rCPool.Value.Get(nearestRegionEntity);

            float minDist = 1e6f;

            //Для каждого региона
            for (int a = 0; a < regionsData.Value.regionPEs.Length; a++)
            {
                //Берём ближайший регион 
                RegionGetNearestToPosition(
                    ref nearestRC.neighbourRegionPEs,
                    localPosition,
                    out float regionDistance).Unpack(world.Value, out int newNearestRegionEntity);

                //Если расстояние меньше минимального
                if (regionDistance < minDist)
                {
                    //Обновляем регион и минимальное расстояние 
                    nearestRC = ref rCPool.Value.Get(newNearestRegionEntity);

                    minDist = regionDistance;
                }
                //Иначе выходим из цикла
                else
                {
                    break;
                }
            }

            //Индекс последнего региона - это индекс ближайшего
            inputData.Value.lastHitRegionIndex = nearestRC.Index;

            return inputData.Value.lastHitRegionIndex;
        }

        EcsPackedEntity RegionGetNearestToPosition(
            ref EcsPackedEntity[] regionPEs,
            Vector3 localPosition,
            out float minDistance)
        {
            minDistance = float.MaxValue;

            EcsPackedEntity nearestRegionPE = new();

            //Для каждого региона в массиве
            for (int a = 0; a < regionPEs.Length; a++)
            {
                //Берём регион
                regionPEs[a].Unpack(world.Value, out int regionEntity);
                ref CRegionHexasphere rHS = ref rHSPool.Value.Get(regionEntity);

                //Берём центр региона
                Vector3 center = rHS.center;

                //Рассчитываем расстояние
                float distance
                    = (center.x - localPosition.x) * (center.x - localPosition.x)
                    + (center.y - localPosition.y) * (center.y - localPosition.y)
                    + (center.z - localPosition.z) * (center.z - localPosition.z);

                //Если расстояние меньше минимального
                if (distance < minDistance)
                {
                    //Обновляем регион и минимальное расстояние
                    nearestRegionPE = rHS.selfPE;
                    minDistance = distance;
                }
            }

            return nearestRegionPE;
        }

        readonly EcsPoolInject<RGameShowRegionHighlight> gameShowRegionHighlightRequestPool = default;
        void RegionShowHighlightRequest(
            EcsPackedEntity regionPE,
            RegionHighlightRequestType requestType)
        {
            //Создаём новую сущность и назначаем ей запрос включения подсветки региона
            int requestEntity = world.Value.NewEntity();
            ref RGameShowRegionHighlight requestComp = ref gameShowRegionHighlightRequestPool.Value.Add(requestEntity);

            //Заполняем данные запроса
            requestComp = new(
                regionPE,
                requestType);
        }

        readonly EcsPoolInject<RGameHideRegionHighlight> gameHideRegionHighlightRequestPool = default;
        void RegionHideHighlightRequest(
            EcsPackedEntity regionPE,
            RegionHighlightRequestType requestType)
        {
            //Создаём новую сущность и назначаем ей запрос выключения подсветки региона
            int requestEntity = world.Value.NewEntity();
            ref RGameHideRegionHighlight requestComp = ref gameHideRegionHighlightRequestPool.Value.Add(requestEntity);

            //Заполняем данные запроса
            requestComp = new(
                regionPE,
                requestType);
        }
        #endregion
        #endregion

        readonly EcsPoolInject<RGeneralAction> generalActionRequestPool = default;
        void GeneralActionRequest(
            GeneralActionType actionType)
        {
            //Создаём новую сущность и назначаем ей запрос общего действия
            int requestEntity = world.Value.NewEntity();
            ref RGeneralAction requestComp = ref generalActionRequestPool.Value.Add(requestEntity);

            //Заполняем данные запроса
            requestComp = new(actionType);
        }
    }
}