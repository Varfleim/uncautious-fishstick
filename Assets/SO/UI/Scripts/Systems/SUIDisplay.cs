
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
        //����
        readonly EcsWorldInject world = default;


        //�����
        readonly EcsPoolInject<CRegionHexasphere> rHSPool = default;
        readonly EcsPoolInject<CRegionCore> rCPool = default;
        readonly EcsFilterInject<Inc<CRegionHexasphere, CRegionDisplayedMapPanels>> regionDisplayedMapPanelsFilter = default;
        readonly EcsPoolInject<CRegionDisplayedMapPanels> regionDisplayedMapPanelsPool = default;

        readonly EcsPoolInject<CExplorationRegionFractionObject> exRFOPool = default;

        //�������
        readonly EcsPoolInject<CFaction> factionPool = default;


        //����� �������
        readonly EcsFilterInject<Inc<RGeneralAction>> generalActionRequestFilter = default;
        readonly EcsPoolInject<RGeneralAction> generalActionRequestPool = default;

        readonly EcsPoolInject<EcsGroupSystemState> ecsGroupSystemStatePool = default;

        //������
        readonly EcsCustomInject<UIData> uIData = default;
        readonly EcsCustomInject<MapGenerationData> mapGenerationData = default;
        readonly EcsCustomInject<InputData> inputData = default;
        readonly EcsCustomInject<RuntimeData> runtimeData = default;

        readonly EcsCustomInject<SOUI> sOUI = default;

        public void Init(IEcsSystems systems)
        {
            ListPool<int>.Init();

            //������� ������� � �� �����
            GORegionRenderer.regionRendererPrefab = uIData.Value.regionRendererPrefab;
            CRegionDisplayedMapPanels.mapPanelGroupPrefab = uIData.Value.mapPanelGroup;
            UIRCMainMapPanel.panelPrefab = uIData.Value.rCMainMapPanelPrefab;

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

                //��������� ������ �����
                MapUIMapPanelsUpdate();
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
            //��������� ������� �������� �������
            GameCreatePanelRequest();

            //��������� ������� �������� � ����
            GameAction();

            //��������� ������� �������� � ������ �������
            ObjPnAction();

            //��������� ����������� ���������� ���������� ��������
            GameRefreshUISelfRequest();

            //��������� ������� �������� �������
            GameDeletePanelRequest();
        }

        readonly EcsFilterInject<Inc<RGameCreatePanel>> gameCreatePanelRequestFilter = default;
        readonly EcsPoolInject<RGameCreatePanel> gameCreatePanelRequestPool = default;
        void GameCreatePanelRequest()
        {
            //��� ������� ������� �������� ������
            foreach(int requestEntity in gameCreatePanelRequestFilter.Value)
            {
                //���� ������
                ref RGameCreatePanel requestComp = ref gameCreatePanelRequestPool.Value.Get(requestEntity);

                //���� ������������� �������� ������� ������ ����� �������
                if(requestComp.panelType == GamePanelType.RegionMainMapPanel)
                {
                    //������ ������� ������ ����� �������
                    MapUICreateRegionMainMapPanel(ref requestComp);
                }

                gameCreatePanelRequestPool.Value.Del(requestEntity);
            }
        }

        readonly EcsFilterInject<Inc<SRGameRefreshPanels>> gameRefreshPanelsSelfRequestFilter = default;
        readonly EcsPoolInject<SRGameRefreshPanels> gameRefreshPanelsSelfRequestPool = default;
        void GameRefreshUISelfRequest()
        {
            //��������� ��������� �������� � RC
            GameRefreshUIRegionAndRC();

            //��� ������ �������� � ������������ ���������� �������
            foreach (int entity in gameRefreshPanelsSelfRequestFilter.Value)
            {
                //������� ���������� ���������� �������
                gameRefreshPanelsSelfRequestPool.Value.Del(entity);
            }
        }

        readonly EcsFilterInject<Inc<RGameDeletePanel>> gameDeletePanelRequestFilter = default;
        readonly EcsPoolInject<RGameDeletePanel> gameDeletePanelRequestPool = default;
        void GameDeletePanelRequest()
        {
            //��� ������� ������� �������� ������
            foreach(int requestEntity in gameDeletePanelRequestFilter.Value)
            {
                //���� ������
                ref RGameDeletePanel requestComp = ref gameDeletePanelRequestPool.Value.Get(requestEntity);

                //���� ������������� �������� ������� ������ ����� �������
                if (requestComp.panelType == GamePanelType.RegionMainMapPanel)
                {
                    //������� ������� ������ ����� �������
                    MapUIDeleteRegionMainMapPanel(ref requestComp);
                }

                gameDeletePanelRequestPool.Value.Del(requestEntity);
            }
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

        readonly EcsFilterInject<Inc<CRegionCore, CRegionDisplayedMapPanels, SRGameRefreshPanels>> regionRefreshMapPanelsSelfRequestFilter = default;
        void GameRefreshUIRegionAndRC()
        {
            //��� ������� ������� � ����������� ������� ����� � ������������ ���������� �������
            foreach(int regionEntity in regionRefreshMapPanelsSelfRequestFilter.Value)
            {
                //���� ������, ��������� ������� � ���������� ����������
                ref CRegionCore rC = ref rCPool.Value.Get(regionEntity);
                ref CRegionDisplayedMapPanels regionDisplayedMapPanels = ref regionDisplayedMapPanelsPool.Value.Get(regionEntity);
                ref SRGameRefreshPanels selfRequestComp = ref gameRefreshPanelsSelfRequestPool.Value.Get(regionEntity);

                //���� ������ ����� ������������ ������� ������
                if(regionDisplayedMapPanels.mainMapPanel != null)
                {
                    //��������� �
                    regionDisplayedMapPanels.mainMapPanel.RefreshPanel(ref rC);
                }
            }
        }

        #region MapUI
        void MapUIMapPanelsUpdate()
        {
            //��� ������� ������� � �������� �����
            foreach(int regionEntity in regionDisplayedMapPanelsFilter.Value)
            {
                //���� ��������� ������� �����
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
            //���� �������� �������
            rc.selfPE.Unpack(world.Value, out int regionEntity);

            //���� ������ �� ����� ��������� ������� �����
            if (regionDisplayedMapPanelsPool.Value.Has(regionEntity) == false)
            {
                //���� ��������� ������������ ������� � ��������� ������� ��������� ������� �����
                ref CRegionHexasphere rHS = ref rHSPool.Value.Get(regionEntity);
                ref CRegionDisplayedMapPanels regionDisplayedMapPanels = ref regionDisplayedMapPanelsPool.Value.Add(regionEntity);

                //������ ����� ������ ������ ������� �����
                CRegionDisplayedMapPanels.InstantiateMapPanelGroup(
                    ref rHS, ref regionDisplayedMapPanels,
                    mapGenerationData.Value.hexasphereScale,
                    uIData.Value.mapPanelAltitude);
            } 
        }

        void MapUIDeleteRCMapPanelGroup(
            EcsPackedEntity regionPE)
        {
            //���� �������� ������� � ��������� ������� �����
            regionPE.Unpack(world.Value, out int regionEntity);
            ref CRegionDisplayedMapPanels regionDisplayedMapPanels = ref regionDisplayedMapPanelsPool.Value.Add(regionEntity);

            //���� ������� �� ������� �� ����������
            if(regionDisplayedMapPanels.mainMapPanel == null)
            {
                //�������� ������ �������
                CRegionDisplayedMapPanels.CacheMapPanelGroup(ref regionDisplayedMapPanels);

                //������� � �������� ������� ��������� ������� �����
                regionDisplayedMapPanelsPool.Value.Del(regionEntity);
            }
        }

        void MapUICreateRegionMainMapPanel(
            ref RGameCreatePanel requestComp)
        {
            //���� RC
            requestComp.objectPE.Unpack(world.Value, out int regionEntity);
            ref CRegionCore rC = ref rCPool.Value.Get(regionEntity);

            //������ ��������� ������� �����, ���� ����������
            MapUICreateRCMapPanelGroup(ref rC);

            //���� ��������� ������� �����
            ref CRegionDisplayedMapPanels regionDisplayedMapPanels = ref regionDisplayedMapPanelsPool.Value.Get(regionEntity);

            //������ ������� ������ �����
            UIRCMainMapPanel.InstantiatePanel(
                ref rC, ref regionDisplayedMapPanels);
        }

        void MapUIDeleteRegionMainMapPanel(
            ref RGameDeletePanel requestComp)
        {
            //���� RC � ��������� ������� �����
            requestComp.objectPE.Unpack(world.Value, out int regionEntity);
            ref CRegionDisplayedMapPanels regionDisplayedMapPanels = ref regionDisplayedMapPanelsPool.Value.Get(regionEntity);

            //�������� ������� ������ �����
            UIRCMainMapPanel.CachePanel(
                ref regionDisplayedMapPanels);

            //������� ��������� ������� �����, ���� ����������
            MapUIDeleteRCMapPanelGroup(requestComp.objectPE);
        }

        void ParentAndAlignToRegion(
            GameObject go,
            ref CRegionHexasphere rHS,
            float altitude = 0)
        {
            //���� ����� �������
            Vector3 regionCenter = rHS.GetRegionCenter() * mapGenerationData.Value.hexasphereScale;

            //���� ������ �� ����� ����
            if(altitude != 0)
            {
                Vector3 direction = regionCenter.normalized * altitude;
                go.transform.position = regionCenter + direction;
            }
            //�����
            else
            {
                go.transform.position = regionCenter;
            }

            //����������� ������ � �������
            go.transform.SetParent(rHS.selfObject.transform, true);
            go.transform.LookAt(rHS.selfObject.transform.position);
        }
        #endregion

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
                    ref CRegionHexasphere rHS = ref rHSPool.Value.Get(regionEntity);
                    ref CRegionCore rC = ref rCPool.Value.Get(regionEntity);

                    //���� ������������� ����������� �������� �������
                    if (requestComp.requestType == ObjectPanelActionRequestType.RegionOverview)
                    {
                        //���������� �������� ������� �������
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
            ref CRegionHexasphere rHS = ref rHSPool.Value.Get(regionEntity);
            ref CRegionCore rC = ref rCPool.Value.Get(regionEntity);

            //���� ������ �������
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //���������� ���������
            ObjPnShowObjectSubpanel(
                ObjectSubpanelType.Region, objectPanel.regionSubpanel);

            //��������� PE �������
            objectPanel.activeObjectPE = rHS.selfPE;

            //����������, ��� ��� ��������� �������
            objectPanel.objectName.text = rC.Index.ToString();


            //���������� �������� �������
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
                //���� ExRFO ������� ������
                rC.rFOPEs[faction.selfIndex].rFOPE.Unpack(world.Value, out int rFOEntity);
                ref CExplorationRegionFractionObject exRFO = ref exRFOPool.Value.Get(rFOEntity);

                //���������� ������� ������������ �������
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
            //������ ����� �������� � ��������� �� ������� ����� ��������� ������ ������
            int eventEntity = world.Value.NewEntity();
            ref EcsGroupSystemState eventComp = ref ecsGroupSystemStatePool.Value.Add(eventEntity);

            //��������� �������� ������ ������ � ������ ���������
            eventComp.Name = systemGroupName;
            eventComp.State = systemGroupState;
        }
    }
}