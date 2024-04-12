
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using Leopotam.EcsLite.ExtendedSystems;

using SO.UI;
using SO.UI.Events;
using SO.UI.MainMenu;
using SO.UI.MainMenu.Events;
using SO.UI.Game;
using SO.UI.Game.Map;
using SO.UI.Game.Map.Events;
using SO.UI.Game.GUI;
using SO.UI.Game.Events;
using SO.UI.Game.GUI.Object;
using SO.UI.Game.GUI.Object.Character;
using SO.UI.Game.GUI.Object.Region;
using SO.UI.Game.GUI.Object.StrategicArea;
using SO.UI.Game.GUI.Object.FleetManager;
using SO.UI.Game.GUI.Object.Events;
using SO.Map;
using SO.Character;
using SO.Warfare.Fleet;
using SO.Map.StrategicArea;
using SO.Map.Hexasphere;
using SO.Map.UI;
using SO.Map.Generation;
using SO.Map.Economy;
using SO.Map.Region;

namespace SO.UI
{
    public class SUIDisplay : IEcsInitSystem, IEcsRunSystem
    {
        //Миры
        readonly EcsWorldInject world = default;


        //Карта
        readonly EcsPoolInject<CRegionHexasphere> rHSPool = default;
        readonly EcsPoolInject<CRegionCore> rCPool = default;

        readonly EcsPoolInject<CRegionEconomy> rEPool = default;

        readonly EcsPoolInject<CRegionDisplayedGUIPanels> regionDisplayedGUIPanelsPool = default;
        readonly EcsFilterInject<Inc<CRegionHexasphere, CRegionDisplayedMapPanels>> regionDisplayedMapPanelsFilter = default;
        readonly EcsPoolInject<CRegionDisplayedMapPanels> regionDisplayedMapPanelsPool = default;

        readonly EcsPoolInject<CStrategicArea> sAPool = default;

        //Персонажи
        readonly EcsPoolInject<CCharacter> characterPool = default;

        //Военное дело
        readonly EcsPoolInject<CTaskForce> tFPool = default;
        readonly EcsPoolInject<CTaskForceDisplayedGUIPanels> tFDisplayedGUIPanelsPool = default;
        readonly EcsPoolInject<CTaskForceDisplayedMapPanels> tFDisplayedMapPanelsPool = default;


        //Общие события
        readonly EcsFilterInject<Inc<RGeneralAction>> generalActionRequestFilter = default;
        readonly EcsPoolInject<RGeneralAction> generalActionRequestPool = default;

        readonly EcsPoolInject<EcsGroupSystemState> ecsGroupSystemStatePool = default;


        //Данные
        readonly EcsCustomInject<UIData> uIData = default;
        readonly EcsCustomInject<MapGenerationData> mapGenerationData = default;
        readonly EcsCustomInject<RuntimeData> runtimeData = default;

        readonly EcsCustomInject<SOUI> sOUI = default;

        public void Init(IEcsSystems systems)
        {
            ListPool<int>.Init();

            //Заносим префабы в их графы
            GORegionRenderer.regionRendererPrefab = uIData.Value.regionRendererPrefab;
            CRegionDisplayedMapPanels.mapPanelGroupPrefab = uIData.Value.mapPanelGroup;

            Game.GUI.Object.StrategicArea.Regions.UIRegionSummaryPanel.panelPrefab = uIData.Value.sASbpnRegionsTabRegionSummaryPanelPrefab;
            UIRCMainMapPanel.panelPrefab = uIData.Value.rCMainMapPanelPrefab;

            Game.GUI.Object.FleetManager.Fleets.UITaskForceSummaryPanel.panelPrefab = uIData.Value.fMSbpnFleetsTabTaskForceSummaryPanelPrefab;
            UITFMainMapPanel.panelPrefab = uIData.Value.tFMainMapPanelPrefab;

            //Открываем окно главного меню
            MainMenuOpenWindow();
        }

        public void Run(IEcsSystems systems)
        {
            //Для каждого общего запроса
            foreach (int requestEntity in generalActionRequestFilter.Value)
            {
                //Берём запрос
                ref RGeneralAction requestComp = ref generalActionRequestPool.Value.Get(requestEntity);

                //Если запрашивается закрытие игры
                if (requestComp.actionType == GeneralActionType.QuitGame)
                {
                    Debug.LogError("Выход из игры!");

                    //Закрываем игру
                    Application.Quit();
                }

                generalActionRequestPool.Value.Del(requestEntity);
            }

            //Если активно окно игры
            if (sOUI.Value.activeMainWindowType == MainWindowType.Game)
            {
                //Проверяем события в окне игры
                GameEventCheck();

                //Обновляем панели карты
                MapUIMapPanelsUpdate();
            }
            //Иначе, если активно окно главного меню
            else if (sOUI.Value.activeMainWindowType == MainWindowType.MainMenu)
            {
                //Проверяем события в окне главного меню
                MainMenuAction();
            }
        }

        void CloseMainWindow()
        {
            //Если какое-либо главное окно было активным
            if (sOUI.Value.activeMainWindowType != MainWindowType.None)
            {
                sOUI.Value.activeMainWindow.gameObject.SetActive(false);
                sOUI.Value.activeMainWindow = null;
                sOUI.Value.activeMainWindowType = MainWindowType.None;
            }
        }

        #region MainMenu
        readonly EcsFilterInject<Inc<RMainMenuAction>> mainMenuActionRequestFilter = default;
        readonly EcsPoolInject<RMainMenuAction> mainMenuActionRequestPool = default;
        void MainMenuAction()
        {
            //Для каждого запроса действия в главном меню
            foreach (int requestEntity in mainMenuActionRequestFilter.Value)
            {
                //Берём запрос
                ref RMainMenuAction requestComp = ref mainMenuActionRequestPool.Value.Get(requestEntity);

                //Если запрашивается открытие окна игры
                if (requestComp.actionType == MainMenuActionType.OpenGame)
                {
                    //Создаём запрос создания новой игры
                    NewGameMenuStartNewGame();

                    //Открываем окно игры
                    GameOpenWindow();
                }

                mainMenuActionRequestPool.Value.Del(requestEntity);
            }
        }

        void MainMenuOpenWindow()
        {
            //Закрываем открытое главное окно
            CloseMainWindow();

            //Берём ссылку на окно главного меню
            UIMainMenuWindow mainMenuWindow = sOUI.Value.mainMenuWindow;

            //Делаем его активным и указываем как активное
            mainMenuWindow.gameObject.SetActive(true);
            sOUI.Value.activeMainWindow = sOUI.Value.mainMenuWindow;

            //Указываем, что активно окно главного меню
            sOUI.Value.activeMainWindowType = MainWindowType.MainMenu;

        }
        #endregion

        #region NewGame
        readonly EcsPoolInject<RStartNewGame> startNewGameRequestPool = default;
        void NewGameMenuStartNewGame()
        {
            //Создаём новую сущность и назначем ей запрос начала новой игры
            int requestEntity = world.Value.NewEntity();
            ref RStartNewGame requestComp = ref startNewGameRequestPool.Value.Add(requestEntity);

            //Запрашиваем включение группы систем "NewGame"
            EcsGroupSystemStateEvent("NewGame", true);
        }
        #endregion

        #region Game
        readonly EcsPoolInject<CNonDeletedUI> nonDeletedUIPool = default;

        void GameEventCheck()
        {
            //Проверяем запросы создания панелей
            GameCreatePanelRequest();

            //Проверяем запросы действий в игре
            GameAction();

            //Проверяем запросы действия в панели объекта
            ObjectPnAction();

            //Проверяем самозапросы обновления интерфейса объектов
            GameRefreshUISelfRequest();

            //Проверяем самозапросы обновления родителей панелей карты
            GameRefreshMapPanelsParentSelfRequest();

            //Проверяем запросы удаления панелей
            GameDeletePanelRequest();
        }

