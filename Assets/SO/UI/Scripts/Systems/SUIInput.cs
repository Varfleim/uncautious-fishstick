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
using SO.Country;
using SO.Warfare.Fleet;
using SO.Warfare.Fleet.Events;
using SO.Warfare.Fleet.Missions.Events;
using SO.Warfare.Fleet.Movement;
using SO.Warfare.Fleet.Movement.Events;
using SO.Map.MapArea;
using SO.Map.Hexasphere;
using SO.Map.UI;
using SO.Map.Generation;
using SO.Map.Events;
using SO.Map.Province;

namespace SO.UI
{
    public class SUIInput : IEcsInitSystem, IEcsRunSystem
    {
        //Миры
        readonly EcsWorldInject world = default;
        EcsWorld uguiMapWorld;
        EcsWorld uguiUIWorld;


        //Карта
        readonly EcsPoolInject<CProvinceHexasphere> pHSPool = default;
        readonly EcsPoolInject<CProvinceCore> pCPool = default;

        readonly EcsFilterInject<Inc<CProvinceCore, CProvinceDisplayedGUIPanels>> provinceDisplayedGUIPanelsFilter = default;
        readonly EcsPoolInject<CProvinceDisplayedGUIPanels> provinceDisplayedGUIPanelsPool = default;

        readonly EcsPoolInject<CMapArea> mAPool = default;

        //Страны
        readonly EcsPoolInject<CCountry> countryPool = default;

        //Военное дело
        readonly EcsPoolInject<CTaskForce> taskForcePool = default;
        readonly EcsPoolInject<CTaskForceDisplayedGUIPanels> tFDisplayedGUIPanelsPool = default;


        EcsFilter clickEventMapFilter;
        EcsPool<EcsUguiClickEvent> clickEventMapPool;

        EcsFilter clickEventUIFilter;
        EcsPool<EcsUguiClickEvent> clickEventUIPool;

        //Данные
        readonly EcsCustomInject<UIData> uIData = default;
        readonly EcsCustomInject<MapGenerationData> mapGenerationData = default;
        readonly EcsCustomInject<ProvincesData> provincesData = default;
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
                    ChangeMapModeRequestType.Country);
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

            //Берём страну игрока
            inputData.Value.playerCountryPE.Unpack(world.Value, out int countryEntity);
            ref CCountry country = ref countryPool.Value.Get(countryEntity);

