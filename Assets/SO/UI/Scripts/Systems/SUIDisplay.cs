
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
using SO.UI.Game.Events;
using SO.UI.Game.Object;
using SO.UI.Game.Object.Events;
using SO.Map;
using SO.Faction;
using SO.Map.RFO;

namespace SO.UI
{
    public class SUIDisplay : IEcsInitSystem, IEcsRunSystem
    {
        //Миры
        readonly EcsWorldInject world = default;


        //Карта
        readonly EcsPoolInject<CRegion> regionPool = default;

        readonly EcsPoolInject<CRegionFO> rFOPool = default;

        readonly EcsPoolInject<CExplorationFRFO> exFRFOPool = default;

        //Фракции
        readonly EcsPoolInject<CFaction> factionPool = default;


        //Общие события
        readonly EcsFilterInject<Inc<RGeneralAction>> generalActionRequestFilter = default;
        readonly EcsPoolInject<RGeneralAction> generalActionRequestPool = default;

        readonly EcsPoolInject<EcsGroupSystemState> ecsGroupSystemStatePool = default;

        //Данные
        readonly EcsCustomInject<InputData> inputData = default;
        readonly EcsCustomInject<RuntimeData> runtimeData = default;

        readonly EcsCustomInject<SOUI> sOUI = default;

        public void Init(IEcsSystems systems)
        {
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
            //Проверяем запросы действий в игре
            GameAction();

            //Проверяем запросы действия в панели объекта
            ObjPnAction();
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
                    ref CRegion region = ref regionPool.Value.Get(regionEntity);
                    ref CRegionFO rFO = ref rFOPool.Value.Get(regionEntity);

                    //Если запрашивается отображение обзорной вкладки
                    if (requestComp.requestType == ObjectPanelActionRequestType.RegionOverview)
                    {
                        //Отображаем обзорную вкладку региона
                        RegionSbpnShowOverviewTab(
                            ref region, ref rFO,
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
            ref CRegion region = ref regionPool.Value.Get(regionEntity);
            ref CRegionFO rFO = ref rFOPool.Value.Get(regionEntity);

            //Берём панель объекта
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //Отображаем подпанель
            ObjPnShowObjectSubpanel(
                ObjectSubpanelType.Region, objectPanel.regionSubpanel);

            //Указываем PE региона
            objectPanel.activeObjectPE = region.selfPE;

            //Отображаем, что это подпанель региона
            objectPanel.objectName.text = region.Index.ToString();


            //Отображаем обзорную вкладку
            RegionSbpnShowOverviewTab(
                ref region, ref rFO,
                false);
        }

        void ObjPnHideRegionSubpanel()
        {

        }

        void RegionSbpnShowOverviewTab(
            ref CRegion region, ref CRegionFO rFO,
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
                //Берём ExFRFO фракции игрока
                rFO.factionRFOs[faction.selfIndex].fRFOPE.Unpack(world.Value, out int fRFOEntity);
                ref CExplorationFRFO exFRFO = ref exFRFOPool.Value.Get(fRFOEntity);

                //Отображаем уровень исследования региона
                overviewTab.explorationLevel.text = exFRFO.explorationLevel.ToString();
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