        readonly EcsFilterInject<Inc<RGameCreatePanel>> gameCreatePanelRequestFilter = default;
        readonly EcsPoolInject<RGameCreatePanel> gameCreatePanelRequestPool = default;
        void GameCreatePanelRequest()
        {
            //Для каждого запроса создания панели
            foreach (int requestEntity in gameCreatePanelRequestFilter.Value)
            {
                //Берём запрос
                ref RGameCreatePanel requestComp = ref gameCreatePanelRequestPool.Value.Get(requestEntity);

                //Если запрашивается создание обзорной панели региона вкладки регионов подпанели стратегической области
                if (requestComp.panelType == GamePanelType.RegionSummaryPanelSASbpnRegionsTab)
                {
                    //Создаём обзорную панель региона
                    StrategicAreaSbpnCreateRegionSummaryPanel(ref requestComp);
                }
                //Иначе, если запрашивается создание главной панели карты региона
                if (requestComp.panelType == GamePanelType.RegionMainMapPanel)
                {
                    //Создаём главную панель карты региона
                    MapUICreateRegionMainMapPanel(ref requestComp);
                }
                //Иначе, если запрашивается создание обзорной панели оперативной группы вкладки флотов менеджера флотов
                else if (requestComp.panelType == GamePanelType.TaskForceSummaryPanelFMSbpnFleetsTab)
                {
                    //Создаём обзорную панель группы
                    FMSbpnFleetsTabCreateTaskForceSummaryPanel(ref requestComp);
                }
                //Иначе, если запрашивается создание главной панели карты оперативной группы
                else if (requestComp.panelType == GamePanelType.TaskForceMainMapPanel)
                {
                    //Создаём главную панель карты
                    MapUICreateTaskForceMainMapPanel(ref requestComp);
                }

                gameCreatePanelRequestPool.Value.Del(requestEntity);
            }
        }

        readonly EcsFilterInject<Inc<SRGameRefreshPanels>> gameRefreshPanelsSelfRequestFilter = default;
        readonly EcsPoolInject<SRGameRefreshPanels> gameRefreshPanelsSelfRequestPool = default;
        void GameRefreshUISelfRequest()
        {
            //Обновляем интерфейс регионов
            GameRefreshUIRegion();

            //Обновляем интерфейс оперативных групп
            GameRefreshUITaskForce();

            //Для каждой сущности с самозапросом обновления панелей
            foreach (int entity in gameRefreshPanelsSelfRequestFilter.Value)
            {
                //Удаляем самозапрос 
                gameRefreshPanelsSelfRequestPool.Value.Del(entity);
            }
        }

        readonly EcsFilterInject<Inc<SRRefreshMapPanelsParent>> refreshMapPanelsParentSelfRequestFilter = default;
        readonly EcsPoolInject<SRRefreshMapPanelsParent> refreshMapPanelsParentSelfRequestPool = default;
        void GameRefreshMapPanelsParentSelfRequest()
        {
            //Обновляем панели оперативных групп
            GameRefreshTaskForceMapPanelsParent();

            //Для каждой сущности с самозапросом обновления родителя панелей карты
            foreach(int entity in refreshMapPanelsParentSelfRequestFilter.Value)
            {
                //Удаляем самозапрос
                refreshMapPanelsParentSelfRequestPool.Value.Del(entity);
            }
        }

        readonly EcsFilterInject<Inc<RGameDeletePanel>> gameDeletePanelRequestFilter = default;
        readonly EcsPoolInject<RGameDeletePanel> gameDeletePanelRequestPool = default;
        void GameDeletePanelRequest()
        {
            //Для каждого запроса удаления панели
            foreach (int requestEntity in gameDeletePanelRequestFilter.Value)
            {
                //Берём запрос
                ref RGameDeletePanel requestComp = ref gameDeletePanelRequestPool.Value.Get(requestEntity);

                //Если запрашивается удаление обзорной панели региона вкладки регионов подпанели стратегической области
                if(requestComp.panelType == GamePanelType.RegionSummaryPanelSASbpnRegionsTab)
                {
                    //Удаляем обзорную панель
                    StrategicAreaSbpnRegionsTabDeleteRegionSummaryPanel(requestComp.objectPE);
                }
                //Иначе, если запрашивается удаление главной панели карты региона
                else if (requestComp.panelType == GamePanelType.RegionMainMapPanel)
                {
                    //Удаляем панель карты
                    MapUIDeleteRegionMainMapPanel(requestComp.objectPE);
                }
                //Иначе, если запрашивается удаление обзорной панели оперативной группы вкладки флотов менеджера флотов
                else if (requestComp.panelType == GamePanelType.TaskForceSummaryPanelFMSbpnFleetsTab)
                {
                    //Удаляем обзорную панель
                    FMSbpnFleetsTabDeleteTaskForceSummaryPanel(requestComp.objectPE);
                }
                //Иначе, если запрашивается удаление главной панели карты оперативной группы
                else if(requestComp.panelType == GamePanelType.TaskForceMainMapPanel)
                {
                    //Удаляем панель карты
                    MapUIDeleteTaskForceMainMapPanel(requestComp.objectPE);
                }

                gameDeletePanelRequestPool.Value.Del(requestEntity);
            }
        }

        readonly EcsFilterInject<Inc<RGameAction>> gameActionRequestFilter = default;
        readonly EcsPoolInject<RGameAction> gameActionRequestPool = default;
        void GameAction()
        {
            //Для каждого запроса действия в игре
            foreach (int requestEntity in gameActionRequestFilter.Value)
            {
                //Берём запрос
                ref RGameAction requestComp = ref gameActionRequestPool.Value.Get(requestEntity);

                //Применяем состояние паузы
                if (requestComp.actionType == GameActionType.PauseOn || requestComp.actionType == GameActionType.PauseOff)
                {
                    GamePause(requestComp.actionType);
                }

                gameActionRequestPool.Value.Del(requestEntity);
            }
        }

        void GameOpenWindow()
        {
            //Закрываем открытое главное окно
            CloseMainWindow();

            //Берём ссылку на окно игры
            UIGameWindow gameWindow = sOUI.Value.gameWindow;

            //Делаем его активным и указываем как активное
            gameWindow.gameObject.SetActive(true);
            sOUI.Value.activeMainWindow = sOUI.Value.gameWindow;

            //Указываем, что активно окно игры
            sOUI.Value.activeMainWindowType = MainWindowType.Game;
        }

        void GamePause(
            GameActionType pauseMode)
        {
            //Если требуется включить паузу
            if (pauseMode == GameActionType.PauseOn)
            {
                //Указываем, что игра неактивна
                runtimeData.Value.isGameActive = false;
            }
            //Иначе
            else if (pauseMode == GameActionType.PauseOff)
            {
                //Указываем, что игра активна
                runtimeData.Value.isGameActive = true;
            }
        }

        readonly EcsFilterInject<Inc<CRegionCore, CRegionDisplayedGUIPanels, SRGameRefreshPanels>> regionRefreshGUIPanelsSelfRequestFilter = default;
        readonly EcsFilterInject<Inc<CRegionCore, CRegionDisplayedMapPanels, SRGameRefreshPanels>> regionRefreshMapPanelsSelfRequestFilter = default;
        void GameRefreshUIRegion()
        {
            //Для каждого региона с компонентом панелей GUI и самозапросом обновления панелей
            foreach(int regionEntity in regionRefreshGUIPanelsSelfRequestFilter.Value)
            {
                //Берём регион и компонент панелей
                ref CRegionCore rC = ref rCPool.Value.Get(regionEntity);
                ref CRegionEconomy rE = ref rEPool.Value.Get(regionEntity);
                ref CRegionDisplayedGUIPanels regionDisplayedGUIPanels = ref regionDisplayedGUIPanelsPool.Value.Get(regionEntity);

                //Если регион имеет отображаемую обзорную панель вкладки регионов подпанели стратегической области
                if(regionDisplayedGUIPanels.sASbpnRegionsTabSummaryPanel != null)
                {
                    //Обновляем её
                    regionDisplayedGUIPanels.sASbpnRegionsTabSummaryPanel.RefreshPanel(ref rC, ref rE);
                }
            }

            //Для каждого региона с компонентом панелей карты и самозапросом обновления панелей
            foreach (int regionEntity in regionRefreshMapPanelsSelfRequestFilter.Value)
            {
                //Берём регион и компонент панелей
                ref CRegionCore rC = ref rCPool.Value.Get(regionEntity);
                ref CRegionDisplayedMapPanels regionDisplayedMapPanels = ref regionDisplayedMapPanelsPool.Value.Get(regionEntity);

                //Если регион имеет отображаемую главную панель
                if (regionDisplayedMapPanels.mainMapPanel != null)
                {
                    //Обновляем её
                    regionDisplayedMapPanels.mainMapPanel.RefreshPanel(ref rC);
                }
            }
        }