            //Если клик по флагу страну игрока
            if (clickEvent.WidgetName == "PlayerCountryFlag")
            {
                //Запрашиваем отображение панели страны
                ObjectPnActionRequest(
                    uIData.Value.countrySubpanelDefaultTab,
                    country.selfPE);
            }
            //Иначе, если клик по кнопке менеджера флотов на панели главных кнопок
            else if(clickEvent.WidgetName == "FleetManagerMBtnPn")
            {
                //Запрашиваем отображение менеджера флотов
                ObjectPnActionRequest(
                    uIData.Value.fleetManagerSubpanelDefaultTab,
                    country.selfPE);
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
            //Берём страну игрока
            inputData.Value.playerCountryPE.Unpack(world.Value, out int countryEntity);
            ref CCountry country = ref countryPool.Value.Get(countryEntity);

            //Берём панель объекта
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //Если нажата кнопка закрытия панели объекта
            if(clickEvent.WidgetName == "ClosePanelObjPn")
            {
                //Запрашиваем открытие панели объекта
                ObjectPnActionRequest(ObjectPanelActionRequestType.Close);
            }

            //Иначе, если активна подпанель страны
            else if (objectPanel.activeSubpanelType == ObjectSubpanelType.Country)
            {
                //Проверяем клики в подпанели страны
                CountrySbpnClickAction(ref clickEvent);
            }
            //Иначе, если активна подпанель провинции
            else if(objectPanel.activeSubpanelType == ObjectSubpanelType.Province)
            {
                //Проверяем клики в подпанели провинции
                ProvinceSbpnClickAction(ref clickEvent);
            }
            //Иначе, если активна подпанель области карты
            else if (objectPanel.activeSubpanelType == ObjectSubpanelType.MapArea)
            {
                //Проверяем клики в подпанели области
                MapAreaSbpnClickAction(ref clickEvent);
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
            EcsPackedEntity objectPE = new(), EcsPackedEntity secondObjectPE = new())
        {
            //Создаём новую сущность и назначаем ей запрос действия панели объекта
            int requestEntity = world.Value.NewEntity();
            ref RGameObjectPanelAction requestComp = ref gameObjectPanelRequestPool.Value.Add(requestEntity);

            //Заполняем данные запроса
            requestComp = new(
                requestType,
                objectPE, secondObjectPE);
        }

        #region CountrySubpanel
        void CountrySbpnClickAction(
            ref EcsUguiClickEvent clickEvent)
        {
            //Берём панель объекта
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //Берём подпанель страны
            UICountrySubpanel countrySubpanel = objectPanel.countrySubpanel;

            //Если нажата кнопка обзорной вкладки
            if(clickEvent.WidgetName == "OverviewTabCountrySbpn")
            {
                //Запрашиваем отображение обзорной вкладки
                ObjectPnActionRequest(
                    ObjectPanelActionRequestType.CountryOverview,
                    countrySubpanel.activeTab.objectPE);
            }
        }
        #endregion

        #region ProvinceSubpanel
        void ProvinceSbpnClickAction(
            ref EcsUguiClickEvent clickEvent)
        {
            //Берём панель объекта
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //Берём подпанель провинции
            UIProvinceSubpanel provinceSubpanel = objectPanel.provinceSubpanel;

            //Если нажата кнопка обзорной вкладки
            if (clickEvent.WidgetName == "OverviewTabProvinceSbpn")
            {
                //Запрашиваем отображение обзорной вкладки
                ObjectPnActionRequest(
                    ObjectPanelActionRequestType.ProvinceOverview,
                    provinceSubpanel.activeTab.objectPE);
            }
        }
        #endregion

        #region MapAreaSubpanel
        void MapAreaSbpnClickAction(
            ref EcsUguiClickEvent clickEvent)
        {
            //Берём панель объекта
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //Берём подпанель области карты
            UIMapAreaSubpanel mASubpanel = objectPanel.mapAreaSubpanel;

            //Если нажата кнопка обзорной вкладки
            if (clickEvent.WidgetName == "OverviewTabMapAreaSbpn")
            {
                //Запрашиваем отображение обзорной вкладки
                ObjectPnActionRequest(
                    ObjectPanelActionRequestType.MapAreaOverview,
                    mASubpanel.activeTab.objectPE);
            }
            //Иначе, если нажата кнопка вкладки провинций
            else if(clickEvent.WidgetName == "ProvincesTabMapAreaSbpn")
            {
                //Запрашиваем отображение вкладки провинций
                ObjectPnActionRequest(
                    ObjectPanelActionRequestType.MapAreaProvinces,
                    mASubpanel.activeTab.objectPE);
            }

            //ТЕСТ
            //Если нажата кнопка захвата области
            else if(clickEvent.WidgetName == "ConquerMapAreaSbpn")
            {
                //Запрашиваем смену владельца области на страну игрока
                MapAreaChangeOwnerRequest(
                    inputData.Value.playerCountryPE,
                    mASubpanel.activeTab.objectPE);
            }
            //ТЕСТ

            //Иначе, если открыта вкладка провинций
            else if(mASubpanel.activeTab == mASubpanel.provincesTab)
            {
                Debug.LogWarning("!");

                //ТЕСТ
                //Для каждой провинции с отображаемыми панелями GUI
                foreach(int provinceEntity in provinceDisplayedGUIPanelsFilter.Value)
                {
                    //Берём панели провинции
                    ref CProvinceDisplayedGUIPanels provinceDisplayedGUIPanels = ref provinceDisplayedGUIPanelsPool.Value.Get(provinceEntity);

                    //Если у провинции есть обзорная панель вкладки провинций
                    if(provinceDisplayedGUIPanels.mASbpnProvincesTabSummaryPanel != null)
                    {
                        Debug.LogWarning("!");

                        Debug.LogWarning(clickEvent.Sender.transform.parent);

                        //Если она является родительским объектом источника события
                        if (provinceDisplayedGUIPanels.mASbpnProvincesTabSummaryPanel.transform == clickEvent.Sender.transform.parent)
                        {
                            Debug.LogWarning("!");

                            //Берём провинцию
                            ref CProvinceCore pC = ref pCPool.Value.Get(provinceEntity);

                            //То запрашиваем смену владельца провинции
                            ProvinceChangeOwnerRequest(
                                inputData.Value.playerCountryPE,
                                pC.selfPE,
                                ProvinceChangeOwnerType.Test);

                            break;
                        }
                    }
                }
                //ТЕСТ
            }
        }

        readonly EcsPoolInject<RMapAreaChangeOwner> mAChangeOwnerRequestPool = default;
        void MapAreaChangeOwnerRequest(
            EcsPackedEntity countryPE,
            EcsPackedEntity mAPE)
        {
            //Создаём новую сущность и назначаем ей запрос смены владельца области карты
            int requestEntity = world.Value.NewEntity();
            ref RMapAreaChangeOwner requestComp = ref mAChangeOwnerRequestPool.Value.Add(requestEntity);

            //Заполняем данные запроса
            requestComp = new(
                countryPE,
                mAPE,
                MapAreaChangeOwnerType.Test);
        }

        readonly EcsPoolInject<RProvinceChangeOwner> provinceChangeOwnerRequestPool = default;
        void ProvinceChangeOwnerRequest(
            EcsPackedEntity countryPE,
            EcsPackedEntity provincePE,
            ProvinceChangeOwnerType requestType)
        {
            //Создаём новую сущность и назначаем ей запрос смены владельца провинции
            int requestEntity = world.Value.NewEntity();
            ref RProvinceChangeOwner requestComp = ref provinceChangeOwnerRequestPool.Value.Add(requestEntity);

            //Заполняем данные запроса
            requestComp = new(
                countryPE,
                provincePE,
                requestType);
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
            //Берём страну игрока
            inputData.Value.playerCountryPE.Unpack(world.Value, out int countryEntity);
            ref CCountry country = ref countryPool.Value.Get(countryEntity);

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
                        ref CTaskForceDisplayedGUIPanels tFDisplayedGUIPanels = ref tFDisplayedGUIPanelsPool.Value.Get(tFEntity);

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
                TaskForceCreatingRequest(ref country);
            }
        }

