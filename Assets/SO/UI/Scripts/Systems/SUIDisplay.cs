
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
using SO.UI.Game.GUI.Object.Country;
using SO.UI.Game.GUI.Object.Province;
using SO.UI.Game.GUI.Object.MapArea;
using SO.UI.Game.GUI.Object.FleetManager;
using SO.UI.Game.GUI.Object.Events;
using SO.Map;
using SO.Country;
using SO.Warfare.Fleet;
using SO.Map.MapArea;
using SO.Map.Hexasphere;
using SO.Map.UI;
using SO.Map.Generation;
using SO.Map.Economy;
using SO.Map.Province;

namespace SO.UI
{
    public class SUIDisplay : IEcsInitSystem, IEcsRunSystem
    {
        //Миры
        readonly EcsWorldInject world = default;


        //Карта
        readonly EcsPoolInject<CProvinceHexasphere> pHSPool = default;
        readonly EcsPoolInject<CProvinceCore> pCPool = default;

        readonly EcsPoolInject<CProvinceEconomy> pEPool = default;

        readonly EcsPoolInject<CProvinceDisplayedGUIPanels> provinceDisplayedGUIPanelsPool = default;
        readonly EcsFilterInject<Inc<CProvinceHexasphere, CProvinceDisplayedMapPanels>> provinceDisplayedMapPanelsFilter = default;
        readonly EcsPoolInject<CProvinceDisplayedMapPanels> provinceDisplayedMapPanelsPool = default;

        readonly EcsPoolInject<CMapArea> mAPool = default;

        //Страны
        readonly EcsPoolInject<CCountry> countryPool = default;

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
            GOProvinceRenderer.provinceRendererPrefab = uIData.Value.provinceRendererPrefab;
            CProvinceDisplayedMapPanels.mapPanelGroupPrefab = uIData.Value.mapPanelGroup;

            Game.GUI.Object.MapArea.Provinces.UIProvinceSummaryPanel.panelPrefab = uIData.Value.mASbpnProvincesTabProvinceSummaryPanelPrefab;
            UIPCMainMapPanel.panelPrefab = uIData.Value.pCMainMapPanelPrefab;

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