        readonly EcsFilterInject<Inc<CTaskForce, CTaskForceDisplayedGUIPanels, SRGameRefreshPanels>> tFRefreshGUIPanelsSelfRequestFilter = default;
        readonly EcsFilterInject<Inc<CTaskForce, CTaskForceDisplayedMapPanels, SRGameRefreshPanels>> tFRefreshMapPanelsSelfRequestFilter = default;
        void GameRefreshUITaskForce()
        {
            //Для каждой оперативной группы с компонентом панелей GUI и самозапросом обновления панелей
            foreach (int tFEntity in tFRefreshGUIPanelsSelfRequestFilter.Value)
            {
                //Берём оперативную группу и компонент панелей
                ref CTaskForce tF = ref tFPool.Value.Get(tFEntity);
                ref CTaskForceDisplayedGUIPanels tFDisplayedGUIPanels = ref tFDisplayedGUIPanelsPool.Value.Get(tFEntity);

                //Если группа имеет отображаемую обзорную панель вкладки флотов менеджера флотов
                if (tFDisplayedGUIPanels.fMSbpnFleetsTabSummaryPanel != null)
                {
                    //Обновляем её
                    tFDisplayedGUIPanels.fMSbpnFleetsTabSummaryPanel.RefreshPanel(ref tF);
                }
            }

            //Для каждой группы с компонентом панелей карты и самозапросом обновления панелей
            foreach(int tFEntity in tFRefreshMapPanelsSelfRequestFilter.Value)
            {
                //Берём оперативную группу и компонент панелей
                ref CTaskForce tF = ref tFPool.Value.Get(tFEntity);
                ref CTaskForceDisplayedMapPanels tFDisplayedMapPanels = ref tFDisplayedMapPanelsPool.Value.Get(tFEntity);

                //Если группа имеет отображаемую главную панель карты
                if(tFDisplayedMapPanels.mainMapPanel != null)
                {
                    //Обновляем её
                    tFDisplayedMapPanels.mainMapPanel.RefreshPanel(ref tF);
                }
            }
        }

        readonly EcsFilterInject<Inc<CTaskForce, CTaskForceDisplayedMapPanels, SRRefreshMapPanelsParent>> tFRefreshMapPanelsParentSelfRequestFilter = default;
        void GameRefreshTaskForceMapPanelsParent()
        {
            //Для каждой оперативной группы с компонентом панелей карты и самозапросом обновления родителей
            foreach(int tFEntity in tFRefreshMapPanelsParentSelfRequestFilter.Value)
            {
                //Берём группу и компонент панелей карты
                ref CTaskForce tF = ref tFPool.Value.Get(tFEntity);
                ref CTaskForceDisplayedMapPanels tFDisplayedMapPanels = ref tFDisplayedMapPanelsPool.Value.Get(tFEntity);
                
                //Если группа имеет отображаемую главную панель карты
                if(tFDisplayedMapPanels.mainMapPanel != null)
                {
                    //Обновляем её родителя
                    MapUISetParentTaskForceMainMapPanel(ref tF, ref tFDisplayedMapPanels);
                }
            }
        }

        #region MapUI
        void MapUIMapPanelsUpdate()
        {
            //Для каждого региона с панелями карты
            foreach (int regionEntity in regionDisplayedMapPanelsFilter.Value)
            {
                //Берём компонент панелей карты
                ref CRegionDisplayedMapPanels regionDisplayedMapPanels = ref regionDisplayedMapPanelsPool.Value.Get(regionEntity);

                float d = Vector3.Dot(
                    Camera.main.transform.position.normalized,
                    regionDisplayedMapPanels.mapPanelGroup.transform.position.normalized);

                regionDisplayedMapPanels.mapPanelGroup.transform.LookAt(Vector3.zero, Vector3.up);
                d = Mathf.Clamp01(d);
                regionDisplayedMapPanels.mapPanelGroup.transform.rotation = Quaternion.Lerp(
                    regionDisplayedMapPanels.mapPanelGroup.transform.rotation,
                    Quaternion.LookRotation(
                        regionDisplayedMapPanels.mapPanelGroup.transform.position - Camera.main.transform.position,
                        Camera.main.transform.up),
                    d);
            }
        }

        void MapUICreateRegionDisplayedMapPanels(
            ref CRegionHexasphere rHS)
        {
            //Берём сущность региона
            rHS.selfPE.Unpack(world.Value, out int regionEntity);

            //Если регион не имеет компонента панелей карты
            if (regionDisplayedMapPanelsPool.Value.Has(regionEntity) == false)
            {
                //Назначаем региону компонент панелей карты
                ref CRegionDisplayedMapPanels regionDisplayedMapPanels = ref regionDisplayedMapPanelsPool.Value.Add(regionEntity);

                //Заполняем данные компонента
                regionDisplayedMapPanels = new(0);

                //Создаём новый объект группы панелей карты
                CRegionDisplayedMapPanels.InstantiateMapPanelGroup(
                    ref rHS, ref regionDisplayedMapPanels,
                    mapGenerationData.Value.hexasphereScale,
                    uIData.Value.mapPanelAltitude);
            }
        }

        void MapUIDeleteRegionDisplayedMapPanels(
            EcsPackedEntity regionPE)
        {
            //Берём сущность региона и компонент панелей карты
            regionPE.Unpack(world.Value, out int regionEntity);
            ref CRegionDisplayedMapPanels regionDisplayedMapPanels = ref regionDisplayedMapPanelsPool.Value.Get(regionEntity);

            //Если никакая из панелей не существует
            if (regionDisplayedMapPanels.IsEmpty == true)
            {
                //Кэшируем группу панелей
                CRegionDisplayedMapPanels.CacheMapPanelGroup(ref regionDisplayedMapPanels);

                //Удаляем с сущности региона компонент панелей карты
                regionDisplayedMapPanelsPool.Value.Del(regionEntity);
            }
        }

        void MapUICreateRegionMainMapPanel(
            ref RGameCreatePanel requestComp)
        {
            //Берём RC и компонент визуализации региона
            requestComp.objectPE.Unpack(world.Value, out int regionEntity);
            ref CRegionCore rC = ref rCPool.Value.Get(regionEntity);
            ref CRegionHexasphere rHS = ref rHSPool.Value.Get(regionEntity);

            //Создаём компонент панелей карты, если необходимо
            MapUICreateRegionDisplayedMapPanels(ref rHS);

            //Берём компонент панелей карты
            ref CRegionDisplayedMapPanels regionDisplayedMapPanels = ref regionDisplayedMapPanelsPool.Value.Get(regionEntity);

            //Создаём главную панель карты
            UIRCMainMapPanel.InstantiatePanel(
                ref rC, ref regionDisplayedMapPanels);
        }

        void MapUIDeleteRegionMainMapPanel(
            EcsPackedEntity regionPE)
        {
            //Берём компонент панелей карты
            regionPE.Unpack(world.Value, out int regionEntity);
            ref CRegionDisplayedMapPanels regionDisplayedMapPanels = ref regionDisplayedMapPanelsPool.Value.Get(regionEntity);

            //Кэшируем главную панель карты
            UIRCMainMapPanel.CachePanel(ref regionDisplayedMapPanels);

            //Удаляем компонент панелей карты, если необходимо
            MapUIDeleteRegionDisplayedMapPanels(regionPE);
        }

        void MapUICreateTaskForceDisplayedMapPanels(
            ref CTaskForce tF)
        {
            //Берём сущность оперативной группы
            tF.selfPE.Unpack(world.Value, out int tFEntity);

            //Если группа не имеет компонента панелей карты
            if (tFDisplayedMapPanelsPool.Value.Has(tFEntity) == false)
            {
                //Назначаем группе компонент панелей карты
                ref CTaskForceDisplayedMapPanels tFDisplayedMapPanels = ref tFDisplayedMapPanelsPool.Value.Add(tFEntity);
            }
        }