        readonly EcsPoolInject<RTaskForceCreating> taskForceCreatingRequestPool = default;
        void TaskForceCreatingRequest(
            ref CCountry ownerCountry)
        {
            //Создаём новую сущность и назначаем ей запрос создания оперативной группы
            int requestEntity = world.Value.NewEntity();
            ref RTaskForceCreating requestComp = ref taskForceCreatingRequestPool.Value.Add(requestEntity);

            //Заполняем данные запроса
            requestComp = new(ownerCountry.selfPE);
        }
        #endregion
        #endregion

        #region Hexasphere
        void HexasphereCheckMouseInteraction()
        {
            //Если курсор находится над гексасферой
            if(HexasphereCheckMousePosition(out Vector3 position, out Ray ray, out EcsPackedEntity provincePE))
            {
                //Берём провинцию под курсором
                provincePE.Unpack(world.Value, out int provinceEntity);
                ref CProvinceHexasphere currentPHS = ref pHSPool.Value.Get(provinceEntity);

                //ТЕСТ
                ref CProvinceCore currentPC = ref pCPool.Value.Get(provinceEntity);

                //Берём родительскую область карты провинции
                currentPC.ParentMapAreaPE.Unpack(world.Value, out int mAEntity);
                ref CMapArea mA = ref mAPool.Value.Get(mAEntity);
                //ТЕСТ

                //Если нажата ЛКМ
                if(inputData.Value.leftMouseButtonClick)
                {
                    //Запрашиваем отображение подпанели провинции
                    //ObjectPnActionRequest(
                    //    uIData.Value.provinceSubpanelDefaultTab,
                    //    currentPHS.selfPE);

                    //Запрашиваем отображение подпанели области
                    ObjectPnActionRequest(
                        uIData.Value.mapAreaSubpanelDefaultTab,
                        mA.selfPE, currentPHS.selfPE);
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
                                currentPHS.selfPE, TaskForceMovementTargetType.Province);
                        }
                    }
                }
            }
        }

        bool HexasphereCheckMousePosition(
            out Vector3 position, out Ray ray,
            out EcsPackedEntity provincePE)
        {
            //Проверяем, находится ли курсор над гексасферой
            inputData.Value.isMouseOver = HexasphereGetHitPoint(out position, out ray);

            provincePE = new();

            //Если курсор находится над гексасферой
            if(inputData.Value.isMouseOver == true)
            {
                //Определяем индекс провинции, над которой находится курсор
                int provinceIndex = ProvinceGetInRayDirection(
                    ray, position,
                    out Vector3 hitPosition);

                //Если индекс провинции больше нуля
                if (provinceIndex >= 0)
                {
                    //Берём провинцию
                    provincesData.Value.provincePEs[provinceIndex].Unpack(world.Value, out int provinceEntity);
                    ref CProvinceHexasphere pHS = ref pHSPool.Value.Get(provinceEntity);
                    ref CProvinceCore pC = ref pCPool.Value.Get(provinceEntity);

                    //Если сущность провинции не равна сущности последнего подсвеченного
                    if(inputData.Value.lastHighlightedProvincePE.EqualsTo(pC.selfPE) == false)
                    {
                        //Если последняя подсвеченная провинция существует
                        if(inputData.Value.lastHighlightedProvincePE.Unpack(world.Value, out int lastHighlightProvinceEntity))
                        {
                            //Запрашиваем отключение подсветки для него
                            ProvinceHideHighlightRequest(
                                inputData.Value.lastHighlightedProvincePE,
                                ProvinceHighlightRequestType.Hover);
                        }

                        //Обновляем подсвеченную провинцию
                        inputData.Value.lastHighlightedProvincePE = pC.selfPE;

                        //Запрашиваем включение подсветки для него
                        ProvinceShowHighlightRequest(
                            pC.selfPE,
                            ProvinceHighlightRequestType.Hover);
                    }

                    provincePE = pC.selfPE;

                    return true;
                }
                //Иначе, если индекс провинции меньше нуля и последняя подсвеченная провинция существует
                else if(provinceIndex < 0 && inputData.Value.lastHighlightedProvincePE.Unpack(world.Value, out int lastHighlightProvinceEntity))
                {
                    //Запрашиваем отключение подсветки для него
                    ProvinceHideHighlightRequest(
                        inputData.Value.lastHighlightedProvincePE,
                        ProvinceHighlightRequestType.Hover);

                    //Удаляем последнюю подсвеченную провинцию
                    inputData.Value.lastHighlightedProvincePE = new();
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

        int ProvinceGetInRayDirection(
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

            //Берём индекс провинции 
            int nearestProvinceIndex = ProvinceGetAtLocalPosition(SceneData.HexashpereGO.transform.InverseTransformPoint(worldPosition));
            //Если индекс меньше нуля, выходим из функции
            if (nearestProvinceIndex < 0)
            {
                return -1;
            }

            //Определяем индекс провинции
            Vector3 currentPoint = worldPosition;

            //Берём ближайшую провинцию
            provincesData.Value.provincePEs[nearestProvinceIndex].Unpack(world.Value, out int nearestProvinceEntity);
            ref CProvinceHexasphere nearestPHS = ref pHSPool.Value.Get(nearestProvinceEntity);

            //Определяем верхнюю точку провинции
            Vector3 provinceTop = SceneData.HexashpereGO.transform.TransformPoint(
                nearestPHS.center * (1.0f + nearestPHS.ExtrudeAmount * mapGenerationData.Value.extrudeMultiplier));

            //Определяем высоту провинции и высоту луча
            float provinceHeight = provinceTop.sqrMagnitude;
            float rayHeight = currentPoint.sqrMagnitude;

            float minDistance = 1e6f;
            distance = minDistance;

            //Определяем индекс провинции-кандидата
            int candidateProvinceIndex = -1;

            const int NUM_STEPS = 10;
            //Уточняем точку
            for (int a = 1; a <= NUM_STEPS; a++)
            {
                distance = Mathf.Abs(rayHeight - provinceHeight);

                //Если расстояние меньше минимального
                if (distance < minDistance)
                {
                    //Обновляем минимальное расстояние и кандидата
                    minDistance = distance;
                    candidateProvinceIndex = nearestProvinceIndex;
                    hitPosition = currentPoint;
                }

                if (rayHeight < provinceHeight)
                {
                    return candidateProvinceIndex;
                }

                float t = a / (float)NUM_STEPS;

                currentPoint = worldPosition * (1f - t) + bestPoint * t;

                nearestProvinceIndex = ProvinceGetAtLocalPosition(SceneData.HexashpereGO.transform.InverseTransformPoint(currentPoint));

                if (nearestProvinceIndex < 0)
                {
                    break;
                }

                //Обновляем ближайшую провинцию
                provincesData.Value.provincePEs[nearestProvinceIndex].Unpack(world.Value, out nearestProvinceEntity);
                nearestPHS = ref pHSPool.Value.Get(nearestProvinceEntity);

                provinceTop = SceneData.HexashpereGO.transform.TransformPoint(
                    nearestPHS.center * (1.0f + nearestPHS.ExtrudeAmount * mapGenerationData.Value.extrudeMultiplier));

                //Определяем высоту провинции и высоту луча
                provinceHeight = provinceTop.sqrMagnitude;
                rayHeight = currentPoint.sqrMagnitude;
            }

            //Если расстояние меньше минимального
            if (distance < minDistance)
            {
                //Обновляем минимальное расстояние и кандидата
                minDistance = distance;
                candidateProvinceIndex = nearestProvinceIndex;
                hitPosition = currentPoint;
            }

            if (rayHeight < provinceHeight)
            {
                return candidateProvinceIndex;
            }
            else
            {
                return -1;
            }
        }

        int ProvinceGetAtLocalPosition(
            Vector3 localPosition)
        {
            //Проверяем, не последняя ли это провинция
            if (inputData.Value.lastHitProvinceIndex >= 0 && inputData.Value.lastHitProvinceIndex < provincesData.Value.provincePEs.Length)
            {
                //Берём последнюю провинцию
                provincesData.Value.provincePEs[inputData.Value.lastHitProvinceIndex].Unpack(world.Value, out int lastHitProvinceEntity);
                ref CProvinceCore lastHitPC = ref pCPool.Value.Get(lastHitProvinceEntity);

                //Определяем расстояние до центра провинции
                float distance = Vector3.SqrMagnitude(lastHitPC.center - localPosition);

                bool isValid = true;

                //Для каждой соседней провинции
                for (int a = 0; a < lastHitPC.neighbourProvincePEs.Length; a++)
                {
                    //Берём соседнюю провинцию
                    lastHitPC.neighbourProvincePEs[a].Unpack(world.Value, out int neighbourProvinceEntity);
                    ref CProvinceHexasphere neighbourPHS = ref pHSPool.Value.Get(neighbourProvinceEntity);

                    //Определяем расстояние до центра провинции
                    float otherDistance = Vector3.SqrMagnitude(neighbourPHS.center - localPosition);

                    //Если оно меньше расстояния до последней провинции
                    if (otherDistance < distance)
                    {
                        //Отмечаем это и выходим из цикла
                        isValid = false;
                        break;
                    }
                }

                //Если это последняя провинция
                if (isValid == true)
                {
                    return inputData.Value.lastHitProvinceIndex;
                }
            }
            //Иначе
            else
            {
                //Обнуляем индекс последней провинции
                inputData.Value.lastHitProvinceIndex = 0;
            }

            //Следуем кратчайшему пути к минимальному расстоянию
            provincesData.Value.provincePEs[inputData.Value.lastHitProvinceIndex].Unpack(world.Value, out int nearestProvinceEntity);
            ref CProvinceCore nearestPC = ref pCPool.Value.Get(nearestProvinceEntity);

            float minDist = 1e6f;

            //Для каждой провинции
            for (int a = 0; a < provincesData.Value.provincePEs.Length; a++)
            {
                //Берём ближайшую провинцию 
                ProvinceGetNearestToPosition(
                    ref nearestPC.neighbourProvincePEs,
                    localPosition,
                    out float provinceDistance).Unpack(world.Value, out int newNearestProvinceEntity);

                //Если расстояние меньше минимального
                if (provinceDistance < minDist)
                {
                    //Обновляем провинцию и минимальное расстояние 
                    nearestPC = ref pCPool.Value.Get(newNearestProvinceEntity);

                    minDist = provinceDistance;
                }
                //Иначе выходим из цикла
                else
                {
                    break;
                }
            }

            //Индекс последней провинции - это индекс ближайшего
            inputData.Value.lastHitProvinceIndex = nearestPC.Index;

            return inputData.Value.lastHitProvinceIndex;
        }

        EcsPackedEntity ProvinceGetNearestToPosition(
            ref EcsPackedEntity[] provincePEs,
            Vector3 localPosition,
            out float minDistance)
        {
            minDistance = float.MaxValue;

            EcsPackedEntity nearestProvincePE = new();

            //Для каждой провинции в массиве
            for (int a = 0; a < provincePEs.Length; a++)
            {
                //Берём провинцию
                provincePEs[a].Unpack(world.Value, out int provinceEntity);
                ref CProvinceHexasphere pHS = ref pHSPool.Value.Get(provinceEntity);

                //Берём центр провинции
                Vector3 center = pHS.center;

                //Рассчитываем расстояние
                float distance
                    = (center.x - localPosition.x) * (center.x - localPosition.x)
                    + (center.y - localPosition.y) * (center.y - localPosition.y)
                    + (center.z - localPosition.z) * (center.z - localPosition.z);

                //Если расстояние меньше минимального
                if (distance < minDistance)
                {
                    //Обновляем провинцию и минимальное расстояние
                    nearestProvincePE = pHS.selfPE;
                    minDistance = distance;
                }
            }

            return nearestProvincePE;
        }

        readonly EcsPoolInject<RGameShowProvinceHighlight> gameShowProvinceHighlightRequestPool = default;
        void ProvinceShowHighlightRequest(
            EcsPackedEntity provincePE,
            ProvinceHighlightRequestType requestType)
        {
            //Создаём новую сущность и назначаем ей запрос включения подсветки провинции
            int requestEntity = world.Value.NewEntity();
            ref RGameShowProvinceHighlight requestComp = ref gameShowProvinceHighlightRequestPool.Value.Add(requestEntity);

            //Заполняем данные запроса
            requestComp = new(
                provincePE,
                requestType);
        }

        readonly EcsPoolInject<RGameHideProvinceHighlight> gameHideProvinceHighlightRequestPool = default;
        void ProvinceHideHighlightRequest(
            EcsPackedEntity provincePE,
            ProvinceHighlightRequestType requestType)
        {
            //Создаём новую сущность и назначаем ей запрос выключения подсветки провинции
            int requestEntity = world.Value.NewEntity();
            ref RGameHideProvinceHighlight requestComp = ref gameHideProvinceHighlightRequestPool.Value.Add(requestEntity);

            //Заполняем данные запроса
            requestComp = new(
                provincePE,
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