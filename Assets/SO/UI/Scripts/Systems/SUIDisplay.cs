
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
        //����
        readonly EcsWorldInject world = default;


        //�����
        readonly EcsPoolInject<CRegion> regionPool = default;

        readonly EcsPoolInject<CRegionFO> rFOPool = default;

        readonly EcsPoolInject<CExplorationFRFO> exFRFOPool = default;

        //�������
        readonly EcsPoolInject<CFaction> factionPool = default;


        //����� �������
        readonly EcsFilterInject<Inc<RGeneralAction>> generalActionRequestFilter = default;
        readonly EcsPoolInject<RGeneralAction> generalActionRequestPool = default;

        readonly EcsPoolInject<EcsGroupSystemState> ecsGroupSystemStatePool = default;

        //������
        readonly EcsCustomInject<InputData> inputData = default;
        readonly EcsCustomInject<RuntimeData> runtimeData = default;

        readonly EcsCustomInject<SOUI> sOUI = default;

        public void Init(IEcsSystems systems)
        {
            //��������� ���� �������� ����
            MainMenuOpenWindow();
        }

        public void Run(IEcsSystems systems)
        {
            //��� ������� ������ �������
            foreach (int requestEntity in generalActionRequestFilter.Value)
            {
                //���� ������
                ref RGeneralAction requestComp = ref generalActionRequestPool.Value.Get(requestEntity);

                //���� ������������� �������� ����
                if(requestComp.actionType == GeneralActionType.QuitGame)
                {
                    Debug.LogError("����� �� ����!");

                    //��������� ����
                    Application.Quit();
                }

                generalActionRequestPool.Value.Del(requestEntity);
            }

            //���� ������� ���� ����
            if(sOUI.Value.activeMainWindowType == MainWindowType.Game)
            {
                //��������� ������� � ���� ����
                GameEventCheck();
            }
            //�����, ���� ������� ���� �������� ����
            else if(sOUI.Value.activeMainWindowType == MainWindowType.MainMenu)
            {
                //��������� ������� � ���� �������� ����
                MainMenuAction();
            }
        }

        void CloseMainWindow()
        {
            //���� �����-���� ������� ���� ���� ��������
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
            //��� ������� ������� �������� � ������� ����
            foreach (int requestEntity in mainMenuActionRequestFilter.Value)
            {
                //���� ������
                ref RMainMenuAction requestComp = ref mainMenuActionRequestPool.Value.Get(requestEntity);

                //���� ������������� �������� ���� ����
                if(requestComp.actionType == MainMenuActionType.OpenGame)
                {
                    //������ ������ �������� ����� ����
                    NewGameMenuStartNewGame();

                    //��������� ���� ����
                    GameOpenWindow();
                }

                mainMenuActionRequestPool.Value.Del(requestEntity);
            }
        }

        void MainMenuOpenWindow()
        {
            //��������� �������� ������� ����
            CloseMainWindow();

            //���� ������ �� ���� �������� ����
            UIMainMenuWindow mainMenuWindow = sOUI.Value.mainMenuWindow;

            //������ ��� �������� � ��������� ��� ��������
            mainMenuWindow.gameObject.SetActive(true);
            sOUI.Value.activeMainWindow = sOUI.Value.mainMenuWindow;

            //���������, ��� ������� ���� �������� ����
            sOUI.Value.activeMainWindowType = MainWindowType.MainMenu;

        }
        #endregion

        #region NewGame
        readonly EcsPoolInject<RStartNewGame> startNewGameRequestPool = default;
        void NewGameMenuStartNewGame()
        {
            //������ ����� �������� � �������� �� ������ ������ ����� ����
            int requestEntity = world.Value.NewEntity();
            ref RStartNewGame requestComp = ref startNewGameRequestPool.Value.Add(requestEntity);

            //����������� ��������� ������ ������ "NewGame"
            EcsGroupSystemStateEvent("NewGame", true);
        }
        #endregion

        #region Game
        void GameEventCheck()
        {
            //��������� ������� �������� � ����
            GameAction();

            //��������� ������� �������� � ������ �������
            ObjPnAction();
        }

        readonly EcsFilterInject<Inc<RGameAction>> gameActionRequestFilter = default;
        readonly EcsPoolInject<RGameAction> gameActionRequestPool = default;
        void GameAction()
        {
            //��� ������� ������� �������� � ����
            foreach(int requestEntity in gameActionRequestFilter.Value)
            {
                //���� ������
                ref RGameAction requestComp = ref gameActionRequestPool.Value.Get(requestEntity);

                //��������� ��������� �����
                if(requestComp.actionType == GameActionType.PauseOn || requestComp.actionType == GameActionType.PauseOff)
                {
                    GamePause(requestComp.actionType);
                }

                gameActionRequestPool.Value.Del(requestEntity);
            }
        }

        void GameOpenWindow()
        {
            //��������� �������� ������� ����
            CloseMainWindow();

            //���� ������ �� ���� ����
            UIGameWindow gameWindow = sOUI.Value.gameWindow;

            //������ ��� �������� � ��������� ��� ��������
            gameWindow.gameObject.SetActive(true);
            sOUI.Value.activeMainWindow = sOUI.Value.gameWindow;

            //���������, ��� ������� ���� ����
            sOUI.Value.activeMainWindowType = MainWindowType.Game;
        }

        void GamePause(
            GameActionType pauseMode)
        {
            //���� ��������� �������� �����
            if(pauseMode == GameActionType.PauseOn)
            {
                //���������, ��� ���� ���������
                runtimeData.Value.isGameActive = false;
            }
            //�����
            else if(pauseMode == GameActionType.PauseOff)
            {
                //���������, ��� ���� �������
                runtimeData.Value.isGameActive = true;
            }
        }

        #region ObjectPanel
        readonly EcsFilterInject<Inc<RGameObjectPanelAction>> gameObjectPanelRequestFilter = default;
        readonly EcsPoolInject<RGameObjectPanelAction> gameObjectPanelRequestPool = default;
        void ObjPnAction()
        {
            //���� ������ �������
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //��� ������� ������� �������� � ����
            foreach (int requestEntity in gameObjectPanelRequestFilter.Value)
            {
                //���� ������
                ref RGameObjectPanelAction requestComp = ref gameObjectPanelRequestPool.Value.Get(requestEntity);

                //���������� ������ �������
                ObjPnShow();

                //���� ������������� ����������� ������ �������
                if (requestComp.requestType == ObjectPanelActionRequestType.Faction)
                {
                    //���������� ��������� �������
                    ObjPnShowFactionSubpanel(ref requestComp);
                }
                //���� ������������� ����������� ������ �������
                else if(requestComp.requestType == ObjectPanelActionRequestType.Region)
                {
                    //���������� ��������� �������
                    ObjPnShowRegionSubpanel(ref requestComp);
                }

                //�����, ���� ������������� �������� ������ �������
                else if(requestComp.requestType == ObjectPanelActionRequestType.Close)
                {
                    //���� ������� ��������� �������
                    if(objectPanel.activeObjectSubpanelType == ObjectSubpanelType.Faction)
                    {
                        //�������� ��������� ������� 
                        ObjPnHideFactionSubpanel();
                    }
                    //�����, ���� ������� ��������� �������
                    else if(objectPanel.activeObjectSubpanelType == ObjectSubpanelType.Region)
                    {
                        //�������� ��������� �������
                        ObjPnHideRegionSubpanel();
                    }

                    //�������� ��������� �������
                    ObjPnHideObjectSubpanel();

                    //�������� ������ �������
                    ObjPnHide();
                }

                //�����, ���� ������� ��������� �������
                else if(objectPanel.activeObjectSubpanelType == ObjectSubpanelType.Faction)
                {
                    //���� �������
                    requestComp.objectPE.Unpack(world.Value, out int factionEntity);
                    ref CFaction faction = ref factionPool.Value.Get(factionEntity);

                    //���� ������������� ����������� �������� �������
                    if (requestComp.requestType == ObjectPanelActionRequestType.FactionOverview)
                    {
                        //���������� �������� ������� �������
                        FactionSbpnShowOverviewTab(
                            ref faction,
                            requestComp.isRefresh);
                    }
                }
                //�����, ���� ������� ��������� �������
                else if(objectPanel.activeObjectSubpanelType == ObjectSubpanelType.Region)
                {
                    //���� ������
                    requestComp.objectPE.Unpack(world.Value, out int regionEntity);
                    ref CRegion region = ref regionPool.Value.Get(regionEntity);
                    ref CRegionFO rFO = ref rFOPool.Value.Get(regionEntity);

                    //���� ������������� ����������� �������� �������
                    if (requestComp.requestType == ObjectPanelActionRequestType.RegionOverview)
                    {
                        //���������� �������� ������� �������
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
            //���� ���� ����
            UIGameWindow gameWindow = sOUI.Value.gameWindow;

            //���� �����-���� ������� ������ �������
            if(gameWindow.activeMainPanelType != MainPanelType.None)
            {
                //�������� �
                gameWindow.activeMainPanel.SetActive(false);
            }

            //������ ������ ������� ��������
            gameWindow.objectPanel.gameObject.SetActive(true);

            //��������� � ��� �������� ������� ������
            gameWindow.activeMainPanelType = MainPanelType.Object;
            gameWindow.activeMainPanel = gameWindow.objectPanel.gameObject;
        }

        void ObjPnHide()
        {
            //���� ���� ����
            UIGameWindow gameWindow = sOUI.Value.gameWindow;

            //�������� ������ �������
            gameWindow.objectPanel.gameObject.SetActive(false);

            //���������, ��� ��� �������� ������� ������
            gameWindow.activeMainPanelType = MainPanelType.None;
            gameWindow.activeMainPanel = null;
        }

        void ObjPnShowObjectSubpanel(
            ObjectSubpanelType objectSubpanelType, UIAObjectSubpanel objectSubpanel)
        {
            //���� ������ �������
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //���� �����-���� ��������� �������, �������� �
            if(objectPanel.activeObjectSubpanelType != ObjectSubpanelType.None)
            {
                objectPanel.activeObjectSubpanel.gameObject.SetActive(false);
            }

            //������ ����������� ��������� ��������
            objectSubpanel.gameObject.SetActive(true);

            //��������� � ��� �������� ���������
            objectPanel.activeObjectSubpanelType = objectSubpanelType;
            objectPanel.activeObjectSubpanel = objectSubpanel;

            //������������� ������ ������ ��������� �������������� ����������� ���������
            objectPanel.titlePanel.offsetMax = new Vector2(
                objectSubpanel.parentRect.offsetMax.x, objectPanel.titlePanel.offsetMax.y);
        }

        void ObjPnHideObjectSubpanel()
        {
            //���� ������ �������
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //�������� �������� ���������
            objectPanel.activeObjectSubpanel.gameObject.SetActive(false);

            //���������, ��� ��� �������� ���������
            objectPanel.activeObjectSubpanelType = ObjectSubpanelType.None;
            objectPanel.activeObjectSubpanel = null;

            //������� PE ��������� �������
            objectPanel.activeObjectPE = new();
        }

        #region FactionSubpanel
        void ObjPnShowFactionSubpanel(
            ref RGameObjectPanelAction requestComp)
        {
            //���� �������
            requestComp.objectPE.Unpack(world.Value, out int factionEntity);
            ref CFaction faction = ref factionPool.Value.Get(factionEntity);

            //���� ������ �������
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //���������� ���������
            ObjPnShowObjectSubpanel(
                ObjectSubpanelType.Faction, objectPanel.factionSubpanel);

            //��������� PE �������
            objectPanel.activeObjectPE = faction.selfPE;

            //����������, ��� ��� ��������� �������
            objectPanel.objectName.text = "Faction";


            //���������� �������� �������
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
            //���� ��������� �������
            UIFactionSubpanel factionSubpanel = sOUI.Value.gameWindow.objectPanel.factionSubpanel;

            //���� �������� �������
            Game.Object.Faction.UIOverviewTab overviewTab = factionSubpanel.overviewTab;

            //���������� �������� �������
            factionSubpanel.tabGroup.OnTabSelected(overviewTab.selfTabButton);

            //���� ������������ ����������
            if(isRefresh == true)
            {

            }
        }
        #endregion

        #region RegionSubpanel
        void ObjPnShowRegionSubpanel(
            ref RGameObjectPanelAction requestComp)
        {
            //���� ������
            requestComp.objectPE.Unpack(world.Value, out int regionEntity);
            ref CRegion region = ref regionPool.Value.Get(regionEntity);
            ref CRegionFO rFO = ref rFOPool.Value.Get(regionEntity);

            //���� ������ �������
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //���������� ���������
            ObjPnShowObjectSubpanel(
                ObjectSubpanelType.Region, objectPanel.regionSubpanel);

            //��������� PE �������
            objectPanel.activeObjectPE = region.selfPE;

            //����������, ��� ��� ��������� �������
            objectPanel.objectName.text = region.Index.ToString();


            //���������� �������� �������
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
            //���� ��������� �������
            UIRegionSubpanel regionSubpanel = sOUI.Value.gameWindow.objectPanel.regionSubpanel;

            //���� �������� �������
            Game.Object.Region.UIOverviewTab overviewTab = regionSubpanel.overviewTab;

            //���������� �������� �������
            regionSubpanel.tabGroup.OnTabSelected(overviewTab.selfTabButton);

            //���� ������� ������
            inputData.Value.playerFactionPE.Unpack(world.Value, out int factionEntity);
            ref CFaction faction = ref factionPool.Value.Get(factionEntity);

            //���� ������������ ����������
            if(isRefresh == true)
            {
                //���� ExFRFO ������� ������
                rFO.factionRFOs[faction.selfIndex].fRFOPE.Unpack(world.Value, out int fRFOEntity);
                ref CExplorationFRFO exFRFO = ref exFRFOPool.Value.Get(fRFOEntity);

                //���������� ������� ������������ �������
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
            //������ ����� �������� � ��������� �� ������� ����� ��������� ������ ������
            int eventEntity = world.Value.NewEntity();
            ref EcsGroupSystemState eventComp = ref ecsGroupSystemStatePool.Value.Add(eventEntity);

            //��������� �������� ������ ������ � ������ ���������
            eventComp.Name = systemGroupName;
            eventComp.State = systemGroupState;
        }
    }
}