        void MapUIDeleteTaskForceDisplayedMapPanels(
            EcsPackedEntity tFPE)
        {
            //Берём сущность оперативной группы и компонент панелей карты
            tFPE.Unpack(world.Value, out int tFEntity);
            ref CTaskForceDisplayedMapPanels tFDisplayedMapPanels = ref tFDisplayedMapPanelsPool.Value.Get(tFEntity);

            //Если никакая из панелей не существует
            if (tFDisplayedMapPanels.mainMapPanel == null)
            {
                //Удаляем с сущности группы компонент панелей карты
                tFDisplayedMapPanelsPool.Value.Del(tFEntity);
            }
        }

        void MapUICreateTaskForceMainMapPanel(
            ref RGameCreatePanel requestComp)
        {
            //Берём оперативную группу
            requestComp.objectPE.Unpack(world.Value, out int tFEntity);
            ref CTaskForce tF = ref tFPool.Value.Get(tFEntity);

            //Создаём компонент панелей карты, если необходимо
            MapUICreateTaskForceDisplayedMapPanels(ref tF);

            //Берём компонент панелей карты
            ref CTaskForceDisplayedMapPanels tFDisplayedMapPanels = ref tFDisplayedMapPanelsPool.Value.Get(tFEntity);

            //Создаём главную панель карты
            UITFMainMapPanel.InstantiatePanel(
                ref tF, ref tFDisplayedMapPanels);

            //Прикрепляем панель к текущему региону группы
            MapUISetParentTaskForceMainMapPanel(ref tF, ref tFDisplayedMapPanels);
        }

        void MapUISetParentTaskForceMainMapPanel(
            ref CTaskForce tF, ref CTaskForceDisplayedMapPanels tFDisplayedMapPanels)
        {
            //Если предыдущий регион оперативной группы существует
            if(tF.previousRegionPE.Unpack(world.Value, out int previousRegionEntity))
            {
                //Берём компонент панелей карты предыдущего региона 
                ref CRegionDisplayedMapPanels previousRegionDisplayedMapPanels = ref regionDisplayedMapPanelsPool.Value.Get(previousRegionEntity);

                //Открепляем панель группы от него
                previousRegionDisplayedMapPanels.CancelParentTaskForceMainMapPanel(tFDisplayedMapPanels.mainMapPanel);
            }

            //Обновляем текущий регион в компоненте панелей карты группы
            tFDisplayedMapPanels.currentRegionPE = tF.currentRegionPE;

            //Берём текущий регион группы
            tF.currentRegionPE.Unpack(world.Value, out int currentRegionEntity);
            ref CRegionHexasphere rHS = ref rHSPool.Value.Get(currentRegionEntity);

            //Создаём компонент панелей карты, если необходимо
            MapUICreateRegionDisplayedMapPanels(ref rHS);

            //Берём компонент панелей карты
            ref CRegionDisplayedMapPanels currentRegionDisplayedMapPanels = ref regionDisplayedMapPanelsPool.Value.Get(currentRegionEntity);

            //Прикрепляем к нему главную панель карты группы
            currentRegionDisplayedMapPanels.SetParentTaskForceMainMapPanel(tFDisplayedMapPanels.mainMapPanel);
        }

        void MapUIDeleteTaskForceMainMapPanel(
            EcsPackedEntity tFPE)
        {
            //Берём компонент панелей карты
            tFPE.Unpack(world.Value, out int tFEntity);
            ref CTaskForceDisplayedMapPanels tFDisplayedMapPanels = ref tFDisplayedMapPanelsPool.Value.Get(tFEntity);

            //Берём компонент панелей карты текущего региона группы
            tFDisplayedMapPanels.currentRegionPE.Unpack(world.Value, out int currentRegionEntity);
            ref CRegionDisplayedMapPanels regionDisplayedMapPanels = ref regionDisplayedMapPanelsPool.Value.Get(currentRegionEntity);

            //Открепляем панель группы от него
            regionDisplayedMapPanels.CancelParentTaskForceMainMapPanel(tFDisplayedMapPanels.mainMapPanel);

            //Кэшируем главную панель карты
            UITFMainMapPanel.CachePanel(ref tFDisplayedMapPanels);

            //Удаляем компонент панелей карты с группы, если необходимо
            MapUIDeleteTaskForceDisplayedMapPanels(tFPE);

            //Удаляем компонент панелей карты с региона, если необходимо
            MapUIDeleteRegionDisplayedMapPanels(tFDisplayedMapPanels.currentRegionPE);
        }

        void ParentAndAlignToRegion(
            GameObject go,
            ref CRegionHexasphere rHS,
            float altitude = 0)
        {
            //Берём центр региона
            Vector3 regionCenter = rHS.GetRegionCenter() * mapGenerationData.Value.hexasphereScale;

            //Если высота не равна нулю
            if(altitude != 0)
            {
                Vector3 direction = regionCenter.normalized * altitude;
                go.transform.position = regionCenter + direction;
            }
            //Иначе
            else
            {
                go.transform.position = regionCenter;
            }

            //Привязываем объект к региону
            go.transform.SetParent(rHS.selfObject.transform, true);
            go.transform.LookAt(rHS.selfObject.transform.position);
        }
        #endregion

        #region GUI
        void GUIOpenPanel(
            GameObject currentPanel, MainPanelType currentPanelType,
            out bool isSamePanel)
        {
            //Значение по умолчанию отрицательно
            isSamePanel = false;

            //Берём окно игры
            UIGameWindow gameWindow = sOUI.Value.gameWindow;

            //Если открыта необходимая панель
            if(gameWindow.activeMainPanelType == currentPanelType)
            {
                //Сообщаем, что была открыта та же панель
                isSamePanel = true;
            }

            //Если была открыта та же панель
            if (isSamePanel == true)
            {

            }
            //Иначе
            else
            {
                //Отображаем запрошенную панель

                //Если какая-либо панель была активна
                if (gameWindow.activeMainPanelType != MainPanelType.None)
                {
                    //Скрываем её
                    gameWindow.activeMainPanel.SetActive(false);
                }

                //Делаем запрошенную панель активной
                currentPanel.SetActive(true);

                //Указываем её как активную панель
                gameWindow.activeMainPanelType = currentPanelType;
                gameWindow.activeMainPanel = currentPanel;
            }
        }

        readonly EcsFilterInject<Inc<CRegionCore, CNonDeletedUI>> regionNonDeletedUIFilter = default;
        readonly EcsFilterInject<Inc<CRegionDisplayedGUIPanels, CNonDeletedUI>> regionDisplayedNonDeletedGUIFilter = default;
        readonly EcsFilterInject<Inc<CRegionDisplayedGUIPanels>, Exc<CNonDeletedUI>> regionDisplayedDeletedGUIFilter = default;
        void GUICreateRegionGUIPanels(
            ref CRegionCore rC)
        {
            //Берём сущность региона
            rC.selfPE.Unpack(world.Value, out int regionEntity);
            
            //Если регион не имеет компонента панелей GUI
            if(regionDisplayedGUIPanelsPool.Value.Has(regionEntity) == false)
            {
                //Назначаем ему компонент панелей GUI
                ref CRegionDisplayedGUIPanels regionDisplayedGUIPanels = ref regionDisplayedGUIPanelsPool.Value.Add(regionEntity);
            }
        }

        void GUIDeleteRegionGUIPanels(
            EcsPackedEntity regionPE)
        {
            //Берём сущность региона и компонент панелей GUI
            regionPE.Unpack(world.Value, out int regionEntity);
            ref CRegionDisplayedGUIPanels regionDisplayedGUIPanels = ref regionDisplayedGUIPanelsPool.Value.Get(regionEntity);

            //Если никакая из панелей не существует
            if(regionDisplayedGUIPanels.sASbpnRegionsTabSummaryPanel == null)
            {
                //Удаляем с сущности региона компонент панелей GUI
                regionDisplayedGUIPanelsPool.Value.Del(regionEntity);
            }
        }