                //Если запрашивается создание обзорной панели провинции вкладки провинций подпанели области карты
                if (requestComp.panelType == GamePanelType.ProvinceSummaryPanelMASbpnProvincesTab)
                {
                    //Создаём обзорную панель провинции
                    MapAreaSbpnCreateProvinceSummaryPanel(ref requestComp);
                }
                //Иначе, если запрашивается создание главной панели карты провинции
                if (requestComp.panelType == GamePanelType.ProvinceMainMapPanel)
                {
                    //Создаём главную панель карты провинции
                    MapUICreateProvinceMainMapPanel(ref requestComp);
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
            //Обновляем интерфейс провинций
            GameRefreshUIProvince();

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

                //Если запрашивается удаление обзорной панели провинции вкладки провинций подпанели области карты
                if (requestComp.panelType == GamePanelType.ProvinceSummaryPanelMASbpnProvincesTab)
                {
                    //Удаляем обзорную панель
                    MapAreaSbpnProvincesTabDeleteProvinceSummaryPanel(requestComp.objectPE);
                }
                //Иначе, если запрашивается удаление главной панели карты провинции
                else if (requestComp.panelType == GamePanelType.ProvinceMainMapPanel)
                {
                    //Удаляем панель карты
                    MapUIDeleteProvinceMainMapPanel(requestComp.objectPE);
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

        readonly EcsFilterInject<Inc<CProvinceCore, CProvinceDisplayedGUIPanels, SRGameRefreshPanels>> provinceRefreshGUIPanelsSelfRequestFilter = default;
        readonly EcsFilterInject<Inc<CProvinceCore, CProvinceDisplayedMapPanels, SRGameRefreshPanels>> provinceRefreshMapPanelsSelfRequestFilter = default;
        void GameRefreshUIProvince()
        {
            //Для каждой провинции с компонентом панелей GUI и самозапросом обновления панелей
            foreach (int provinceEntity in provinceRefreshGUIPanelsSelfRequestFilter.Value)
            {
                //Берём провинцию и компонент панелей
                ref CProvinceCore pC = ref pCPool.Value.Get(provinceEntity);
                ref CProvinceEconomy pE = ref pEPool.Value.Get(provinceEntity);
                ref CProvinceDisplayedGUIPanels provinceDisplayedGUIPanels = ref provinceDisplayedGUIPanelsPool.Value.Get(provinceEntity);

                //Если провинция имеет отображаемую обзорную панель вкладки провинций подпанели области карты
                if (provinceDisplayedGUIPanels.mASbpnProvincesTabSummaryPanel != null)
                {
                    //Обновляем её
                    provinceDisplayedGUIPanels.mASbpnProvincesTabSummaryPanel.RefreshPanel(ref pC, ref pE);
                }
            }

            //Для каждой провинции с компонентом панелей карты и самозапросом обновления панелей
            foreach (int provinceEntity in provinceRefreshMapPanelsSelfRequestFilter.Value)
            {
                //Берём провинцию и компонент панелей
                ref CProvinceCore pC = ref pCPool.Value.Get(provinceEntity);
                ref CProvinceDisplayedMapPanels provinceDisplayedMapPanels = ref provinceDisplayedMapPanelsPool.Value.Get(provinceEntity);

                //Если провинция имеет отображаемую главную панель
                if (provinceDisplayedMapPanels.mainMapPanel != null)
                {
                    //Обновляем её
                    provinceDisplayedMapPanels.mainMapPanel.RefreshPanel(ref pC);
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
            //Для каждой провинции с панелями карты
            foreach (int provinceEntity in provinceDisplayedMapPanelsFilter.Value)
            {
                //Берём компонент панелей карты
                ref CProvinceDisplayedMapPanels provinceDisplayedMapPanels = ref provinceDisplayedMapPanelsPool.Value.Get(provinceEntity);

                float d = Vector3.Dot(
                    Camera.main.transform.position.normalized,
                    provinceDisplayedMapPanels.mapPanelGroup.transform.position.normalized);

                provinceDisplayedMapPanels.mapPanelGroup.transform.LookAt(Vector3.zero, Vector3.up);
                d = Mathf.Clamp01(d);
                provinceDisplayedMapPanels.mapPanelGroup.transform.rotation = Quaternion.Lerp(
                    provinceDisplayedMapPanels.mapPanelGroup.transform.rotation,
                    Quaternion.LookRotation(
                        provinceDisplayedMapPanels.mapPanelGroup.transform.position - Camera.main.transform.position,
                        Camera.main.transform.up),
                    d);
            }
        }

        void MapUICreateProvinceDisplayedMapPanels(
            ref CProvinceHexasphere pHS)
        {
            //Берём сущность провинции
            pHS.selfPE.Unpack(world.Value, out int provinceEntity);

            //Если провинция не имеет компонента панелей карты
            if (provinceDisplayedMapPanelsPool.Value.Has(provinceEntity) == false)
            {
                //Назначаем провинции компонент панелей карты
                ref CProvinceDisplayedMapPanels provinceDisplayedMapPanels = ref provinceDisplayedMapPanelsPool.Value.Add(provinceEntity);

                //Заполняем данные компонента
                provinceDisplayedMapPanels = new(0);

                //Создаём новый объект группы панелей карты
                CProvinceDisplayedMapPanels.InstantiateMapPanelGroup(
                    ref pHS, ref provinceDisplayedMapPanels,
                    mapGenerationData.Value.hexasphereScale,
                    uIData.Value.mapPanelAltitude);
            }
        }

        void MapUIDeleteProvinceDisplayedMapPanels(
            EcsPackedEntity provincePE)
        {
            //Берём сущность провинции и компонент панелей карты
            provincePE.Unpack(world.Value, out int provinceEntity);
            ref CProvinceDisplayedMapPanels provinceDisplayedMapPanels = ref provinceDisplayedMapPanelsPool.Value.Get(provinceEntity);

            //Если никакая из панелей не существует
            if (provinceDisplayedMapPanels.IsEmpty == true)
            {
                //Кэшируем группу панелей
                CProvinceDisplayedMapPanels.CacheMapPanelGroup(ref provinceDisplayedMapPanels);

                //Удаляем с сущности провинции компонент панелей карты
                provinceDisplayedMapPanelsPool.Value.Del(provinceEntity);
            }
        }

        void MapUICreateProvinceMainMapPanel(
            ref RGameCreatePanel requestComp)
        {
            //Берём PC и компонент визуализации провинции
            requestComp.objectPE.Unpack(world.Value, out int provinceEntity);
            ref CProvinceCore pC = ref pCPool.Value.Get(provinceEntity);
            ref CProvinceHexasphere pHS = ref pHSPool.Value.Get(provinceEntity);

            //Создаём компонент панелей карты, если необходимо
            MapUICreateProvinceDisplayedMapPanels(ref pHS);

            //Берём компонент панелей карты
            ref CProvinceDisplayedMapPanels provinceDisplayedMapPanels = ref provinceDisplayedMapPanelsPool.Value.Get(provinceEntity);

            //Создаём главную панель карты
            UIPCMainMapPanel.InstantiatePanel(
                ref pC, ref provinceDisplayedMapPanels);
        }

        void MapUIDeleteProvinceMainMapPanel(
            EcsPackedEntity provincePE)
        {
            //Берём компонент панелей карты
            provincePE.Unpack(world.Value, out int provinceEntity);
            ref CProvinceDisplayedMapPanels provinceDisplayedMapPanels = ref provinceDisplayedMapPanelsPool.Value.Get(provinceEntity);

            //Кэшируем главную панель карты
            UIPCMainMapPanel.CachePanel(ref provinceDisplayedMapPanels);

            //Удаляем компонент панелей карты, если необходимо
            MapUIDeleteProvinceDisplayedMapPanels(provincePE);
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

            //Прикрепляем панель к текущей провинции группы
            MapUISetParentTaskForceMainMapPanel(ref tF, ref tFDisplayedMapPanels);
        }

        void MapUISetParentTaskForceMainMapPanel(
            ref CTaskForce tF, ref CTaskForceDisplayedMapPanels tFDisplayedMapPanels)
        {
            //Если предыдущая провинция оперативной группы существует
            if(tF.previousProvincePE.Unpack(world.Value, out int previousProvinceEntity))
            {
                //Берём компонент панелей карты предыдущей провинции 
                ref CProvinceDisplayedMapPanels previousProvinceDisplayedMapPanels = ref provinceDisplayedMapPanelsPool.Value.Get(previousProvinceEntity);

                //Открепляем панель группы от него
                previousProvinceDisplayedMapPanels.CancelParentTaskForceMainMapPanel(tFDisplayedMapPanels.mainMapPanel);
            }

            //Обновляем текущую провинцию в компоненте панелей карты группы
            tFDisplayedMapPanels.currentProvincePE = tF.currentProvincePE;

            //Берём текущую провинцию группы
            tF.currentProvincePE.Unpack(world.Value, out int currentProvinceEntity);
            ref CProvinceHexasphere pHS = ref pHSPool.Value.Get(currentProvinceEntity);

            //Создаём компонент панелей карты, если необходимо
            MapUICreateProvinceDisplayedMapPanels(ref pHS);

            //Берём компонент панелей карты
            ref CProvinceDisplayedMapPanels currentProvinceDisplayedMapPanels = ref provinceDisplayedMapPanelsPool.Value.Get(currentProvinceEntity);

            //Прикрепляем к нему главную панель карты группы
            currentProvinceDisplayedMapPanels.SetParentTaskForceMainMapPanel(tFDisplayedMapPanels.mainMapPanel);
        }

        void MapUIDeleteTaskForceMainMapPanel(
            EcsPackedEntity tFPE)
        {
            //Берём компонент панелей карты
            tFPE.Unpack(world.Value, out int tFEntity);
            ref CTaskForceDisplayedMapPanels tFDisplayedMapPanels = ref tFDisplayedMapPanelsPool.Value.Get(tFEntity);

            //Берём компонент панелей карты текущей провинции группы
            tFDisplayedMapPanels.currentProvincePE.Unpack(world.Value, out int currentProvinceEntity);
            ref CProvinceDisplayedMapPanels provinceDisplayedMapPanels = ref provinceDisplayedMapPanelsPool.Value.Get(currentProvinceEntity);

            //Открепляем панель группы от него
            provinceDisplayedMapPanels.CancelParentTaskForceMainMapPanel(tFDisplayedMapPanels.mainMapPanel);

            //Кэшируем главную панель карты
            UITFMainMapPanel.CachePanel(ref tFDisplayedMapPanels);

            //Удаляем компонент панелей карты с группы, если необходимо
            MapUIDeleteTaskForceDisplayedMapPanels(tFPE);

            //Удаляем компонент панелей карты с провинции, если необходимо
            MapUIDeleteProvinceDisplayedMapPanels(tFDisplayedMapPanels.currentProvincePE);
        }

        void ParentAndAlignToProvince(
            GameObject go,
            ref CProvinceHexasphere pHS,
            float altitude = 0)
        {
            //Берём центр провинции
            Vector3 provinceCenter = pHS.GetProvinceCenter() * mapGenerationData.Value.hexasphereScale;

            //Если высота не равна нулю
            if(altitude != 0)
            {
                Vector3 direction = provinceCenter.normalized * altitude;
                go.transform.position = provinceCenter + direction;
            }
            //Иначе
            else
            {
                go.transform.position = provinceCenter;
            }

            //Привязываем объект к провинции
            go.transform.SetParent(pHS.selfObject.transform, true);
            go.transform.LookAt(pHS.selfObject.transform.position);
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

        readonly EcsFilterInject<Inc<CProvinceCore, CNonDeletedUI>> provinceNonDeletedUIFilter = default;
        readonly EcsFilterInject<Inc<CProvinceDisplayedGUIPanels, CNonDeletedUI>> provinceDisplayedNonDeletedGUIFilter = default;
        readonly EcsFilterInject<Inc<CProvinceDisplayedGUIPanels>, Exc<CNonDeletedUI>> provinceDisplayedDeletedGUIFilter = default;
        void GUICreateProvinceGUIPanels(
            ref CProvinceCore pC)
        {
            //Берём сущность провинции
            pC.selfPE.Unpack(world.Value, out int provinceEntity);
            
            //Если провинция не имеет компонента панелей GUI
            if(provinceDisplayedGUIPanelsPool.Value.Has(provinceEntity) == false)
            {
                //Назначаем ему компонент панелей GUI
                ref CProvinceDisplayedGUIPanels provinceDisplayedGUIPanels = ref provinceDisplayedGUIPanelsPool.Value.Add(provinceEntity);
            }
        }

        void GUIDeleteProvinceGUIPanels(
            EcsPackedEntity provincePE)
        {
            //Берём сущность провинции и компонент панелей GUI
            provincePE.Unpack(world.Value, out int provinceEntity);
            ref CProvinceDisplayedGUIPanels provinceDisplayedGUIPanels = ref provinceDisplayedGUIPanelsPool.Value.Get(provinceEntity);

            //Если никакая из панелей не существует
            if(provinceDisplayedGUIPanels.mASbpnProvincesTabSummaryPanel == null)
            {
                //Удаляем с сущности провинции компонент панелей GUI
                provinceDisplayedGUIPanelsPool.Value.Del(provinceEntity);
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

                //Иначе, если запрашивается отображение вкладок страны
                else if (requestComp.requestType >= ObjectPanelActionRequestType.CountryOverview
                    && requestComp.requestType <= ObjectPanelActionRequestType.CountryOverview)
                {
                    //Берём страну
                    requestComp.objectPE.Unpack(world.Value, out int countryEntity);
                    ref CCountry country = ref countryPool.Value.Get(countryEntity);

                    //Если запрашивается отображение обзорной вкладки
                    if (requestComp.requestType == ObjectPanelActionRequestType.CountryOverview)
                    {
                        //Отображаем обзорную вкладку страны
                        CountrySbpnShowOverviewTab(ref country);
                    }
                }
                //Иначе, если запрашивается отображение вкладок провинции
                else if (requestComp.requestType >= ObjectPanelActionRequestType.ProvinceOverview
                    && requestComp.requestType <= ObjectPanelActionRequestType.ProvinceOverview)
                {
                    //Берём провинцию
                    requestComp.objectPE.Unpack(world.Value, out int provinceEntity);
                    ref CProvinceHexasphere pHS = ref pHSPool.Value.Get(provinceEntity);
                    ref CProvinceCore pC = ref pCPool.Value.Get(provinceEntity);

                    //Если запрашивается отображение обзорной вкладки
                    if (requestComp.requestType == ObjectPanelActionRequestType.ProvinceOverview)
                    {
                        //Отображаем обзорную вкладку провинции
                        ProvinceSbpnShowOverviewTab(ref pHS, ref pC);
                    }
                }
                //Иначе, если запрашивается отображение вкладок области карты
                else if (requestComp.requestType >= ObjectPanelActionRequestType.MapAreaOverview
                    && requestComp.requestType <= ObjectPanelActionRequestType.MapAreaProvinces)
                {
                    //Берём область
                    requestComp.objectPE.Unpack(world.Value, out int mAEntity);
                    ref CMapArea mA = ref mAPool.Value.Get(mAEntity);

                    //Если запрашивается отображение обзорной вкладки
                    if (requestComp.requestType == ObjectPanelActionRequestType.MapAreaOverview)
                    {
                        //Отображаем обзорную вкладку области
                        MapAreaSbpnShowOverviewTab(ref mA);
                    }
                    //Иначе, если запрашивается отображение вкладки провинций
                    else if (requestComp.requestType == ObjectPanelActionRequestType.MapAreaProvinces)
                    {
                        //Отображаем вкладку провинций области
                        MapAreaSbpnShowProvincesTab(ref mA, requestComp.secondObjectPE);
                    }
                }
                //Иначе, если запрашивается отображение вкладок менеджера флотов
                else if(requestComp.requestType >= ObjectPanelActionRequestType.FleetManagerFleets
                    && requestComp.requestType <= ObjectPanelActionRequestType.FleetManagerFleets)
                {
                    //Берём страну
                    requestComp.objectPE.Unpack(world.Value, out int countryEntity);
                    ref CCountry country = ref countryPool.Value.Get(countryEntity);

                    //Если запрашивается отображение вкладки флотов
                    if (requestComp.requestType == ObjectPanelActionRequestType.FleetManagerFleets)
                    {
                        //Отображаем вкладку флотов
                        FleetManagerSbpnShowFleetsTab(ref country);
                    }
                }

                gameObjectPanelRequestPool.Value.Del(requestEntity);
            }
        }

        void ObjectPnClose()
        {
            //Берём панель объекта
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //Если активна подпанель страны
            if (objectPanel.activeSubpanelType == ObjectSubpanelType.Country)
            {
                //Скрываем активную вкладку
                objectPanel.countrySubpanel.HideActiveTab();
            }
            //Иначе, если активна подпанель провинции
            else if (objectPanel.activeSubpanelType == ObjectSubpanelType.Province)
            {
                //Скрываем активную вкладку
                objectPanel.provinceSubpanel.HideActiveTab();
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

        #region CountrySubpanel
        void CountrySbpnShowTab(
            UIASubpanelTab currentCountryTab,
            ref CCountry currentCountry,
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

            //Берём подпанель страны
            UICountrySubpanel countrySubpanel = objectPanel.countrySubpanel;

            //Отображаем подпанель страны, если необходимо
            ObjectPnOpenSubpanel(
                countrySubpanel, ObjectSubpanelType.Country,
                out isSamePanel,
                out isSameSubpanel);

            //Если открыта необходимая вкладка
            if (countrySubpanel.activeTab == currentCountryTab)
            {
                //Если была открыта та же подпанель
                if(isSameSubpanel == true)
                {
                    //Сообщаем, что была открыта та же вкладка
                    isSameTab = true;

                    //Если вкладка была открыта для той же страны
                    if (countrySubpanel.activeTab.objectPE.EqualsTo(currentCountry.selfPE) == true)
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
                countrySubpanel.tabGroup.OnTabSelected(currentCountryTab.selfTabButton);

                //Указываем её как активную вкладку
                countrySubpanel.activeTab = currentCountryTab;
            }

            //Если был отображён тот же объект
            if (isSameObject == true)
            {

            }
            //Иначе
            else
            {
                //Указываем PE текущей страны
                countrySubpanel.activeTab.objectPE = currentCountry.selfPE;

                //Отображаем название панели - название страны
                objectPanel.objectName.text = currentCountry.selfIndex.ToString();
            }
        }

        void CountrySbpnShowOverviewTab(
            ref CCountry country)
        {
            //Берём подпанель страны
            UICountrySubpanel countrySubpanel = sOUI.Value.gameWindow.objectPanel.countrySubpanel;

            //Берём обзорную вкладку
            Game.GUI.Object.Country.UIOverviewTab overviewTab = countrySubpanel.overviewTab;

            //Отображаем обзорную вкладку
            CountrySbpnShowTab(
                overviewTab,
                ref country,
                out bool isSamePanel,
                out bool isSameSubpanel,
                out bool isSameTab,
                out bool isSameObject);
        }
        #endregion

        #region RegionSubpanel
        void ProvinceSbpnShowTab(
            UIASubpanelTab currentProvinceTab,
            ref CProvinceCore currentPC,
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

            //Берём подпанель провинции
            UIProvinceSubpanel provinceSubpanel = objectPanel.provinceSubpanel;

            //Отображаем подпанель провинции, если необходимо
            ObjectPnOpenSubpanel(
                provinceSubpanel, ObjectSubpanelType.Province,
                out isSamePanel,
                out isSameSubpanel);

            //Если открыта необходимая вкладка
            if (provinceSubpanel.activeTab == currentProvinceTab)
            {
                //Если была открыта та же подпанель
                if (isSameSubpanel == true)
                {
                    //Сообщаем, что была открыта та же вкладка
                    isSameTab = true;

                    //Если вкладка была открыта для той же провинции
                    if (provinceSubpanel.activeTab.objectPE.EqualsTo(currentPC.selfPE) == true)
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
                provinceSubpanel.tabGroup.OnTabSelected(currentProvinceTab.selfTabButton);

                //Указываем её как активную вкладку
                provinceSubpanel.activeTab = currentProvinceTab;
            }

            //Если был отображён тот же объект
            if (isSameObject == true)
            {

            }
            //Иначе
            else
            {
                //Указываем PE текущей провинции
                provinceSubpanel.activeTab.objectPE = currentPC.selfPE;

                //Отображаем название панели - название провинции
                objectPanel.objectName.text = currentPC.Index.ToString();
            }
        }

        void ProvinceSbpnShowOverviewTab(
            ref CProvinceHexasphere pHS, ref CProvinceCore pC)
        {
            //Берём подпанель провинции
            UIProvinceSubpanel provinceSubpanel = sOUI.Value.gameWindow.objectPanel.provinceSubpanel;

            //Берём обзорную вкладку
            Game.GUI.Object.Province.UIOverviewTab overviewTab = provinceSubpanel.overviewTab;

            //Отображаем обзорную вкладку
            ProvinceSbpnShowTab(
                overviewTab,
                ref pC,
                out bool isSamePanel,
                out bool isSameSubpanel,
                out bool isSameTab,
                out bool isSameObject);
        }
        #endregion

        #region MapAreaSubpanel
        void MapAreaSbpnShowTab(
            UIASubpanelTab currentMATab,
            ref CMapArea currentMA,
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

            //Берём подпанель области карты
            UIMapAreaSubpanel mASubpanel = objectPanel.mapAreaSubpanel;

            //Отображаем подпанель области, если необходимо
            ObjectPnOpenSubpanel(
                mASubpanel, ObjectSubpanelType.MapArea,
                out isSamePanel,
                out isSameSubpanel);

            //Если открыта необходимая вкладка
            if (mASubpanel.activeTab == currentMATab)
            {
                //Если была открыта та же подпанель
                if (isSameSubpanel == true)
                {
                    //Сообщаем, что была открыта та же вкладка
                    isSameTab = true;

                    //Если вкладка была открыта для той же области
                    if (mASubpanel.activeTab.objectPE.EqualsTo(currentMA.selfPE) == true)
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
                mASubpanel.tabGroup.OnTabSelected(currentMATab.selfTabButton);

                //Указываем её как активную вкладку
                mASubpanel.activeTab = currentMATab;
            }

            //Если был отображён тот же объект
            if (isSameObject == true)
            {

            }
            //Иначе
            else
            {
                //Указываем PE текущей области
                mASubpanel.activeTab.objectPE = currentMA.selfPE;

                //Отображаем название панели - название области
                objectPanel.objectName.text = currentMA.selfPE.Id.ToString();
            }
        }

        void MapAreaSbpnShowOverviewTab(
            ref CMapArea mA)
        {
            //Берём подпанель области карты
            UIMapAreaSubpanel mASubpanel = sOUI.Value.gameWindow.objectPanel.mapAreaSubpanel;

            //Берём обзорную вкладку
            Game.GUI.Object.MapArea.UIOverviewTab overviewTab = mASubpanel.overviewTab;

            //Отображаем обзорную вкладку
            MapAreaSbpnShowTab(
                overviewTab,
                ref mA,
                out bool isSamePanel,
                out bool isSameSubpanel,
                out bool isSameTab,
                out bool isSameObject);
        }

        void MapAreaSbpnShowProvincesTab(
            ref CMapArea mA, EcsPackedEntity currentPCPE = new())
        {
            //Берём подпанель области карты
            UIMapAreaSubpanel mASubpanel = sOUI.Value.gameWindow.objectPanel.mapAreaSubpanel;

            //Берём вкладку провинций
            Game.GUI.Object.MapArea.UIProvincesTab provincesTab = mASubpanel.provincesTab;

            //Отображаем вкладку провинций
            MapAreaSbpnShowTab(
                provincesTab,
                ref mA,
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
                //Проверяем, какие провинции должны быть отображены в этом списке
                //Для каждой провинции области
                for (int a = 0; a < mA.provincePEs.Length; a++)
                {
                    //Берём сущность провинции и назначаем компонент "неудаляемый UI"
                    mA.provincePEs[a].Unpack(world.Value, out int provinceEntity);
                    ref CNonDeletedUI provinceNonDeletedUI = ref nonDeletedUIPool.Value.Add(provinceEntity);
                }

                //Удаляем обзорную панель вкладки провинций с каждой провинции, которой не должно быть в списке
                //Для каждой провинции с отображаемыми панелями GUI, но без "неудаляемого UI"
                foreach (int provinceEntity in provinceDisplayedDeletedGUIFilter.Value)
                {
                    //Берём компонент панелей GUI
                    ref CProvinceDisplayedGUIPanels provinceDisplayedGUIPanels = ref provinceDisplayedGUIPanelsPool.Value.Get(provinceEntity);

                    //Если у провинции есть обзорная панель вкладки провинций
                    if (provinceDisplayedGUIPanels.mASbpnProvincesTabSummaryPanel != null)
                    {
                        //Удаляем обзорную панель
                        MapAreaSbpnProvincesTabDeleteProvinceSummaryPanel(provinceDisplayedGUIPanels.mASbpnProvincesTabSummaryPanel.selfPE);
                    }
                }

                //Обновляем панели провинций, которые уже имеют обзорные панели
                //Для каждой провинции с панелями GUI и "неудаляемым UI"
                foreach (int provinceEntity in provinceDisplayedNonDeletedGUIFilter.Value)
                {
                    //Берём провинцию и компонент панелей GUI
                    ref CProvinceCore pC = ref pCPool.Value.Get(provinceEntity);
                    ref CProvinceEconomy pE = ref pEPool.Value.Get(provinceEntity);
                    ref CProvinceDisplayedGUIPanels provinceDisplayedGUIPanels = ref provinceDisplayedGUIPanelsPool.Value.Get(provinceEntity);

                    //Если у провинции есть обзорная панель вкладки провинций
                    if (provinceDisplayedGUIPanels.mASbpnProvincesTabSummaryPanel != null)
                    {
                        //Обновляем её данные
                        provinceDisplayedGUIPanels.mASbpnProvincesTabSummaryPanel.RefreshPanel(ref pC, ref pE);

                        //И удаляем с провинции "неудаляемый UI"
                        nonDeletedUIPool.Value.Del(provinceEntity);
                    }
                    //Иначе панель должна создаваться, но это оставляем на следующий цикл, в котором панели создаются
                }

                //Создаём обзорные панели для провинций, которые не имеют их, но должны
                //Для каждой провинции с "неудаляемым UI"
                foreach (int provinceEntity in provinceNonDeletedUIFilter.Value)
                {
                    //Берём провинцию
                    ref CProvinceCore pC = ref pCPool.Value.Get(provinceEntity);
                    ref CProvinceEconomy pE = ref pEPool.Value.Get(provinceEntity);

                    //Создаём обзорную панель
                    MapAreaSbpnProvincesTabCreateProvinceSummaryPanel(ref pC, ref pE);

                    //И удаляем с провинции "неудаляемый UI"
                    nonDeletedUIPool.Value.Del(provinceEntity);
                }
            }

            //Если PE текущей провинции не пуста
            if (currentPCPE.Unpack(world.Value, out int currentProvinceEntity))
            {
                //Берём сущность текущей провинции и компонент панелей GUI
                ref CProvinceDisplayedGUIPanels currentProvinceDisplayedGUIPanels = ref provinceDisplayedGUIPanelsPool.Value.Get(currentProvinceEntity);

                //Центрируем список на текущей провинции
                provincesTab.scrollView.FocusOnItem(currentProvinceDisplayedGUIPanels.mASbpnProvincesTabSummaryPanel.selfRect);
            }
        }

        void MapAreaSbpnCreateProvinceSummaryPanel(
            ref RGameCreatePanel requestComp)
        {
            //Берём провинцию
            requestComp.objectPE.Unpack(world.Value, out int provinceEntity);
            ref CProvinceCore pC = ref pCPool.Value.Get(provinceEntity);
            ref CProvinceEconomy pE = ref pEPool.Value.Get(provinceEntity);

            //Создаём обзорную панель
            MapAreaSbpnProvincesTabCreateProvinceSummaryPanel(ref pC, ref pE);
        }

        void MapAreaSbpnProvincesTabCreateProvinceSummaryPanel(
            ref CProvinceCore pC, ref CProvinceEconomy pE)
        {
            //Берём сущность провинции
            pC.selfPE.Unpack(world.Value, out int provinceEntity);

            //Создаём компонент панелей GUI, если необходимо
            GUICreateProvinceGUIPanels(ref pC);

            //Берём компонент панелей GUI
            ref CProvinceDisplayedGUIPanels provinceDisplayedGUIPanels = ref provinceDisplayedGUIPanelsPool.Value.Get(provinceEntity);

            //Берём вкладку провинций
            UIProvincesTab provincesTab = sOUI.Value.gameWindow.objectPanel.mapAreaSubpanel.provincesTab;

            //Создаём обзорную панель вкладки провинций
            Game.GUI.Object.MapArea.Provinces.UIProvinceSummaryPanel.InstantiatePanel(
                ref pC, ref pE, ref provinceDisplayedGUIPanels,
                provincesTab.layoutGroup);
        }

        void MapAreaSbpnProvincesTabDeleteProvinceSummaryPanel(
            EcsPackedEntity provincePE)
        {
            //Берём провинцию и компонент панелей GUI
            provincePE.Unpack(world.Value, out int provinceEntity);
            ref CProvinceDisplayedGUIPanels provinceDisplayedGUIPanels = ref provinceDisplayedGUIPanelsPool.Value.Get(provinceEntity);

            //Кэшируем обзорную панель вкладки провинций
            Game.GUI.Object.MapArea.Provinces.UIProvinceSummaryPanel.CachePanel(ref provinceDisplayedGUIPanels);

            //Удаляем компонент панелей GUI, если необходимо
            GUIDeleteProvinceGUIPanels(provincePE);
        }
        #endregion

        #region FleetManager
        void FleetManagerSbpnShowTab(
            UIASubpanelTab currentFleetManagerTab,
            ref CCountry currentCountry,
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

                    //Если вкладка была открыта для той же страны
                    if (fleetManagerSubpanel.activeTab.objectPE.EqualsTo(currentCountry.selfPE) == true)
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
                //Указываем PE текущей страны
                fleetManagerSubpanel.activeTab.objectPE = currentCountry.selfPE;
            }
        }

        void FleetManagerSbpnShowFleetsTab(
            ref CCountry country)
        {
            //Берём подпанель менеджера флотов
            UIFleetManagerSubpanel fleetManagerSubpanel = sOUI.Value.gameWindow.objectPanel.fleetManagerSubpanel;

            //Берём вкладку флотов
            UIFleetsTab fleetsTab = fleetManagerSubpanel.fleetsTab;

            //Отображаем вкладку флотов
            FleetManagerSbpnShowTab(
                fleetsTab,
                ref country,
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
                //Для каждой оперативной группы страны
                for (int a = 0; a < country.ownedTaskForces.Count; a++)
                {
                    //Берём сущность оперативной группы и назначаем компонент "неудаляемый UI"
                    country.ownedTaskForces[a].Unpack(world.Value, out int tFEntity);
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