
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
using SO.UI.Game.Events;
using SO.UI.Game.Object;
using SO.UI.Game.Object.Events;
using SO.Map;
using SO.Faction;
using SO.Map.Hexasphere;

namespace SO.UI
{
    public class SUIDisplay : IEcsInitSystem, IEcsRunSystem
    {
        //Миры
        readonly EcsWorldInject world = default;


        //Карта
        readonly EcsPoolInject<CRegionHexasphere> rHSPool = default;
        readonly EcsPoolInject<CRegionCore> rCPool = default;
        readonly EcsFilterInject<Inc<CRegionHexasphere, CRegionDisplayedMapPanels>> regionDisplayedMapPanelsFilter = default;
        readonly EcsPoolInject<CRegionDisplayedMapPanels> regionDisplayedMapPanelsPool = default;

        readonly EcsPoolInject<CExplorationRegionFractionObject> exRFOPool = default;

        //Фракции
        readonly EcsPoolInject<CFaction> factionPool = default;


        //Общие события
        readonly EcsFilterInject<Inc<RGeneralAction>> generalActionRequestFilter = default;
        readonly EcsPoolInject<RGeneralAction> generalActionRequestPool = default;

        readonly EcsPoolInject<EcsGroupSystemState> ecsGroupSystemStatePool = default;

        //Данные
        readonly EcsCustomInject<UIData> uIData = default;
        readonly EcsCustomInject<MapGenerationData> mapGenerationData = default;
        readonly EcsCustomInject<InputData> inputData = default;
        readonly EcsCustomInject<RuntimeData> runtimeData = default;

        readonly EcsCustomInject<SOUI> sOUI = default;

        public void Init(IEcsSystems systems)
        {
            ListPool<int>.Init();

            //Заносим префабы в их графы
            GORegionRenderer.regionRendererPrefab = uIData.Value.regionRendererPrefab;
            CRegionDisplayedMapPanels.mapPanelGroupPrefab = uIData.Value.mapPanelGroup;
            UIRCMainMapPanel.panelPrefab = uIData.Value.rCMainMapPanelPrefab;

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
                if(requestComp.actionType == GeneralActionType.QuitGame)
                {
                    Debug.LogError("Выход из игры!");

                    //Закрываем игру
                    Application.Quit();
                }

                generalActionRequestPool.Value.Del(requestEntity);
            }

            //Если активно окно игры
            if(sOUI.Value.activeMainWindowType == MainWindowType.Game)
            {
                //Проверяем события в окне игры
                GameEventCheck();

                //Обновляем панели карты
                MapUIMapPanelsUpdate();
            }
            //Иначе, если активно окно главного меню
            else if(sOUI.Value.activeMainWindowType == MainWindowType.MainMenu)
            {
                //Проверяем события в окне главного меню
                MainMenuAction();
            }
        }