        readonly EcsFilterInject<Inc<CTaskForce, CNonDeletedUI>> taskForceNonDeletedUIFilter = default;
        readonly EcsFilterInject<Inc<CTaskForceDisplayedGUIPanels, CNonDeletedUI>> taskForceDisplayedNonDeletedGUIFilter = default;
        readonly EcsFilterInject<Inc<CTaskForceDisplayedGUIPanels>, Exc<CNonDeletedUI>> taskForceDisplayedDeletedGUIFilter = default;
        void GUICreateTaskForceGUIPanels(
            ref CTaskForce tF)
        {
            //Берём сущность группы
            tF.selfPE.Unpack(world.Value, out int tFEntity);

            //Если группа не имеет компонента панелей GUI
            if (tFDisplayedGUIPanelsPool.Value.Has(tFEntity) == false)
            {
                //Назначаем ей компонент панелей GUI
                ref CTaskForceDisplayedGUIPanels tFDisplayedGUIPanels = ref tFDisplayedGUIPanelsPool.Value.Add(tFEntity);
            }
        }

        void GUIDeleteTaskForceGUIPanels(
            EcsPackedEntity tFPE)
        {
            //Берём сущность оперативной группы и компонент панелей GUI
            tFPE.Unpack(world.Value, out int tFEntity);
            ref CTaskForceDisplayedGUIPanels tFDisplayedGUIPanels = ref tFDisplayedGUIPanelsPool.Value.Get(tFEntity);

            //Если никакая из панелей не существует
            if (tFDisplayedGUIPanels.fMSbpnFleetsTabSummaryPanel == null)
            {
                //Удаляем с сущности группы компонент панелей GUI
                tFDisplayedGUIPanelsPool.Value.Del(tFEntity);
            }
        }

        #region ObjectPanel
        readonly EcsFilterInject<Inc<RGameObjectPanelAction>> gameObjectPanelRequestFilter = default;
        readonly EcsPoolInject<RGameObjectPanelAction> gameObjectPanelRequestPool = default;
        void ObjectPnAction()
        {
            //Для каждого запроса действия в игре
            foreach (int requestEntity in gameObjectPanelRequestFilter.Value)
            {
                //Берём запрос
                ref RGameObjectPanelAction requestComp = ref gameObjectPanelRequestPool.Value.Get(requestEntity);

                //Если запрашивается закрытие панели объекта
                if (requestComp.requestType == ObjectPanelActionRequestType.Close)
                {
                    //Закрываем панель объекта
                    ObjectPnClose();
                }

                //Иначе, если запрашивается отображение вкладок персонажа
                else if (requestComp.requestType >= ObjectPanelActionRequestType.CharacterOverview
                    && requestComp.requestType <= ObjectPanelActionRequestType.CharacterOverview)
                {
                    //Берём персонажа
                    requestComp.objectPE.Unpack(world.Value, out int characterEntity);
                    ref CCharacter character = ref characterPool.Value.Get(characterEntity);

                    //Если запрашивается отображение обзорной вкладки
                    if (requestComp.requestType == ObjectPanelActionRequestType.CharacterOverview)
                    {
                        //Отображаем обзорную вкладку персонажа
                        CharacterSbpnShowOverviewTab(ref character);
                    }
                }
                //Иначе, если запрашивается отображение вкладок региона
                else if (requestComp.requestType >= ObjectPanelActionRequestType.RegionOverview
                    && requestComp.requestType <= ObjectPanelActionRequestType.RegionOverview)
                {
                    //Берём регион
                    requestComp.objectPE.Unpack(world.Value, out int regionEntity);
                    ref CRegionHexasphere rHS = ref rHSPool.Value.Get(regionEntity);
                    ref CRegionCore rC = ref rCPool.Value.Get(regionEntity);

                    //Если запрашивается отображение обзорной вкладки
                    if (requestComp.requestType == ObjectPanelActionRequestType.RegionOverview)
                    {
                        //Отображаем обзорную вкладку региона
                        RegionSbpnShowOverviewTab(ref rHS, ref rC);
                    }
                }
                //Иначе, если запрашивается отображение вкладок стратегической области
                else if (requestComp.requestType >= ObjectPanelActionRequestType.StrategicAreaOverview
                    && requestComp.requestType <= ObjectPanelActionRequestType.StrategicAreaRegions)
                {
                    //Берём область
                    requestComp.objectPE.Unpack(world.Value, out int sAEntity);
                    ref CStrategicArea sA = ref sAPool.Value.Get(sAEntity);

                    //Если запрашивается отображение обзорной вкладки
                    if (requestComp.requestType == ObjectPanelActionRequestType.StrategicAreaOverview)
                    {
                        //Отображаем обзорную вкладку области
                        StrategicAreaSbpnShowOverviewTab(ref sA);
                    }
                    //Иначе, если запрашивается отображение вкладки регионов
                    else if (requestComp.requestType == ObjectPanelActionRequestType.StrategicAreaRegions)
                    {
                        //Отображаем вкладку регионов области
                        StrategicAreaSbpnShowRegionsTab(ref sA, requestComp.secondObjectPE);
                    }
                }
                //Иначе, если запрашивается отображение вкладок менеджера флотов
                else if(requestComp.requestType >= ObjectPanelActionRequestType.FleetManagerFleets
                    && requestComp.requestType <= ObjectPanelActionRequestType.FleetManagerFleets)
                {
                    //Берём персонажа
                    requestComp.objectPE.Unpack(world.Value, out int characterEntity);
                    ref CCharacter character = ref characterPool.Value.Get(characterEntity);

                    //Если запрашивается отображение вкладки флотов
                    if (requestComp.requestType == ObjectPanelActionRequestType.FleetManagerFleets)
                    {
                        //Отображаем вкладку флотов
                        FleetManagerSbpnShowFleetsTab(ref character);
                    }
                }

                gameObjectPanelRequestPool.Value.Del(requestEntity);
            }
        }

        void ObjectPnClose()
        {
            //Берём панель объекта
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //Если активна подпанель персонажа
            if (objectPanel.activeSubpanelType == ObjectSubpanelType.Character)
            {
                //Скрываем активную вкладку
                objectPanel.characterSubpanel.HideActiveTab();
            }
            //Иначе, если активна подпанель региона
            else if (objectPanel.activeSubpanelType == ObjectSubpanelType.Region)
            {
                //Скрываем активную вкладку
                objectPanel.regionSubpanel.HideActiveTab();
            }
            //Иначе, если активна подпанель менеджера флотов
            else if (objectPanel.activeSubpanelType == ObjectSubpanelType.FleetManager)
            {
                //Скрываем активную вкладку
                objectPanel.fleetManagerSubpanel.HideActiveTab();
            }

            //Скрываем активную подпанель объекта
            objectPanel.HideActiveSubpanel();

            //Скрываем активную главную панель
            sOUI.Value.gameWindow.HideActivePanel();
        }

        void ObjectPnOpenSubpanel(
            UIAObjectSubpanel currentSubpanel, ObjectSubpanelType currentSubpanelType,
            out bool isSamePanel,
            out bool isSameSubpanel)
        {
            //Значение по умолчанию отрицательно
            isSameSubpanel = false;

            //Берём панель объекта
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //Отображаем панель объекта, если необходимо
            GUIOpenPanel(
                objectPanel.gameObject, MainPanelType.Object,
                out isSamePanel);

            //Если открыта необходимая подпанель
            if(objectPanel.activeSubpanelType == currentSubpanelType)
            {
                //Если была открыта та же панель
                if(isSamePanel == true)
                {
                    //Сообщаем, что была открыта та же подпанель
                    isSameSubpanel = true;
                }
            }

            //Если была открыта та же панель
            if (isSamePanel == true)
            {

            }
            //Иначе
            else
            {

            }

            //Если была открыта та же подпанель 
            if (isSameSubpanel == true)
            {

            }
            //Иначе
            else
            {
                //Отображаем запрошенную подпанель

                //Если какая-либо подпанель была активна, скрываем её
                if (objectPanel.activeSubpanelType != ObjectSubpanelType.None)
                {
                    objectPanel.activeSubpanel.gameObject.SetActive(false);
                }

                //Делаем запрошенную подпанель активной
                currentSubpanel.gameObject.SetActive(true);

                //Указываем её как активную подпанель
                objectPanel.activeSubpanelType = currentSubpanelType;
                objectPanel.activeSubpanel = currentSubpanel;

                //Устанавливаем ширину панели заголовка соответственно запрошенной подпанели
                objectPanel.titlePanel.offsetMax = new Vector2(
                    currentSubpanel.parentRect.offsetMax.x, objectPanel.titlePanel.offsetMax.y);
            }
        }

