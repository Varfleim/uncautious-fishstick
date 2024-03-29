
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
using SO.UI.Game.GUI.Object.Faction;
using SO.UI.Game.GUI.Object.Region;
using SO.UI.Game.GUI.Object.FleetManager;
using SO.UI.Game.GUI.Object.Events;
using SO.Map;
using SO.Map.Hexasphere;
using SO.Faction;
using SO.Warfare.Fleet;

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

        //������� ����
        readonly EcsPoolInject<CTaskForce> tFPool = default;
        readonly EcsPoolInject<CTaskForceDisplayedGUIPanels> tFDisplayedGUIPanelsPool = default;
        readonly EcsPoolInject<CTaskForceDisplayedMapPanels> tFDisplayedMapPanelsPool = default;

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
            Game.GUI.Object.FleetManager.Fleets.UITaskForceSummaryPanel.panelPrefab = uIData.Value.fMSbpnFleetsTabTaskForceSummaryPanelPrefab;
            UITFMainMapPanel.panelPrefab = uIData.Value.tFMainMapPanelPrefab;

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
                if (requestComp.actionType == GeneralActionType.QuitGame)
                {
                    Debug.LogError("����� �� ����!");

                    //��������� ����
                    Application.Quit();
                }

                generalActionRequestPool.Value.Del(requestEntity);
            }

            //���� ������� ���� ����
            if (sOUI.Value.activeMainWindowType == MainWindowType.Game)
            {
                //��������� ������� � ���� ����
                GameEventCheck();

                //��������� ������ �����
                MapUIMapPanelsUpdate();
            }
            //�����, ���� ������� ���� �������� ����
            else if (sOUI.Value.activeMainWindowType == MainWindowType.MainMenu)
            {
                //��������� ������� � ���� �������� ����
                MainMenuAction();
            }
        }

        void CloseMainWindow()
        {
            //���� �����-���� ������� ���� ���� ��������
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
            //��� ������� ������� �������� � ������� ����
            foreach (int requestEntity in mainMenuActionRequestFilter.Value)
            {
                //���� ������
                ref RMainMenuAction requestComp = ref mainMenuActionRequestPool.Value.Get(requestEntity);

                //���� ������������� �������� ���� ����
                if (requestComp.actionType == MainMenuActionType.OpenGame)
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
        readonly EcsPoolInject<CNonDeletedUI> nonDeletedUIPool = default;

        void GameEventCheck()
        {
            //��������� ������� �������� �������
            GameCreatePanelRequest();

            //��������� ������� �������� � ����
            GameAction();

            //��������� ������� �������� � ������ �������
            ObjectPnAction();

            //��������� ����������� ���������� ���������� ��������
            GameRefreshUISelfRequest();

            //��������� ����������� ���������� ��������� ������� �����
            GameRefreshMapPanelsParentSelfRequest();

            //��������� ������� �������� �������
            GameDeletePanelRequest();
        }

        readonly EcsFilterInject<Inc<RGameCreatePanel>> gameCreatePanelRequestFilter = default;
        readonly EcsPoolInject<RGameCreatePanel> gameCreatePanelRequestPool = default;
        void GameCreatePanelRequest()
        {
            //��� ������� ������� �������� ������
            foreach (int requestEntity in gameCreatePanelRequestFilter.Value)
            {
                //���� ������
                ref RGameCreatePanel requestComp = ref gameCreatePanelRequestPool.Value.Get(requestEntity);

                //���� ������������� �������� ������� ������ ����� �������
                if (requestComp.panelType == GamePanelType.RegionMainMapPanel)
                {
                    //������ ������� ������ ����� �������
                    MapUICreateRegionMainMapPanel(ref requestComp);
                }
                //�����, ���� ������������� �������� �������� ������ ����������� ������ ������� ������ ��������� ������
                else if (requestComp.panelType == GamePanelType.TaskForceSummaryPanelFMSbpnFleetsTab)
                {
                    //������ �������� ������ ������
                    FMSbpnFleetsTabCreateTaskForceSummaryPanel(ref requestComp);
                }
                //�����, ���� ������������� �������� ������� ������ ����� ����������� ������
                else if (requestComp.panelType == GamePanelType.TaskForceMainMapPanel)
                {
                    //������ ������� ������ �����
                    MapUICreateTaskForceMainMapPanel(ref requestComp);
                }

                gameCreatePanelRequestPool.Value.Del(requestEntity);
            }
        }

        readonly EcsFilterInject<Inc<SRGameRefreshPanels>> gameRefreshPanelsSelfRequestFilter = default;
        readonly EcsPoolInject<SRGameRefreshPanels> gameRefreshPanelsSelfRequestPool = default;
        void GameRefreshUISelfRequest()
        {
            //��������� ��������� ��������
            GameRefreshUIRegion();

            //��������� ��������� ����������� �����
            GameRefreshUITaskForce();

            //��� ������ �������� � ������������ ���������� �������
            foreach (int entity in gameRefreshPanelsSelfRequestFilter.Value)
            {
                //������� ���������� 
                gameRefreshPanelsSelfRequestPool.Value.Del(entity);
            }
        }

        readonly EcsFilterInject<Inc<SRRefreshMapPanelsParent>> refreshMapPanelsParentSelfRequestFilter = default;
        readonly EcsPoolInject<SRRefreshMapPanelsParent> refreshMapPanelsParentSelfRequestPool = default;
        void GameRefreshMapPanelsParentSelfRequest()
        {
            //��������� ������ ����������� �����
            GameRefreshTaskForceMapPanelsParent();

            //��� ������ �������� � ������������ ���������� �������� ������� �����
            foreach(int entity in refreshMapPanelsParentSelfRequestFilter.Value)
            {
                //������� ����������
                refreshMapPanelsParentSelfRequestPool.Value.Del(entity);
            }
        }

        readonly EcsFilterInject<Inc<RGameDeletePanel>> gameDeletePanelRequestFilter = default;
        readonly EcsPoolInject<RGameDeletePanel> gameDeletePanelRequestPool = default;
        void GameDeletePanelRequest()
        {
            //��� ������� ������� �������� ������
            foreach (int requestEntity in gameDeletePanelRequestFilter.Value)
            {
                //���� ������
                ref RGameDeletePanel requestComp = ref gameDeletePanelRequestPool.Value.Get(requestEntity);

                //���� ������������� �������� ������� ������ ����� �������
                if (requestComp.panelType == GamePanelType.RegionMainMapPanel)
                {
                    //������� ������ �����
                    MapUIDeleteRegionMainMapPanel(requestComp.objectPE);
                }
                //�����, ���� ������������� �������� �������� ������ ����������� ������ ������� ������ ��������� ������
                else if (requestComp.panelType == GamePanelType.TaskForceSummaryPanelFMSbpnFleetsTab)
                {
                    //������� �������� ������
                    FMSbpnFleetsTabDeleteTaskForceSummaryPanel(requestComp.objectPE);
                }
                //�����, ���� ������������� �������� ������� ������ ����� ����������� ������
                else if(requestComp.panelType == GamePanelType.TaskForceMainMapPanel)
                {
                    //������� ������ �����
                    MapUIDeleteTaskForceMainMapPanel(requestComp.objectPE);
                }

                gameDeletePanelRequestPool.Value.Del(requestEntity);
            }
        }

        readonly EcsFilterInject<Inc<RGameAction>> gameActionRequestFilter = default;
        readonly EcsPoolInject<RGameAction> gameActionRequestPool = default;
        void GameAction()
        {
            //��� ������� ������� �������� � ����
            foreach (int requestEntity in gameActionRequestFilter.Value)
            {
                //���� ������
                ref RGameAction requestComp = ref gameActionRequestPool.Value.Get(requestEntity);

                //��������� ��������� �����
                if (requestComp.actionType == GameActionType.PauseOn || requestComp.actionType == GameActionType.PauseOff)
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
            if (pauseMode == GameActionType.PauseOn)
            {
                //���������, ��� ���� ���������
                runtimeData.Value.isGameActive = false;
            }
            //�����
            else if (pauseMode == GameActionType.PauseOff)
            {
                //���������, ��� ���� �������
                runtimeData.Value.isGameActive = true;
            }
        }

        readonly EcsFilterInject<Inc<CRegionCore, CRegionDisplayedMapPanels, SRGameRefreshPanels>> regionRefreshMapPanelsSelfRequestFilter = default;
        void GameRefreshUIRegion()
        {
            //��� ������� ������� � ����������� ������� ����� � ������������ ���������� �������
            foreach (int regionEntity in regionRefreshMapPanelsSelfRequestFilter.Value)
            {
                //���� ������ � ��������� �������
                ref CRegionCore rC = ref rCPool.Value.Get(regionEntity);
                ref CRegionDisplayedMapPanels regionDisplayedMapPanels = ref regionDisplayedMapPanelsPool.Value.Get(regionEntity);

                //���� ������ ����� ������������ ������� ������
                if (regionDisplayedMapPanels.mainMapPanel != null)
                {
                    //��������� �
                    regionDisplayedMapPanels.mainMapPanel.RefreshPanel(ref rC);
                }
            }
        }

        readonly EcsFilterInject<Inc<CTaskForce, CTaskForceDisplayedGUIPanels, SRGameRefreshPanels>> tFRefreshGUIPanelsSelfRequestFilter = default;
        readonly EcsFilterInject<Inc<CTaskForce, CTaskForceDisplayedMapPanels, SRGameRefreshPanels>> tFRefreshMapPanelsSelfRequestFilter = default;
        void GameRefreshUITaskForce()
        {
            //��� ������ ����������� ������ � ����������� ������� GUI � ������������ ���������� �������
            foreach (int tFEntity in tFRefreshGUIPanelsSelfRequestFilter.Value)
            {
                //���� ����������� ������ � ��������� �������
                ref CTaskForce tF = ref tFPool.Value.Get(tFEntity);
                ref CTaskForceDisplayedGUIPanels tFDisplayedGUIPanels = ref tFDisplayedGUIPanelsPool.Value.Get(tFEntity);

                //���� ������ ����� ������������ �������� ������ ������� ������ ��������� ������
                if (tFDisplayedGUIPanels.fMSbpnFleetsTabSummaryPanel != null)
                {
                    //��������� �
                    tFDisplayedGUIPanels.fMSbpnFleetsTabSummaryPanel.RefreshPanel(ref tF);
                }
            }

            //��� ������ ������ � ����������� ������� ����� � ������������ ���������� �������
            foreach(int tFEntity in tFRefreshMapPanelsSelfRequestFilter.Value)
            {
                //���� ����������� ������ � ��������� �������
                ref CTaskForce tF = ref tFPool.Value.Get(tFEntity);
                ref CTaskForceDisplayedMapPanels tFDisplayedMapPanels = ref tFDisplayedMapPanelsPool.Value.Get(tFEntity);

                //���� ������ ����� ������������ ������� ������ �����
                if(tFDisplayedMapPanels.mainMapPanel != null)
                {
                    //��������� �
                    tFDisplayedMapPanels.mainMapPanel.RefreshPanel(ref tF);
                }
            }
        }

        readonly EcsFilterInject<Inc<CTaskForce, CTaskForceDisplayedMapPanels, SRRefreshMapPanelsParent>> tFRefreshMapPanelsParentSelfRequestFilter = default;
        void GameRefreshTaskForceMapPanelsParent()
        {
            //��� ������ ����������� ������ � ����������� ������� ����� � ������������ ���������� ���������
            foreach(int tFEntity in tFRefreshMapPanelsParentSelfRequestFilter.Value)
            {
                //���� ������ � ��������� ������� �����
                ref CTaskForce tF = ref tFPool.Value.Get(tFEntity);
                ref CTaskForceDisplayedMapPanels tFDisplayedMapPanels = ref tFDisplayedMapPanelsPool.Value.Get(tFEntity);
                
                //���� ������ ����� ������������ ������� ������ �����
                if(tFDisplayedMapPanels.mainMapPanel != null)
                {
                    //��������� � ��������
                    MapUISetParentTaskForceMainMapPanel(ref tF, ref tFDisplayedMapPanels);
                }
            }
        }

        #region MapUI
        void MapUIMapPanelsUpdate()
        {
            //��� ������� ������� � �������� �����
            foreach (int regionEntity in regionDisplayedMapPanelsFilter.Value)
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

        void MapUICreateRegionDisplayedMapPanels(
            ref CRegionHexasphere rHS)
        {
            //���� �������� �������
            rHS.selfPE.Unpack(world.Value, out int regionEntity);

            //���� ������ �� ����� ���������� ������� �����
            if (regionDisplayedMapPanelsPool.Value.Has(regionEntity) == false)
            {
                //��������� ������� ��������� ������� �����
                ref CRegionDisplayedMapPanels regionDisplayedMapPanels = ref regionDisplayedMapPanelsPool.Value.Add(regionEntity);

                //��������� ������ ����������
                regionDisplayedMapPanels = new(0);

                //������ ����� ������ ������ ������� �����
                CRegionDisplayedMapPanels.InstantiateMapPanelGroup(
                    ref rHS, ref regionDisplayedMapPanels,
                    mapGenerationData.Value.hexasphereScale,
                    uIData.Value.mapPanelAltitude);
            }
        }

        void MapUIDeleteRegionDisplayedMapPanels(
            EcsPackedEntity regionPE)
        {
            //���� �������� ������� � ��������� ������� �����
            regionPE.Unpack(world.Value, out int regionEntity);
            ref CRegionDisplayedMapPanels regionDisplayedMapPanels = ref regionDisplayedMapPanelsPool.Value.Get(regionEntity);

            //���� ������� �� ������� �� ����������
            if (regionDisplayedMapPanels.mainMapPanel == null)
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
            //���� RC � ��������� ������������ �������
            requestComp.objectPE.Unpack(world.Value, out int regionEntity);
            ref CRegionCore rC = ref rCPool.Value.Get(regionEntity);
            ref CRegionHexasphere rHS = ref rHSPool.Value.Get(regionEntity);

            //������ ��������� ������� �����, ���� ����������
            MapUICreateRegionDisplayedMapPanels(ref rHS);

            //���� ��������� ������� �����
            ref CRegionDisplayedMapPanels regionDisplayedMapPanels = ref regionDisplayedMapPanelsPool.Value.Get(regionEntity);

            //������ ������� ������ �����
            UIRCMainMapPanel.InstantiatePanel(
                ref rC, ref regionDisplayedMapPanels);
        }

        void MapUIDeleteRegionMainMapPanel(
            EcsPackedEntity regionPE)
        {
            //���� ��������� ������� �����
            regionPE.Unpack(world.Value, out int regionEntity);
            ref CRegionDisplayedMapPanels regionDisplayedMapPanels = ref regionDisplayedMapPanelsPool.Value.Get(regionEntity);

            //�������� ������� ������ �����
            UIRCMainMapPanel.CachePanel(ref regionDisplayedMapPanels);

            //������� ��������� ������� �����, ���� ����������
            MapUIDeleteRegionDisplayedMapPanels(regionPE);
        }

        void MapUICreateTaskForceDisplayedMapPanels(
            ref CTaskForce tF)
        {
            //���� �������� ����������� ������
            tF.selfPE.Unpack(world.Value, out int tFEntity);

            //���� ������ �� ����� ���������� ������� �����
            if (tFDisplayedMapPanelsPool.Value.Has(tFEntity) == false)
            {
                //��������� ������ ��������� ������� �����
                ref CTaskForceDisplayedMapPanels tFDisplayedMapPanels = ref tFDisplayedMapPanelsPool.Value.Add(tFEntity);
            }
        }

        void MapUIDeleteTaskForceDisplayedMapPanels(
            EcsPackedEntity tFPE)
        {
            //���� �������� ����������� ������ � ��������� ������� �����
            tFPE.Unpack(world.Value, out int tFEntity);
            ref CTaskForceDisplayedMapPanels tFDisplayedMapPanels = ref tFDisplayedMapPanelsPool.Value.Get(tFEntity);

            //���� ������� �� ������� �� ����������
            if (tFDisplayedMapPanels.mainMapPanel == null)
            {
                //������� � �������� ������ ��������� ������� �����
                tFDisplayedMapPanelsPool.Value.Del(tFEntity);
            }
        }

        void MapUICreateTaskForceMainMapPanel(
            ref RGameCreatePanel requestComp)
        {
            //���� ����������� ������
            requestComp.objectPE.Unpack(world.Value, out int tFEntity);
            ref CTaskForce tF = ref tFPool.Value.Get(tFEntity);

            //������ ��������� ������� �����, ���� ����������
            MapUICreateTaskForceDisplayedMapPanels(ref tF);

            //���� ��������� ������� �����
            ref CTaskForceDisplayedMapPanels tFDisplayedMapPanels = ref tFDisplayedMapPanelsPool.Value.Get(tFEntity);

            //������ ������� ������ �����
            UITFMainMapPanel.InstantiatePanel(
                ref tF, ref tFDisplayedMapPanels);

            //����������� ������ � �������� ������� ������
            MapUISetParentTaskForceMainMapPanel(ref tF, ref tFDisplayedMapPanels);
        }

        void MapUISetParentTaskForceMainMapPanel(
            ref CTaskForce tF, ref CTaskForceDisplayedMapPanels tFDisplayedMapPanels)
        {
            //���� ���������� ������ ����������� ������ ����������
            if(tF.previousRegionPE.Unpack(world.Value, out int previousRegionEntity))
            {
                //���� ��������� ������� ����� ����������� ������� 
                ref CRegionDisplayedMapPanels previousRegionDisplayedMapPanels = ref regionDisplayedMapPanelsPool.Value.Get(previousRegionEntity);

                //���������� ������ ������ �� ����
                previousRegionDisplayedMapPanels.CancelParentTaskForceMainMapPanel(tFDisplayedMapPanels.mainMapPanel);
            }

            //��������� ������� ������ � ���������� ������� ����� ������
            tFDisplayedMapPanels.currentRegionPE = tF.currentRegionPE;

            //���� ������� ������ ������
            tF.currentRegionPE.Unpack(world.Value, out int currentRegionEntity);
            ref CRegionHexasphere rHS = ref rHSPool.Value.Get(currentRegionEntity);

            //������ ��������� ������� �����, ���� ����������
            MapUICreateRegionDisplayedMapPanels(ref rHS);

            //���� ��������� ������� �����
            ref CRegionDisplayedMapPanels currentRegionDisplayedMapPanels = ref regionDisplayedMapPanelsPool.Value.Get(currentRegionEntity);

            //����������� � ���� ������� ������ ����� ������
            currentRegionDisplayedMapPanels.SetParentTaskForceMainMapPanel(tFDisplayedMapPanels.mainMapPanel);
        }

        void MapUIDeleteTaskForceMainMapPanel(
            EcsPackedEntity tFPE)
        {
            //���� ��������� ������� �����
            tFPE.Unpack(world.Value, out int tFEntity);
            ref CTaskForceDisplayedMapPanels tFDisplayedMapPanels = ref tFDisplayedMapPanelsPool.Value.Get(tFEntity);

            //���� ��������� ������� ����� �������� ������� ������
            tFDisplayedMapPanels.currentRegionPE.Unpack(world.Value, out int currentRegionEntity);
            ref CRegionDisplayedMapPanels regionDisplayedMapPanels = ref regionDisplayedMapPanelsPool.Value.Get(currentRegionEntity);

            //���������� ������ ������ �� ����
            regionDisplayedMapPanels.CancelParentTaskForceMainMapPanel(tFDisplayedMapPanels.mainMapPanel);

            //�������� ������� ������ �����
            UITFMainMapPanel.CachePanel(ref tFDisplayedMapPanels);

            //������� ��������� ������� ����� � ������, ���� ����������
            MapUIDeleteTaskForceDisplayedMapPanels(tFPE);

            //������� ��������� ������� ����� � �������, ���� ����������
            MapUIDeleteRegionDisplayedMapPanels(tFDisplayedMapPanels.currentRegionPE);
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

        #region GUI
        readonly EcsFilterInject<Inc<CTaskForce, CNonDeletedUI>> taskForceNonDeletedUIFilter = default;
        readonly EcsFilterInject<Inc<CTaskForceDisplayedGUIPanels, CNonDeletedUI>> taskForceDisplayedNonDeletedGUIFilter = default;
        readonly EcsFilterInject<Inc<CTaskForceDisplayedGUIPanels>, Exc<CNonDeletedUI>> taskForceDisplayedDeletedGUIFilter = default;

        void GUIOpenPanel(
            GameObject currentPanel, MainPanelType currentPanelType,
            out bool isSamePanel)
        {
            //�������� �� ��������� ������������
            isSamePanel = false;

            //���� ���� ����
            UIGameWindow gameWindow = sOUI.Value.gameWindow;

            //���� ������� ����������� ������
            if(gameWindow.activeMainPanelType == currentPanelType)
            {
                //��������, ��� ���� ������� �� �� ������
                isSamePanel = true;
            }

            //���� ���� ������� �� �� ������
            if (isSamePanel == true)
            {

            }
            //�����
            else
            {
                //���������� ����������� ������

                //���� �����-���� ������ ���� �������
                if (gameWindow.activeMainPanelType != MainPanelType.None)
                {
                    //�������� �
                    gameWindow.activeMainPanel.SetActive(false);
                }

                //������ ����������� ������ ��������
                currentPanel.SetActive(true);

                //��������� � ��� �������� ������
                gameWindow.activeMainPanelType = currentPanelType;
                gameWindow.activeMainPanel = currentPanel;
            }
        }

        void GUICreateTFUIPanels(
            ref CTaskForce tF)
        {
            //���� �������� ������
            tF.selfPE.Unpack(world.Value, out int tFEntity);

            //���� ������ �� ����� ���������� ������� GUI
            if (tFDisplayedGUIPanelsPool.Value.Has(tFEntity) == false)
            {
                //��������� �� ��������� ������� GUI
                ref CTaskForceDisplayedGUIPanels tFDisplayedGUIPanels = ref tFDisplayedGUIPanelsPool.Value.Add(tFEntity);
            }
        }

        void GUIDeleteTaskForceGUIPanels(
            EcsPackedEntity tFPE)
        {
            //���� �������� ����������� ������ � ��������� ������� GUI
            tFPE.Unpack(world.Value, out int tFEntity);
            ref CTaskForceDisplayedGUIPanels tFDisplayedGUIPanels = ref tFDisplayedGUIPanelsPool.Value.Get(tFEntity);

            //���� ������� �� ������� �� ����������
            if (tFDisplayedGUIPanels.fMSbpnFleetsTabSummaryPanel == null)
            {
                //������� � �������� ������ ��������� ������� GUI
                tFDisplayedGUIPanelsPool.Value.Del(tFEntity);
            }
        }

        #region ObjectPanel
        readonly EcsFilterInject<Inc<RGameObjectPanelAction>> gameObjectPanelRequestFilter = default;
        readonly EcsPoolInject<RGameObjectPanelAction> gameObjectPanelRequestPool = default;
        void ObjectPnAction()
        {
            //��� ������� ������� �������� � ����
            foreach (int requestEntity in gameObjectPanelRequestFilter.Value)
            {
                //���� ������
                ref RGameObjectPanelAction requestComp = ref gameObjectPanelRequestPool.Value.Get(requestEntity);

                //���� ������������� �������� ������ �������
                if (requestComp.requestType == ObjectPanelActionRequestType.Close)
                {
                    //��������� ������ �������
                    ObjectPnClose();
                }

                //�����, ���� ������������� ����������� ������� �������
                else if(requestComp.requestType >= ObjectPanelActionRequestType.FactionOverview
                    && requestComp.requestType <= ObjectPanelActionRequestType.FactionOverview)
                {
                    //���� �������
                    requestComp.objectPE.Unpack(world.Value, out int factionEntity);
                    ref CFaction faction = ref factionPool.Value.Get(factionEntity);

                    //���� ������������� ����������� �������� �������
                    if (requestComp.requestType == ObjectPanelActionRequestType.FactionOverview)
                    {
                        //���������� �������� ������� �������
                        FactionSbpnShowOverviewTab(ref faction);
                    }
                }
                //�����, ���� ������������� ����������� ������� �������
                else if(requestComp.requestType >= ObjectPanelActionRequestType.RegionOverview
                    && requestComp.requestType <= ObjectPanelActionRequestType.RegionOverview)
                {
                    //���� ������
                    requestComp.objectPE.Unpack(world.Value, out int regionEntity);
                    ref CRegionHexasphere rHS = ref rHSPool.Value.Get(regionEntity);
                    ref CRegionCore rC = ref rCPool.Value.Get(regionEntity);

                    //���� ������������� ����������� �������� �������
                    if (requestComp.requestType == ObjectPanelActionRequestType.RegionOverview)
                    {
                        //���������� �������� ������� �������
                        RegionSbpnShowOverviewTab(ref rHS, ref rC);
                    }
                }
                //�����, ���� ������������� ����������� ������� ��������� ������
                else if(requestComp.requestType >= ObjectPanelActionRequestType.FleetManagerFleets
                    && requestComp.requestType <= ObjectPanelActionRequestType.FleetManagerFleets)
                {
                    //���� �������
                    requestComp.objectPE.Unpack(world.Value, out int factionEntity);
                    ref CFaction faction = ref factionPool.Value.Get(factionEntity);

                    //���� ������������� ����������� ������� ������
                    if (requestComp.requestType == ObjectPanelActionRequestType.FleetManagerFleets)
                    {
                        //���������� ������� ������
                        FleetManagerSbpnShowFleetsTab(ref faction);
                    }
                }

                gameObjectPanelRequestPool.Value.Del(requestEntity);
            }
        }

        void ObjectPnClose()
        {
            //���� ������ �������
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //���� ������� ��������� �������
            if (objectPanel.activeSubpanelType == ObjectSubpanelType.Faction)
            {
                //�������� �������� �������
                objectPanel.factionSubpanel.HideActiveTab();
            }
            //�����, ���� ������� ��������� �������
            else if (objectPanel.activeSubpanelType == ObjectSubpanelType.Region)
            {
                //�������� �������� �������
                objectPanel.regionSubpanel.HideActiveTab();
            }
            //�����, ���� ������� ��������� ��������� ������
            else if (objectPanel.activeSubpanelType == ObjectSubpanelType.FleetManager)
            {
                //�������� �������� �������
                objectPanel.fleetManagerSubpanel.HideActiveTab();
            }

            //�������� �������� ��������� �������
            objectPanel.HideActiveSubpanel();

            //�������� �������� ������� ������
            sOUI.Value.gameWindow.HideActivePanel();
        }

        void ObjectPnOpenSubpanel(
            UIAObjectSubpanel currentSubpanel, ObjectSubpanelType currentSubpanelType,
            out bool isSamePanel,
            out bool isSameSubpanel)
        {
            //�������� �� ��������� ������������
            isSameSubpanel = false;

            //���� ������ �������
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //���������� ������ �������, ���� ����������
            GUIOpenPanel(
                objectPanel.gameObject, MainPanelType.Object,
                out isSamePanel);

            //���� ������� ����������� ���������
            if(objectPanel.activeSubpanelType == currentSubpanelType)
            {
                //���� ���� ������� �� �� ������
                if(isSamePanel == true)
                {
                    //��������, ��� ���� ������� �� �� ���������
                    isSameSubpanel = true;
                }
            }

            //���� ���� ������� �� �� ������
            if (isSamePanel == true)
            {

            }
            //�����
            else
            {

            }

            //���� ���� ������� �� �� ��������� 
            if (isSameSubpanel == true)
            {

            }
            //�����
            else
            {
                //���������� ����������� ���������

                //���� �����-���� ��������� ���� �������, �������� �
                if (objectPanel.activeSubpanelType != ObjectSubpanelType.None)
                {
                    objectPanel.activeSubpanel.gameObject.SetActive(false);
                }

                //������ ����������� ��������� ��������
                currentSubpanel.gameObject.SetActive(true);

                //��������� � ��� �������� ���������
                objectPanel.activeSubpanelType = currentSubpanelType;
                objectPanel.activeSubpanel = currentSubpanel;

                //������������� ������ ������ ��������� �������������� ����������� ���������
                objectPanel.titlePanel.offsetMax = new Vector2(
                    currentSubpanel.parentRect.offsetMax.x, objectPanel.titlePanel.offsetMax.y);
            }
        }

        #region FactionSubpanel
        void FactionSbpnShowTab(
            UIASubpanelTab currentFactionTab,
            ref CFaction currentFaction,
            out bool isSamePanel,
            out bool isSameSubpanel,
            out bool isSameTab,
            out bool isSameObject)
        {
            //�������� �� ��������� ������������
            isSameTab = false;
            isSameObject = false;

            //���� ������ �������
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //���� ��������� �������
            UIFactionSubpanel factionSubpanel = objectPanel.factionSubpanel;

            //���������� ��������� �������, ���� ����������
            ObjectPnOpenSubpanel(
                factionSubpanel, ObjectSubpanelType.Faction,
                out isSamePanel,
                out isSameSubpanel);

            //���� ������� ����������� �������
            if (factionSubpanel.activeTab == currentFactionTab)
            {
                //���� ���� ������� �� �� ���������
                if(isSameSubpanel == true)
                {
                    //��������, ��� ���� ������� �� �� �������
                    isSameTab = true;

                    //���� ������� ���� ������� ��� ��� �� �������
                    if (factionSubpanel.activeTab.objectPE.EqualsTo(currentFaction.selfPE) == true)
                    {
                        //�� ��������, ��� ��� �������� ��� �� ������
                        isSameObject = true;
                    }
                }
            }

            //���� ���� ������� �� �� ������
            if (isSamePanel == true)
            {

            }
            //�����
            else
            {

            }

            //���� ���� ������� �� �� ��������� 
            if (isSameSubpanel == true)
            {

            }
            //�����
            else
            {

            }

            //���� ���� ������� �� �� �������
            if (isSameTab == true)
            {

            }
            //�����
            else
            {
                //���������� ����������� �������
                factionSubpanel.tabGroup.OnTabSelected(currentFactionTab.selfTabButton);

                //��������� � ��� �������� �������
                factionSubpanel.activeTab = currentFactionTab;
            }

            //���� ��� �������� ��� �� ������
            if (isSameObject == true)
            {

            }
            //�����
            else
            {
                //��������� PE ������� �������
                factionSubpanel.activeTab.objectPE = currentFaction.selfPE;

                //���������� �������� ������ - �������� �������
                objectPanel.objectName.text = currentFaction.selfIndex.ToString();
            }
        }

        void FactionSbpnShowOverviewTab(
            ref CFaction faction)
        {
            //���� ��������� �������
            UIFactionSubpanel factionSubpanel = sOUI.Value.gameWindow.objectPanel.factionSubpanel;

            //���� �������� �������
            Game.GUI.Object.Faction.UIOverviewTab overviewTab = factionSubpanel.overviewTab;

            //���������� �������� �������
            FactionSbpnShowTab(
                overviewTab,
                ref faction,
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
            //�������� �� ��������� ������������
            isSameTab = false;
            isSameObject = false;

            //���� ������ �������
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //���� ��������� �������
            UIRegionSubpanel regionSubpanel = objectPanel.regionSubpanel;

            //���������� ��������� �������, ���� ����������
            ObjectPnOpenSubpanel(
                regionSubpanel, ObjectSubpanelType.Region,
                out isSamePanel,
                out isSameSubpanel);

            //���� ������� ����������� �������
            if (regionSubpanel.activeTab == currentRegionTab)
            {
                //���� ���� ������� �� �� ���������
                if (isSameSubpanel == true)
                {
                    //��������, ��� ���� ������� �� �� �������
                    isSameTab = true;

                    //���� ������� ���� ������� ��� ���� �� �������
                    if (regionSubpanel.activeTab.objectPE.EqualsTo(currentRC.selfPE) == true)
                    {
                        //�� ��������, ��� ��� �������� ��� �� ������
                        isSameObject = true;
                    }
                }
            }

            //���� ���� ������� �� �� ������
            if (isSamePanel == true)
            {

            }
            //�����
            else
            {

            }

            //���� ���� ������� �� �� ��������� 
            if (isSameSubpanel == true)
            {

            }
            //�����
            else
            {

            }

            //���� ���� ������� �� �� �������
            if (isSameTab == true)
            {

            }
            //�����
            else
            {
                //���������� ����������� �������
                regionSubpanel.tabGroup.OnTabSelected(currentRegionTab.selfTabButton);

                //��������� � ��� �������� �������
                regionSubpanel.activeTab = currentRegionTab;
            }

            //���� ��� �������� ��� �� ������
            if (isSameObject == true)
            {

            }
            //�����
            else
            {
                //��������� PE �������� �������
                regionSubpanel.activeTab.objectPE = currentRC.selfPE;

                //���������� �������� ������ - �������� �������
                objectPanel.objectName.text = currentRC.Index.ToString();
            }
        }

        void RegionSbpnShowOverviewTab(
            ref CRegionHexasphere rHS, ref CRegionCore rC)
        {
            //���� ��������� �������
            UIRegionSubpanel regionSubpanel = sOUI.Value.gameWindow.objectPanel.regionSubpanel;

            //���� �������� �������
            Game.GUI.Object.Region.UIOverviewTab overviewTab = regionSubpanel.overviewTab;

            //���������� �������� �������
            RegionSbpnShowTab(
                overviewTab,
                ref rC,
                out bool isSamePanel,
                out bool isSameSubpanel,
                out bool isSameTab,
                out bool isSameObject);

            //���� ������� ������
            inputData.Value.playerFactionPE.Unpack(world.Value, out int factionEntity);
            ref CFaction faction = ref factionPool.Value.Get(factionEntity);

            //���� ExRFO ������� ������
            rC.rFOPEs[faction.selfIndex].rFOPE.Unpack(world.Value, out int rFOEntity);
            ref CExplorationRegionFractionObject exRFO = ref exRFOPool.Value.Get(rFOEntity);

            //���������� ������� ������������ �������
            overviewTab.explorationLevel.text = exRFO.explorationLevel.ToString();
        }
        #endregion

        #region FleetManager
        void FleetManagerSbpnShowTab(
            UIASubpanelTab currentFleetManagerTab,
            ref CFaction currentFaction,
            out bool isSamePanel,
            out bool isSameSubpanel,
            out bool isSameTab,
            out bool isSameObject)
        {
            //�������� �� ��������� ������������
            isSameTab = false;
            isSameObject = false;

            //���� ������ �������
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //���� ��������� ��������� ������
            UIFleetManagerSubpanel fleetManagerSubpanel = objectPanel.fleetManagerSubpanel;

            //���������� ��������� ��������� ������, ���� ����������
            ObjectPnOpenSubpanel(
                fleetManagerSubpanel, ObjectSubpanelType.FleetManager,
                out isSamePanel,
                out isSameSubpanel);

            //���� ������� ����������� �������
            if(fleetManagerSubpanel.activeTab == currentFleetManagerTab)
            {
                //���� ���� ������� �� �� ���������
                if (isSameSubpanel == true)
                {
                    //��������, ��� ���� ������� �� �� �������
                    isSameTab = true;

                    //���� ������� ���� ������� ��� ��� �� �������
                    if (fleetManagerSubpanel.activeTab.objectPE.EqualsTo(currentFaction.selfPE) == true)
                    {
                        //�� ��������, ��� ��� �������� ��� �� ������
                        isSameObject = true;
                    }
                }
            }

            //���� ���� ������� �� �� ������
            if (isSamePanel == true)
            {
                
            }
            //�����
            else
            {

            }

            //���� ���� ������� �� �� ��������� 
            if (isSameSubpanel == true)
            {
                
            }
            //�����
            else
            {
                //���������� �������� ������ - �������� ������
                objectPanel.objectName.text = "FleetManager";
            }

            //���� ���� ������� �� �� �������
            if (isSameTab == true)
            {
                
            }
            //�����
            else
            {
                //���������� ����������� �������
                fleetManagerSubpanel.tabGroup.OnTabSelected(currentFleetManagerTab.selfTabButton);

                //��������� � ��� �������� �������
                fleetManagerSubpanel.activeTab = currentFleetManagerTab;
            }

            //���� ��� �������� ��� �� ������
            if (isSameObject == true)
            {

            }
            //�����
            else
            {
                //��������� PE ������� �������
                fleetManagerSubpanel.activeTab.objectPE = currentFaction.selfPE;
            }
        }

        void FleetManagerSbpnShowFleetsTab(
            ref CFaction faction)
        {
            //���� ��������� ��������� ������
            UIFleetManagerSubpanel fleetManagerSubpanel = sOUI.Value.gameWindow.objectPanel.fleetManagerSubpanel;

            //���� ������� ������
            UIFleetsTab fleetsTab = fleetManagerSubpanel.fleetsTab;

            //���������� ������� ������
            FleetManagerSbpnShowTab(
                fleetsTab,
                ref faction,
                out bool isSamePanel,
                out bool isSameSubpanel,
                out bool isSameTab,
                out bool isSameObject);

            //���� ���� ������� �� �� ������
            if (isSamePanel == true)
            {

            }
            //�����
            else
            {

            }

            //���� ���� ������� �� �� ��������� 
            if (isSameSubpanel == true)
            {

            }
            //�����
            else
            {

            }

            //���� ���� ������� �� �� �������
            if (isSameTab == true)
            {

            }
            //�����
            else
            {

            }

            //���� ��� �������� ��� �� ������
            if (isSameObject == true)
            {

            }
            //�����
            else
            {
                //���������, ����� ����������� ������ ������ ���� ���������� � ���� ������
                //��� ������ ����������� ������ �������
                for(int a = 0; a < faction.ownedTaskForces.Count; a++)
                {
                    //���� �������� ����������� ������ � ��������� ��������� "����������� UI"
                    faction.ownedTaskForces[a].Unpack(world.Value, out int tFEntity);
                    ref CNonDeletedUI tFNonDeletedUI = ref nonDeletedUIPool.Value.Add(tFEntity);
                }

                //������� �������� ������ ������� ������ � ������ ������, ������� �� ������ ���� � ������
                //��� ������ ������ � ������������� �������� GUI, �� ��� "������������ UI"
                foreach(int tFEntity in taskForceDisplayedDeletedGUIFilter.Value)
                {
                    //���� ��������� ������� GUI
                    ref CTaskForceDisplayedGUIPanels tFDisplayedGUIPanels = ref tFDisplayedGUIPanelsPool.Value.Get(tFEntity);

                    //���� � ������ ���� �������� ������ ������� ������
                    if(tFDisplayedGUIPanels.fMSbpnFleetsTabSummaryPanel != null)
                    {
                        //������� �������� ������
                        FMSbpnFleetsTabDeleteTaskForceSummaryPanel(tFDisplayedGUIPanels.fMSbpnFleetsTabSummaryPanel.selfPE);
                    }
                }

                //��������� ������ �����, ������� ��� ����� �������� ������
                //��� ������ ������ � �������� GUI � "����������� UI"
                foreach (int tFEntity in taskForceDisplayedNonDeletedGUIFilter.Value)
                {
                    //���� ������ � ��������� ������� GUI
                    ref CTaskForce tF = ref tFPool.Value.Get(tFEntity);
                    ref CTaskForceDisplayedGUIPanels tFDisplayedGUIPanels = ref tFDisplayedGUIPanelsPool.Value.Get(tFEntity);

                    //���� � ������ ���� �������� ������ ������� ������
                    if (tFDisplayedGUIPanels.fMSbpnFleetsTabSummaryPanel != null)
                    {
                        //��������� � ������
                        tFDisplayedGUIPanels.fMSbpnFleetsTabSummaryPanel.RefreshPanel(ref tF);

                        //� ������� � ������ "����������� UI"
                        nonDeletedUIPool.Value.Del(tFEntity);
                    }
                    //����� ������ ������ �����������, �� ��� ��������� �� ��������� ����, � ������� ������ ���������
                }

                //������ �������� ������ ��� �����, ������� �� ����� ��, �� ������
                //��� ������ ������ � "����������� UI"
                foreach(int tFEntity in taskForceNonDeletedUIFilter.Value)
                {
                    //���� ������
                    ref CTaskForce tF = ref tFPool.Value.Get(tFEntity);

                    //� ������ ������ � ������ �� ����� ������������ �������� ������ ������� ������,
                    //������� ����� ��������� � ��������, �� ����������

                    //������ �������� ������
                    FMSbpnFleetsTabCreateTaskForceSummaryPanel(ref tF);

                    //� ������� � ������ "����������� UI"
                    nonDeletedUIPool.Value.Del(tFEntity);
                }
            }
        }

        void FMSbpnFleetsTabCreateTaskForceSummaryPanel(
            ref RGameCreatePanel requestComp)
        {
            //���� ����������� ������
            requestComp.objectPE.Unpack(world.Value, out int tFEntity);
            ref CTaskForce tF = ref tFPool.Value.Get(tFEntity);

            //������ �������� ������
            FMSbpnFleetsTabCreateTaskForceSummaryPanel(ref tF);
        }

        void FMSbpnFleetsTabCreateTaskForceSummaryPanel(
            ref CTaskForce tF)
        {
            //���� �������� ����������� ������
            tF.selfPE.Unpack(world.Value, out int tFEntity);

            //������ ��������� ������� GUI, ���� ����������
            GUICreateTFUIPanels(ref tF);

            //���� ��������� ������� GUI
            ref CTaskForceDisplayedGUIPanels tFDisplayedGUIPanels = ref tFDisplayedGUIPanelsPool.Value.Get(tFEntity);

            //���� ������� ������
            UIFleetsTab fleetsTab = sOUI.Value.gameWindow.objectPanel.fleetManagerSubpanel.fleetsTab;

            //������ �������� ������ ������� ������
            Game.GUI.Object.FleetManager.Fleets.UITaskForceSummaryPanel.InstantiatePanel(
                ref tF, ref tFDisplayedGUIPanels,
                fleetsTab.layoutGroup);
        }

        void FMSbpnFleetsTabDeleteTaskForceSummaryPanel(
            EcsPackedEntity tFPE)
        {
            //���� ����������� ������ � ��������� ������� GUI
            tFPE.Unpack(world.Value, out int tFEntity);
            ref CTaskForceDisplayedGUIPanels tFDisplayedUIPanels = ref tFDisplayedGUIPanelsPool.Value.Get(tFEntity);

            //�������� �������� ������ ������� ������
            Game.GUI.Object.FleetManager.Fleets.UITaskForceSummaryPanel.CachePanel(ref tFDisplayedUIPanels);

            //������� ��������� ������� GUI, ���� ����������
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
            //������ ����� �������� � ��������� �� ������� ����� ��������� ������ ������
            int eventEntity = world.Value.NewEntity();
            ref EcsGroupSystemState eventComp = ref ecsGroupSystemStatePool.Value.Add(eventEntity);

            //��������� �������� ������ ������ � ������ ���������
            eventComp.Name = systemGroupName;
            eventComp.State = systemGroupState;
        }
    }
}