        void CloseMainWindow()
        {
            //Если какое-либо главное окно было активным
            if(sOUI.Value.activeMainWindowType != MainWindowType.None)
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
                if(requestComp.actionType == MainMenuActionType.OpenGame)
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
        void GameEventCheck()
        {
            //Проверяем запросы создания панелей
            GameCreatePanelRequest();

            //Проверяем запросы действий в игре
            GameAction();

            //Проверяем запросы действия в панели объекта
            ObjPnAction();

            //Проверяем самозапросы обновления интерфейса объектов
            GameRefreshUISelfRequest();

            //Проверяем запросы удаления панелей
            GameDeletePanelRequest();
        }

        readonly EcsFilterInject<Inc<RGameCreatePanel>> gameCreatePanelRequestFilter = default;
        readonly EcsPoolInject<RGameCreatePanel> gameCreatePanelRequestPool = default;
        void GameCreatePanelRequest()
        {
            //Для каждого запроса создания панели
            foreach(int requestEntity in gameCreatePanelRequestFilter.Value)
            {
                //Берём запрос
                ref RGameCreatePanel requestComp = ref gameCreatePanelRequestPool.Value.Get(requestEntity);

                //Если запрашивается создание главной панели карты региона
                if(requestComp.panelType == GamePanelType.RegionMainMapPanel)
                {
                    //Создаём главную панель карты региона
                    MapUICreateRegionMainMapPanel(ref requestComp);
                }

                gameCreatePanelRequestPool.Value.Del(requestEntity);
            }
        }

        readonly EcsFilterInject<Inc<SRGameRefreshPanels>> gameRefreshPanelsSelfRequestFilter = default;
        readonly EcsPoolInject<SRGameRefreshPanels> gameRefreshPanelsSelfRequestPool = default;
        void GameRefreshUISelfRequest()
        {
            //Обновляем интерфейс регионов и RC
            GameRefreshUIRegionAndRC();

            //Для каждой сущности с самозапросом обновления панелей
            foreach (int entity in gameRefreshPanelsSelfRequestFilter.Value)
            {
                //Удаляем самозапрос обновления панелей
                gameRefreshPanelsSelfRequestPool.Value.Del(entity);
            }
        }

        readonly EcsFilterInject<Inc<RGameDeletePanel>> gameDeletePanelRequestFilter = default;
        readonly EcsPoolInject<RGameDeletePanel> gameDeletePanelRequestPool = default;
        void GameDeletePanelRequest()
        {
            //Для каждого запроса удаления панели
            foreach(int requestEntity in gameDeletePanelRequestFilter.Value)
            {
                //Берём запрос
                ref RGameDeletePanel requestComp = ref gameDeletePanelRequestPool.Value.Get(requestEntity);

                //Если запрашивается удаление главной панели карты региона
                if (requestComp.panelType == GamePanelType.RegionMainMapPanel)
                {
                    //Удаляем главную панель карты региона
                    MapUIDeleteRegionMainMapPanel(ref requestComp);
                }

                gameDeletePanelRequestPool.Value.Del(requestEntity);
            }
        }

        readonly EcsFilterInject<Inc<RGameAction>> gameActionRequestFilter = default;
        readonly EcsPoolInject<RGameAction> gameActionRequestPool = default;
        void GameAction()
        {
            //Для каждого запроса действия в игре
            foreach(int requestEntity in gameActionRequestFilter.Value)
            {
                //Берём запрос
                ref RGameAction requestComp = ref gameActionRequestPool.Value.Get(requestEntity);

                //Применяем состояние паузы
                if(requestComp.actionType == GameActionType.PauseOn || requestComp.actionType == GameActionType.PauseOff)
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
            if(pauseMode == GameActionType.PauseOn)
            {
                //Указываем, что игра неактивна
                runtimeData.Value.isGameActive = false;
            }
            //Иначе
            else if(pauseMode == GameActionType.PauseOff)
            {
                //Указываем, что игра активна
                runtimeData.Value.isGameActive = true;
            }
        }

        readonly EcsFilterInject<Inc<CRegionCore, CRegionDisplayedMapPanels, SRGameRefreshPanels>> regionRefreshMapPanelsSelfRequestFilter = default;
        void GameRefreshUIRegionAndRC()
        {
            //Для каждого региона с компонентом панелей карты и самозапросом обновления панелей
            foreach(int regionEntity in regionRefreshMapPanelsSelfRequestFilter.Value)
            {
                //Берём регион, компонент панелей и самозапрос обновления
                ref CRegionCore rC = ref rCPool.Value.Get(regionEntity);
                ref CRegionDisplayedMapPanels regionDisplayedMapPanels = ref regionDisplayedMapPanelsPool.Value.Get(regionEntity);
                ref SRGameRefreshPanels selfRequestComp = ref gameRefreshPanelsSelfRequestPool.Value.Get(regionEntity);

                //Если регион имеет отображаемую главную панель
                if(regionDisplayedMapPanels.mainMapPanel != null)
                {
                    //Обновляем её
                    regionDisplayedMapPanels.mainMapPanel.RefreshPanel(ref rC);
                }
            }
        }

        #region MapUI
        void MapUIMapPanelsUpdate()
        {
            //Для каждого региона с панелями карты
            foreach(int regionEntity in regionDisplayedMapPanelsFilter.Value)
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

        void MapUICreateRCMapPanelGroup(
            ref CRegionCore rc)
        {
            //Берём сущность региона
            rc.selfPE.Unpack(world.Value, out int regionEntity);

            //Если регион не имеет компонент панелей карты
            if (regionDisplayedMapPanelsPool.Value.Has(regionEntity) == false)
            {
                //Берём компонент визуализации региона и назначаем региону компонент панелей карты
                ref CRegionHexasphere rHS = ref rHSPool.Value.Get(regionEntity);
                ref CRegionDisplayedMapPanels regionDisplayedMapPanels = ref regionDisplayedMapPanelsPool.Value.Add(regionEntity);

                //Создаём новый объект группы панелей карты
                CRegionDisplayedMapPanels.InstantiateMapPanelGroup(
                    ref rHS, ref regionDisplayedMapPanels,
                    mapGenerationData.Value.hexasphereScale,
                    uIData.Value.mapPanelAltitude);
            } 
        }

        void MapUIDeleteRCMapPanelGroup(
            EcsPackedEntity regionPE)
        {
            //Берём сущность региона и компонент панелей карты
            regionPE.Unpack(world.Value, out int regionEntity);
            ref CRegionDisplayedMapPanels regionDisplayedMapPanels = ref regionDisplayedMapPanelsPool.Value.Add(regionEntity);

            //Если никакая из панелей не существует
            if(regionDisplayedMapPanels.mainMapPanel == null)
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
            //Берём RC
            requestComp.objectPE.Unpack(world.Value, out int regionEntity);
            ref CRegionCore rC = ref rCPool.Value.Get(regionEntity);

            //Создаём компонент панелей карты, если необходимо
            MapUICreateRCMapPanelGroup(ref rC);

            //Берём компонент панелей карты
            ref CRegionDisplayedMapPanels regionDisplayedMapPanels = ref regionDisplayedMapPanelsPool.Value.Get(regionEntity);

            //Создаём главную панель карты
            UIRCMainMapPanel.InstantiatePanel(
                ref rC, ref regionDisplayedMapPanels);
        }

        void MapUIDeleteRegionMainMapPanel(
            ref RGameDeletePanel requestComp)
        {
            //Берём RC и компонент панелей карты
            requestComp.objectPE.Unpack(world.Value, out int regionEntity);
            ref CRegionDisplayedMapPanels regionDisplayedMapPanels = ref regionDisplayedMapPanelsPool.Value.Get(regionEntity);

            //Кэшируем главную панель карты
            UIRCMainMapPanel.CachePanel(
                ref regionDisplayedMapPanels);

            //Удаляем компонент панелей карты, если необходимо
            MapUIDeleteRCMapPanelGroup(requestComp.objectPE);
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

        #region ObjectPanel
        readonly EcsFilterInject<Inc<RGameObjectPanelAction>> gameObjectPanelRequestFilter = default;
        readonly EcsPoolInject<RGameObjectPanelAction> gameObjectPanelRequestPool = default;
        void ObjPnAction()
        {
            //Берём панель объекта
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //Для каждого запроса действия в игре
            foreach (int requestEntity in gameObjectPanelRequestFilter.Value)
            {
                //Берём запрос
                ref RGameObjectPanelAction requestComp = ref gameObjectPanelRequestPool.Value.Get(requestEntity);

                //Отображаем панель объекта
                ObjPnShow();

                //Если запрашивается отображение панели фракции
                if (requestComp.requestType == ObjectPanelActionRequestType.Faction)
                {
                    //Отображаем подпанель фракции
                    ObjPnShowFactionSubpanel(ref requestComp);
                }
                //Если запрашивается отображение панели региона
                else if(requestComp.requestType == ObjectPanelActionRequestType.Region)
                {
                    //Отображаем подпанель региона
                    ObjPnShowRegionSubpanel(ref requestComp);
                }

                //Иначе, если запрашивается закрытие панели объекта
                else if(requestComp.requestType == ObjectPanelActionRequestType.Close)
                {
                    //Если активна подпанель фракции
                    if(objectPanel.activeObjectSubpanelType == ObjectSubpanelType.Faction)
                    {
                        //Скрываем подпанель фракции 
                        ObjPnHideFactionSubpanel();
                    }
                    //Иначе, если активна подпанель региона
                    else if(objectPanel.activeObjectSubpanelType == ObjectSubpanelType.Region)
                    {
                        //Скрываем подпанель региона
                        ObjPnHideRegionSubpanel();
                    }

                    //Скрываем подпанель объекта
                    ObjPnHideObjectSubpanel();

                    //Скрываем панель объекта
                    ObjPnHide();
                }

                //Иначе, если активна подпанель фракции
                else if(objectPanel.activeObjectSubpanelType == ObjectSubpanelType.Faction)
                {
                    //Берём фракцию
                    requestComp.objectPE.Unpack(world.Value, out int factionEntity);
                    ref CFaction faction = ref factionPool.Value.Get(factionEntity);

                    //Если запрашивается отображение обзорной вкладки
                    if (requestComp.requestType == ObjectPanelActionRequestType.FactionOverview)
                    {
                        //Отображаем обзорную вкладку фракции
                        FactionSbpnShowOverviewTab(
                            ref faction,
                            requestComp.isRefresh);
                    }
                }
                //Иначе, если активна подпанель региона
                else if(objectPanel.activeObjectSubpanelType == ObjectSubpanelType.Region)
                {
                    //Берём регион
                    requestComp.objectPE.Unpack(world.Value, out int regionEntity);
                    ref CRegionHexasphere rHS = ref rHSPool.Value.Get(regionEntity);
                    ref CRegionCore rC = ref rCPool.Value.Get(regionEntity);

                    //Если запрашивается отображение обзорной вкладки
                    if (requestComp.requestType == ObjectPanelActionRequestType.RegionOverview)
                    {
                        //Отображаем обзорную вкладку региона
                        RegionSbpnShowOverviewTab(
                            ref rHS, ref rC,
                            requestComp.isRefresh);
                    }
                }

                gameObjectPanelRequestPool.Value.Del(requestEntity);
            }
        }

        void ObjPnShow()
        {
            //Берём окно игры
            UIGameWindow gameWindow = sOUI.Value.gameWindow;

            //Если какая-либо главная панель активна
            if(gameWindow.activeMainPanelType != MainPanelType.None)
            {
                //Скрываем её
                gameWindow.activeMainPanel.SetActive(false);
            }

            //Делаем панель объекта активной
            gameWindow.objectPanel.gameObject.SetActive(true);

            //Указываем её как активную главную панель
            gameWindow.activeMainPanelType = MainPanelType.Object;
            gameWindow.activeMainPanel = gameWindow.objectPanel.gameObject;
        }

        void ObjPnHide()
        {
            //Берём окно игры
            UIGameWindow gameWindow = sOUI.Value.gameWindow;

            //Скрываем панель объекта
            gameWindow.objectPanel.gameObject.SetActive(false);

            //Указываем, что нет активной главной панели
            gameWindow.activeMainPanelType = MainPanelType.None;
            gameWindow.activeMainPanel = null;
        }

        void ObjPnShowObjectSubpanel(
            ObjectSubpanelType objectSubpanelType, UIAObjectSubpanel objectSubpanel)
        {
            //Берём панель объекта
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //Если какая-либо подпанель активна, скрываем её
            if(objectPanel.activeObjectSubpanelType != ObjectSubpanelType.None)
            {
                objectPanel.activeObjectSubpanel.gameObject.SetActive(false);
            }

            //Делаем запрошенную подпанель активной
            objectSubpanel.gameObject.SetActive(true);

            //Указываем её как активную подпанель
            objectPanel.activeObjectSubpanelType = objectSubpanelType;
            objectPanel.activeObjectSubpanel = objectSubpanel;

            //Устанавливаем ширину панели заголовка соответственно запрошенной подпанели
            objectPanel.titlePanel.offsetMax = new Vector2(
                objectSubpanel.parentRect.offsetMax.x, objectPanel.titlePanel.offsetMax.y);
        }

        void ObjPnHideObjectSubpanel()
        {
            //Берём панель объекта
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //Скрываем активную подпанель
            objectPanel.activeObjectSubpanel.gameObject.SetActive(false);

            //Указываем, что нет активной подпанели
            objectPanel.activeObjectSubpanelType = ObjectSubpanelType.None;
            objectPanel.activeObjectSubpanel = null;

            //Очищаем PE активного объекта
            objectPanel.activeObjectPE = new();
        }

        #region FactionSubpanel
        void ObjPnShowFactionSubpanel(
            ref RGameObjectPanelAction requestComp)
        {
            //Берём фракцию
            requestComp.objectPE.Unpack(world.Value, out int factionEntity);
            ref CFaction faction = ref factionPool.Value.Get(factionEntity);

            //Берём панель объекта
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //Отображаем подпанель
            ObjPnShowObjectSubpanel(
                ObjectSubpanelType.Faction, objectPanel.factionSubpanel);

            //Указываем PE фракции
            objectPanel.activeObjectPE = faction.selfPE;

            //Отображаем, что это подпанель фракции
            objectPanel.objectName.text = "Faction";


            //Отображаем обзорную вкладку
            FactionSbpnShowOverviewTab(
                ref faction,
                false);
        }

        void ObjPnHideFactionSubpanel()
        {

        }

        void FactionSbpnShowOverviewTab(
            ref CFaction faction,
            bool isRefresh)
        {
            //Берём подпанель фракции
            UIFactionSubpanel factionSubpanel = sOUI.Value.gameWindow.objectPanel.factionSubpanel;

            //Берём обзорную вкладку
            Game.Object.Faction.UIOverviewTab overviewTab = factionSubpanel.overviewTab;

            //Отображаем обзорную вкладку
            factionSubpanel.tabGroup.OnTabSelected(overviewTab.selfTabButton);

            //Если производится обновление
            if(isRefresh == true)
            {

            }
        }
        #endregion

        #region RegionSubpanel
        void ObjPnShowRegionSubpanel(
            ref RGameObjectPanelAction requestComp)
        {
            //Берём регион
            requestComp.objectPE.Unpack(world.Value, out int regionEntity);
            ref CRegionHexasphere rHS = ref rHSPool.Value.Get(regionEntity);
            ref CRegionCore rC = ref rCPool.Value.Get(regionEntity);

            //Берём панель объекта
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //Отображаем подпанель
            ObjPnShowObjectSubpanel(
                ObjectSubpanelType.Region, objectPanel.regionSubpanel);

            //Указываем PE региона
            objectPanel.activeObjectPE = rHS.selfPE;

            //Отображаем, что это подпанель региона
            objectPanel.objectName.text = rC.Index.ToString();


            //Отображаем обзорную вкладку
            RegionSbpnShowOverviewTab(
                ref rHS, ref rC,
                false);
        }

        void ObjPnHideRegionSubpanel()
        {

        }

        void RegionSbpnShowOverviewTab(
            ref CRegionHexasphere rHS, ref CRegionCore rC,
            bool isRefresh)
        {
            //Берём подпанель региона
            UIRegionSubpanel regionSubpanel = sOUI.Value.gameWindow.objectPanel.regionSubpanel;

            //Берём обзорную вкладку
            Game.Object.Region.UIOverviewTab overviewTab = regionSubpanel.overviewTab;

            //Отображаем обзорную вкладку
            regionSubpanel.tabGroup.OnTabSelected(overviewTab.selfTabButton);

            //Берём фракцию игрока
            inputData.Value.playerFactionPE.Unpack(world.Value, out int factionEntity);
            ref CFaction faction = ref factionPool.Value.Get(factionEntity);

            //Если производится обновление
            if(isRefresh == true)
            {
                //Берём ExRFO фракции игрока
                rC.rFOPEs[faction.selfIndex].rFOPE.Unpack(world.Value, out int rFOEntity);
                ref CExplorationRegionFractionObject exRFO = ref exRFOPool.Value.Get(rFOEntity);

                //Отображаем уровень исследования региона
                overviewTab.explorationLevel.text = exRFO.explorationLevel.ToString();
            }
        }
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