        #region CharacterSubpanel
        void CharacterSbpnShowTab(
            UIASubpanelTab currentCharacterTab,
            ref CCharacter currentCharacter,
            out bool isSamePanel,
            out bool isSameSubpanel,
            out bool isSameTab,
            out bool isSameObject)
        {
            //Значения по умолчанию отрицательны
            isSameTab = false;
            isSameObject = false;

            //Берём панель объекта
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //Берём подпанель персонажа
            UICharacterSubpanel characterSubpanel = objectPanel.characterSubpanel;

            //Отображаем подпанель персонажа, если необходимо
            ObjectPnOpenSubpanel(
                characterSubpanel, ObjectSubpanelType.Character,
                out isSamePanel,
                out isSameSubpanel);

            //Если открыта необходимая вкладка
            if (characterSubpanel.activeTab == currentCharacterTab)
            {
                //Если была открыта та же подпанель
                if(isSameSubpanel == true)
                {
                    //Сообщаем, что была открыта та же вкладка
                    isSameTab = true;

                    //Если вкладка была открыта для того же персонажа
                    if (characterSubpanel.activeTab.objectPE.EqualsTo(currentCharacter.selfPE) == true)
                    {
                        //То сообщаем, что был отображён тот же объект
                        isSameObject = true;
                    }
                }
            }

            //Если была открыта та же панель
            if (isSamePanel == true)
            {

            }
            //Иначе
            else
            {

            }

            //Если была открыта та же подпанель 
            if (isSameSubpanel == true)
            {

            }
            //Иначе
            else
            {

            }

            //Если была открыта та же вкладка
            if (isSameTab == true)
            {

            }
            //Иначе
            else
            {
                //Отображаем запрошенную вкладку
                characterSubpanel.tabGroup.OnTabSelected(currentCharacterTab.selfTabButton);

                //Указываем её как активную вкладку
                characterSubpanel.activeTab = currentCharacterTab;
            }

            //Если был отображён тот же объект
            if (isSameObject == true)
            {

            }
            //Иначе
            else
            {
                //Указываем PE текущего персонажа
                characterSubpanel.activeTab.objectPE = currentCharacter.selfPE;

                //Отображаем название панели - название персонажа
                objectPanel.objectName.text = currentCharacter.selfIndex.ToString();
            }
        }

        void CharacterSbpnShowOverviewTab(
            ref CCharacter character)
        {
            //Берём подпанель персонажа
            UICharacterSubpanel characterSubpanel = sOUI.Value.gameWindow.objectPanel.characterSubpanel;

            //Берём обзорную вкладку
            Game.GUI.Object.Character.UIOverviewTab overviewTab = characterSubpanel.overviewTab;

            //Отображаем обзорную вкладку
            CharacterSbpnShowTab(
                overviewTab,
                ref character,
                out bool isSamePanel,
                out bool isSameSubpanel,
                out bool isSameTab,
                out bool isSameObject);
        }
        #endregion

        #region RegionSubpanel
        void RegionSbpnShowTab(
            UIASubpanelTab currentRegionTab,
            ref CRegionCore currentRC,
            out bool isSamePanel,
            out bool isSameSubpanel,
            out bool isSameTab,
            out bool isSameObject)
        {
            //Значения по умолчанию отрицательны
            isSameTab = false;
            isSameObject = false;

            //Берём панель объекта
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //Берём подпанель региона
            UIRegionSubpanel regionSubpanel = objectPanel.regionSubpanel;

            //Отображаем подпанель региона, если необходимо
            ObjectPnOpenSubpanel(
                regionSubpanel, ObjectSubpanelType.Region,
                out isSamePanel,
                out isSameSubpanel);

            //Если открыта необходимая вкладка
            if (regionSubpanel.activeTab == currentRegionTab)
            {
                //Если была открыта та же подпанель
                if (isSameSubpanel == true)
                {
                    //Сообщаем, что была открыта та же вкладка
                    isSameTab = true;

                    //Если вкладка была открыта для того же региона
                    if (regionSubpanel.activeTab.objectPE.EqualsTo(currentRC.selfPE) == true)
                    {
                        //То сообщаем, что был отображён тот же объект
                        isSameObject = true;
                    }
                }
            }

            //Если была открыта та же панель
            if (isSamePanel == true)
            {

            }
            //Иначе
            else
            {

            }

            //Если была открыта та же подпанель 
            if (isSameSubpanel == true)
            {

            }
            //Иначе
            else
            {

            }

            //Если была открыта та же вкладка
            if (isSameTab == true)
            {

            }
            //Иначе
            else
            {
                //Отображаем запрошенную вкладку
                regionSubpanel.tabGroup.OnTabSelected(currentRegionTab.selfTabButton);

                //Указываем её как активную вкладку
                regionSubpanel.activeTab = currentRegionTab;
            }

            //Если был отображён тот же объект
            if (isSameObject == true)
            {

            }
            //Иначе
            else
            {
                //Указываем PE текущего региона
                regionSubpanel.activeTab.objectPE = currentRC.selfPE;

                //Отображаем название панели - название региона
                objectPanel.objectName.text = currentRC.Index.ToString();
            }
        }

        void RegionSbpnShowOverviewTab(
            ref CRegionHexasphere rHS, ref CRegionCore rC)
        {
            //Берём подпанель региона
            UIRegionSubpanel regionSubpanel = sOUI.Value.gameWindow.objectPanel.regionSubpanel;

            //Берём обзорную вкладку
            Game.GUI.Object.Region.UIOverviewTab overviewTab = regionSubpanel.overviewTab;

            //Отображаем обзорную вкладку
            RegionSbpnShowTab(
                overviewTab,
                ref rC,
                out bool isSamePanel,
                out bool isSameSubpanel,
                out bool isSameTab,
                out bool isSameObject);
        }
        #endregion

        #region StrategicAreaSubpanel
        void StrategicAreaSbpnShowTab(
            UIASubpanelTab currentSATab,
            ref CStrategicArea currentSA,
            out bool isSamePanel,
            out bool isSameSubpanel,
            out bool isSameTab,
            out bool isSameObject)
        {
            //Значения по умолчанию отрицательны
            isSameTab = false;
            isSameObject = false;

            //Берём панель объекта
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //Берём подпанель стратегической области
            UIStrategicAreaSubpanel sASubpanel = objectPanel.strategicAreaSubpanel;

            //Отображаем подпанель области, если необходимо
            ObjectPnOpenSubpanel(
                sASubpanel, ObjectSubpanelType.StrategicArea,
                out isSamePanel,
                out isSameSubpanel);

            //Если открыта необходимая вкладка
            if (sASubpanel.activeTab == currentSATab)
            {
                //Если была открыта та же подпанель
                if (isSameSubpanel == true)
                {
                    //Сообщаем, что была открыта та же вкладка
                    isSameTab = true;

                    //Если вкладка была открыта для той же области
                    if (sASubpanel.activeTab.objectPE.EqualsTo(currentSA.selfPE) == true)
                    {
                        //То сообщаем, что был отображён тот же объект
                        isSameObject = true;
                    }
                }
            }

            //Если была открыта та же панель
            if (isSamePanel == true)
            {

            }
            //Иначе
            else
            {

            }

            //Если была открыта та же подпанель 
            if (isSameSubpanel == true)
            {

            }
            //Иначе
            else
            {

            }

            //Если была открыта та же вкладка
            if (isSameTab == true)
            {

            }
            //Иначе
            else
            {
                //Отображаем запрошенную вкладку
                sASubpanel.tabGroup.OnTabSelected(currentSATab.selfTabButton);

                //Указываем её как активную вкладку
                sASubpanel.activeTab = currentSATab;
            }

            //Если был отображён тот же объект
            if (isSameObject == true)
            {

            }
            //Иначе
            else
            {
                //Указываем PE текущей области
                sASubpanel.activeTab.objectPE = currentSA.selfPE;

                //Отображаем название панели - название области
                objectPanel.objectName.text = currentSA.selfPE.Id.ToString();
            }
        }

        void StrategicAreaSbpnShowOverviewTab(
            ref CStrategicArea sA)
        {
            //Берём подпанель стратегической области
            UIStrategicAreaSubpanel sASubpanel = sOUI.Value.gameWindow.objectPanel.strategicAreaSubpanel;

            //Берём обзорную вкладку
            Game.GUI.Object.StrategicArea.UIOverviewTab overviewTab = sASubpanel.overviewTab;

            //Отображаем обзорную вкладку
            StrategicAreaSbpnShowTab(
                overviewTab,
                ref sA,
                out bool isSamePanel,
                out bool isSameSubpanel,
                out bool isSameTab,
                out bool isSameObject);
        }

        void StrategicAreaSbpnShowRegionsTab(
            ref CStrategicArea sA, EcsPackedEntity currentRCPE = new())
        {
            //Берём подпанель стратегической области
            UIStrategicAreaSubpanel sASubpanel = sOUI.Value.gameWindow.objectPanel.strategicAreaSubpanel;

            //Берём вкладку регионов
            Game.GUI.Object.StrategicArea.UIRegionsTab regionsTab = sASubpanel.regionsTab;

            //Отображаем вкладку регионов
            StrategicAreaSbpnShowTab(
                regionsTab,
                ref sA,
                out bool isSamePanel,
                out bool isSameSubpanel,
                out bool isSameTab,
                out bool isSameObject);

            //Если была открыта та же панель
            if (isSamePanel == true)
            {

            }
            //Иначе
            else
            {

            }

            //Если была открыта та же подпанель 
            if (isSameSubpanel == true)
            {

            }
            //Иначе
            else
            {

            }

            //Если была открыта та же вкладка
            if (isSameTab == true)
            {

            }
            //Иначе
            else
            {

            }

            //Если был отображён тот же объект
            if (isSameObject == true)
            {

            }
            //Иначе
            else
            {
                //Проверяем, какие регионы должны быть отображены в этом списке
                //Для каждого региона области
                for(int a = 0; a < sA.regionPEs.Length; a++)
                {
                    //Берём сущность региона и назначаем компонент "неудаляемый UI"
                    sA.regionPEs[a].Unpack(world.Value, out int regionEntity);
                    ref CNonDeletedUI regionNonDeletedUI = ref nonDeletedUIPool.Value.Add(regionEntity);
                }

                //Удаляем обзорную панель вкладки регионов с каждого региона, которого не должно быть в списке
                //Для каждого региона с отображаемыми панелями GUI, но без "неудаляемого UI"
                foreach(int regionEntity in regionDisplayedDeletedGUIFilter.Value)
                {
                    //Берём компонент панелей GUI
                    ref CRegionDisplayedGUIPanels regionDisplayedGUIPanels = ref regionDisplayedGUIPanelsPool.Value.Get(regionEntity);

                    //Если у региона есть обзорная панель вкладки регионов
                    if(regionDisplayedGUIPanels.sASbpnRegionsTabSummaryPanel != null)
                    {
                        //Удаляем обзорную панель
                        StrategicAreaSbpnRegionsTabDeleteRegionSummaryPanel(regionDisplayedGUIPanels.sASbpnRegionsTabSummaryPanel.selfPE);
                    }
                }

                //Обновляем панели регионов, которые уже имеют обзорные панели
                //Для каждого региона с панелями GUI и "неудаляемым UI"
                foreach(int regionEntity in regionDisplayedNonDeletedGUIFilter.Value)
                {
                    //Берём регион и компонент панелей GUI
                    ref CRegionCore rC = ref rCPool.Value.Get(regionEntity);
                    ref CRegionEconomy rE = ref rEPool.Value.Get(regionEntity);
                    ref CRegionDisplayedGUIPanels regionDisplayedGUIPanels = ref regionDisplayedGUIPanelsPool.Value.Get(regionEntity);

                    //Если у региона есть обзорная панель вкладки регионов
                    if(regionDisplayedGUIPanels.sASbpnRegionsTabSummaryPanel != null)
                    {
                        //Обновляем её данные
                        regionDisplayedGUIPanels.sASbpnRegionsTabSummaryPanel.RefreshPanel(ref rC, ref rE);

                        //И удаляем с региона "неудаляемый UI"
                        nonDeletedUIPool.Value.Del(regionEntity);
                    }
                    //Иначе панель должна создаваться, но это оставляем на следующий цикл, в котором панели создаются
                }

                //Создаём обзорные панели для регионов, которые не имеют их, но должны
                //Для каждого региона с "неудаляемым UI"
                foreach(int regionEntity in regionNonDeletedUIFilter.Value)
                {
                    //Берём регион
                    ref CRegionCore rC = ref rCPool.Value.Get(regionEntity);
                    ref CRegionEconomy rE = ref rEPool.Value.Get(regionEntity);

                    //Создаём обзорную панель
                    StrategicAreaSbpnRegionsTabCreateRegionSummaryPanel(ref rC, ref rE);

                    //И удаляем с региона "неудаляемый UI"
                    nonDeletedUIPool.Value.Del(regionEntity);
                }
            }

            //Если PE текущего региона не пуста
            if (currentRCPE.Unpack(world.Value, out int currentRegionEntity))
            {
                //Берём сущность текущего региона и компонент панелей GUI
                ref CRegionDisplayedGUIPanels currentRegionDisplayedGUIPanels = ref regionDisplayedGUIPanelsPool.Value.Get(currentRegionEntity);

                //Центрируем список на текущем регионе
                regionsTab.scrollView.FocusOnItem(currentRegionDisplayedGUIPanels.sASbpnRegionsTabSummaryPanel.selfRect);
            }
        }

        void StrategicAreaSbpnCreateRegionSummaryPanel(
            ref RGameCreatePanel requestComp)
        {
            //Берём регион
            requestComp.objectPE.Unpack(world.Value, out int regionEntity);
            ref CRegionCore rC = ref rCPool.Value.Get(regionEntity);
            ref CRegionEconomy rE = ref rEPool.Value.Get(regionEntity);

            //Создаём обзорную панель
            StrategicAreaSbpnRegionsTabCreateRegionSummaryPanel(ref rC, ref rE);
        }

        void StrategicAreaSbpnRegionsTabCreateRegionSummaryPanel(
            ref CRegionCore rC, ref CRegionEconomy rE)
        {
            //Берём сущность региона
            rC.selfPE.Unpack(world.Value, out int regionEntity);

            //Создаём компонент панелей GUI, если необходимо
            GUICreateRegionGUIPanels(ref rC);

            //Берём компонент панелей GUI
            ref CRegionDisplayedGUIPanels regionDisplayedGUIPanels = ref regionDisplayedGUIPanelsPool.Value.Get(regionEntity);

            //Берём вкладку регионов
            UIRegionsTab regionsTab = sOUI.Value.gameWindow.objectPanel.strategicAreaSubpanel.regionsTab;

            //Создаём обзорную панель вкладки регионов
            Game.GUI.Object.StrategicArea.Regions.UIRegionSummaryPanel.InstantiatePanel(
                ref rC, ref rE, ref regionDisplayedGUIPanels,
                regionsTab.layoutGroup);
        }

        void StrategicAreaSbpnRegionsTabDeleteRegionSummaryPanel(
            EcsPackedEntity regionPE)
        {
            //Берём регион и компонент панелей GUI
            regionPE.Unpack(world.Value, out int regionEntity);
            ref CRegionDisplayedGUIPanels regionDisplayedGUIPanels = ref regionDisplayedGUIPanelsPool.Value.Get(regionEntity);

            //Кэшируем обзорную панель вкладки регионов
            Game.GUI.Object.StrategicArea.Regions.UIRegionSummaryPanel.CachePanel(ref regionDisplayedGUIPanels);

            //Удаляем компонент панелей GUI, если необходимо
            GUIDeleteRegionGUIPanels(regionPE);
        }
        #endregion

        #region FleetManager
        void FleetManagerSbpnShowTab(
            UIASubpanelTab currentFleetManagerTab,
            ref CCharacter currentCharacter,
            out bool isSamePanel,
            out bool isSameSubpanel,
            out bool isSameTab,
            out bool isSameObject)
        {
            //Значения по умолчанию отрицательны
            isSameTab = false;
            isSameObject = false;

            //Берём панель объекта
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //Берём подпанель менеджера флотов
            UIFleetManagerSubpanel fleetManagerSubpanel = objectPanel.fleetManagerSubpanel;

            //Отображаем подпанель менеджера флотов, если необходимо
            ObjectPnOpenSubpanel(
                fleetManagerSubpanel, ObjectSubpanelType.FleetManager,
                out isSamePanel,
                out isSameSubpanel);

            //Если открыта необходимая вкладка
            if(fleetManagerSubpanel.activeTab == currentFleetManagerTab)
            {
                //Если была открыта та же подпанель
                if (isSameSubpanel == true)
                {
                    //Сообщаем, что была открыта та же вкладка
                    isSameTab = true;

                    //Если вкладка была открыта для того же персонажа
                    if (fleetManagerSubpanel.activeTab.objectPE.EqualsTo(currentCharacter.selfPE) == true)
                    {
                        //То сообщаем, что был отображён тот же объект
                        isSameObject = true;
                    }
                }
            }

            //Если была открыта та же панель
            if (isSamePanel == true)
            {
                
            }
            //Иначе
            else
            {

            }

            //Если была открыта та же подпанель 
            if (isSameSubpanel == true)
            {
                
            }
            //Иначе
            else
            {
                //Отображаем название панели - менеджер флотов
                objectPanel.objectName.text = "FleetManager";
            }

            //Если была открыта та же вкладка
            if (isSameTab == true)
            {
                
            }
            //Иначе
            else
            {
                //Отображаем запрошенную вкладку
                fleetManagerSubpanel.tabGroup.OnTabSelected(currentFleetManagerTab.selfTabButton);

                //Указываем её как активную вкладку
                fleetManagerSubpanel.activeTab = currentFleetManagerTab;
            }

            //Если был отображён тот же объект
            if (isSameObject == true)
            {

            }
            //Иначе
            else
            {
                //Указываем PE текущего персонажа
                fleetManagerSubpanel.activeTab.objectPE = currentCharacter.selfPE;
            }
        }

        void FleetManagerSbpnShowFleetsTab(
            ref CCharacter character)
        {
            //Берём подпанель менеджера флотов
            UIFleetManagerSubpanel fleetManagerSubpanel = sOUI.Value.gameWindow.objectPanel.fleetManagerSubpanel;

            //Берём вкладку флотов
            UIFleetsTab fleetsTab = fleetManagerSubpanel.fleetsTab;

            //Отображаем вкладку флотов
            FleetManagerSbpnShowTab(
                fleetsTab,
                ref character,
                out bool isSamePanel,
                out bool isSameSubpanel,
                out bool isSameTab,
                out bool isSameObject);

            //Если была открыта та же панель
            if (isSamePanel == true)
            {

            }
            //Иначе
            else
            {

            }

            //Если была открыта та же подпанель 
            if (isSameSubpanel == true)
            {

            }
            //Иначе
            else
            {

            }

            //Если была открыта та же вкладка
            if (isSameTab == true)
            {

            }
            //Иначе
            else
            {

            }

            //Если был отображён тот же объект
            if (isSameObject == true)
            {

            }
            //Иначе
            else
            {
                //Проверяем, какие оперативные группы должны быть отображены в этом списке
                //Для каждой оперативной группы персонажа
                for (int a = 0; a < character.ownedTaskForces.Count; a++)
                {
                    //Берём сущность оперативной группы и назначаем компонент "неудаляемый UI"
                    character.ownedTaskForces[a].Unpack(world.Value, out int tFEntity);
                    ref CNonDeletedUI tFNonDeletedUI = ref nonDeletedUIPool.Value.Add(tFEntity);
                }

                //Удаляем обзорную панель вкладки флотов с каждой группы, которой не должно быть в списке
                //Для каждой группы с отображаемыми панелями GUI, но без "неудаляемого UI"
                foreach(int tFEntity in taskForceDisplayedDeletedGUIFilter.Value)
                {
                    //Берём компонент панелей GUI
                    ref CTaskForceDisplayedGUIPanels tFDisplayedGUIPanels = ref tFDisplayedGUIPanelsPool.Value.Get(tFEntity);

                    //Если у группы есть обзорная панель вкладки флотов
                    if(tFDisplayedGUIPanels.fMSbpnFleetsTabSummaryPanel != null)
                    {
                        //Удаляем обзорную панель
                        FMSbpnFleetsTabDeleteTaskForceSummaryPanel(tFDisplayedGUIPanels.fMSbpnFleetsTabSummaryPanel.selfPE);
                    }
                }

                //Обновляем панели групп, которые уже имеют обзорные панели
                //Для каждой группы с панелями GUI и "неудаляемым UI"
                foreach (int tFEntity in taskForceDisplayedNonDeletedGUIFilter.Value)
                {
                    //Берём группу и компонент панелей GUI
                    ref CTaskForce tF = ref tFPool.Value.Get(tFEntity);
                    ref CTaskForceDisplayedGUIPanels tFDisplayedGUIPanels = ref tFDisplayedGUIPanelsPool.Value.Get(tFEntity);

                    //Если у группы есть обзорная панель вкладки флотов
                    if (tFDisplayedGUIPanels.fMSbpnFleetsTabSummaryPanel != null)
                    {
                        //Обновляем её данные
                        tFDisplayedGUIPanels.fMSbpnFleetsTabSummaryPanel.RefreshPanel(ref tF);

                        //И удаляем с группы "неудаляемый UI"
                        nonDeletedUIPool.Value.Del(tFEntity);
                    }
                    //Иначе панель должна создаваться, но это оставляем на следующий цикл, в котором панели создаются
                }

                //Создаём обзорные панели для групп, которые не имеют их, но должны
                //Для каждой группы с "неудаляемым UI"
                foreach(int tFEntity in taskForceNonDeletedUIFilter.Value)
                {
                    //Берём группу
                    ref CTaskForce tF = ref tFPool.Value.Get(tFEntity);

                    //В данный момент у группы не может существовать обзорной панели вкладки флотов,
                    //поэтому можно запросить её создание, не беспокоясь

                    //Создаём обзорную панель
                    FMSbpnFleetsTabCreateTaskForceSummaryPanel(ref tF);

                    //И удаляем с группы "неудаляемый UI"
                    nonDeletedUIPool.Value.Del(tFEntity);
                }
            }
        }

        void FMSbpnFleetsTabCreateTaskForceSummaryPanel(
            ref RGameCreatePanel requestComp)
        {
            //Берём оперативную группу
            requestComp.objectPE.Unpack(world.Value, out int tFEntity);
            ref CTaskForce tF = ref tFPool.Value.Get(tFEntity);

            //Создаём обзорную панель
            FMSbpnFleetsTabCreateTaskForceSummaryPanel(ref tF);
        }

        void FMSbpnFleetsTabCreateTaskForceSummaryPanel(
            ref CTaskForce tF)
        {
            //Берём сущность оперативной группы
            tF.selfPE.Unpack(world.Value, out int tFEntity);

            //Создаём компонент панелей GUI, если необходимо
            GUICreateTaskForceGUIPanels(ref tF);

            //Берём компонент панелей GUI
            ref CTaskForceDisplayedGUIPanels tFDisplayedGUIPanels = ref tFDisplayedGUIPanelsPool.Value.Get(tFEntity);

            //Берём вкладку флотов
            UIFleetsTab fleetsTab = sOUI.Value.gameWindow.objectPanel.fleetManagerSubpanel.fleetsTab;

            //Создаём обзорную панель вкладки флотов
            Game.GUI.Object.FleetManager.Fleets.UITaskForceSummaryPanel.InstantiatePanel(
                ref tF, ref tFDisplayedGUIPanels,
                fleetsTab.layoutGroup);
        }

        void FMSbpnFleetsTabDeleteTaskForceSummaryPanel(
            EcsPackedEntity tFPE)
        {
            //Берём оперативную группу и компонент панелей GUI
            tFPE.Unpack(world.Value, out int tFEntity);
            ref CTaskForceDisplayedGUIPanels tFDisplayedGUIPanels = ref tFDisplayedGUIPanelsPool.Value.Get(tFEntity);

            //Кэшируем обзорную панель вкладки флотов
            Game.GUI.Object.FleetManager.Fleets.UITaskForceSummaryPanel.CachePanel(ref tFDisplayedGUIPanels);

            //Удаляем компонент панелей GUI, если необходимо
            GUIDeleteTaskForceGUIPanels(tFPE);
        }
        #endregion
        #endregion
        #endregion
        #endregion

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
    }
}