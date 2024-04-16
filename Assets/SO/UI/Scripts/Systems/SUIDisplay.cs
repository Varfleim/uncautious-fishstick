
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
        //����
        readonly EcsWorldInject world = default;


        //�����
        readonly EcsPoolInject<CProvinceHexasphere> pHSPool = default;
        readonly EcsPoolInject<CProvinceCore> pCPool = default;

        readonly EcsPoolInject<CProvinceEconomy> pEPool = default;

        readonly EcsPoolInject<CProvinceDisplayedGUIPanels> provinceDisplayedGUIPanelsPool = default;
        readonly EcsFilterInject<Inc<CProvinceHexasphere, CProvinceDisplayedMapPanels>> provinceDisplayedMapPanelsFilter = default;
        readonly EcsPoolInject<CProvinceDisplayedMapPanels> provinceDisplayedMapPanelsPool = default;

        readonly EcsPoolInject<CMapArea> mAPool = default;

        //������
        readonly EcsPoolInject<CCountry> countryPool = default;

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
        readonly EcsCustomInject<RuntimeData> runtimeData = default;

        readonly EcsCustomInject<SOUI> sOUI = default;

        public void Init(IEcsSystems systems)
        {
            ListPool<int>.Init();

            //������� ������� � �� �����
            GOProvinceRenderer.provinceRendererPrefab = uIData.Value.provinceRendererPrefab;
            CProvinceDisplayedMapPanels.mapPanelGroupPrefab = uIData.Value.mapPanelGroup;

            Game.GUI.Object.MapArea.Provinces.UIProvinceSummaryPanel.panelPrefab = uIData.Value.mASbpnProvincesTabProvinceSummaryPanelPrefab;
            UIPCMainMapPanel.panelPrefab = uIData.Value.pCMainMapPanelPrefab;

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

                //���� ������������� �������� �������� ������ ��������� ������� ��������� ��������� ������� �����
                if (requestComp.panelType == GamePanelType.ProvinceSummaryPanelMASbpnProvincesTab)
                {
                    //������ �������� ������ ���������
                    MapAreaSbpnCreateProvinceSummaryPanel(ref requestComp);
                }
                //�����, ���� ������������� �������� ������� ������ ����� ���������
                if (requestComp.panelType == GamePanelType.ProvinceMainMapPanel)
                {
                    //������ ������� ������ ����� ���������
                    MapUICreateProvinceMainMapPanel(ref requestComp);
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
            //��������� ��������� ���������
            GameRefreshUIProvince();

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

                //���� ������������� �������� �������� ������ ��������� ������� ��������� ��������� ������� �����
                if (requestComp.panelType == GamePanelType.ProvinceSummaryPanelMASbpnProvincesTab)
                {
                    //������� �������� ������
                    MapAreaSbpnProvincesTabDeleteProvinceSummaryPanel(requestComp.objectPE);
                }
                //�����, ���� ������������� �������� ������� ������ ����� ���������
                else if (requestComp.panelType == GamePanelType.ProvinceMainMapPanel)
                {
                    //������� ������ �����
                    MapUIDeleteProvinceMainMapPanel(requestComp.objectPE);
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

        readonly EcsFilterInject<Inc<CProvinceCore, CProvinceDisplayedGUIPanels, SRGameRefreshPanels>> provinceRefreshGUIPanelsSelfRequestFilter = default;
        readonly EcsFilterInject<Inc<CProvinceCore, CProvinceDisplayedMapPanels, SRGameRefreshPanels>> provinceRefreshMapPanelsSelfRequestFilter = default;
        void GameRefreshUIProvince()
        {
            //��� ������ ��������� � ����������� ������� GUI � ������������ ���������� �������
            foreach (int provinceEntity in provinceRefreshGUIPanelsSelfRequestFilter.Value)
            {
                //���� ��������� � ��������� �������
                ref CProvinceCore pC = ref pCPool.Value.Get(provinceEntity);
                ref CProvinceEconomy pE = ref pEPool.Value.Get(provinceEntity);
                ref CProvinceDisplayedGUIPanels provinceDisplayedGUIPanels = ref provinceDisplayedGUIPanelsPool.Value.Get(provinceEntity);

                //���� ��������� ����� ������������ �������� ������ ������� ��������� ��������� ������� �����
                if (provinceDisplayedGUIPanels.mASbpnProvincesTabSummaryPanel != null)
                {
                    //��������� �
                    provinceDisplayedGUIPanels.mASbpnProvincesTabSummaryPanel.RefreshPanel(ref pC, ref pE);
                }
            }

            //��� ������ ��������� � ����������� ������� ����� � ������������ ���������� �������
            foreach (int provinceEntity in provinceRefreshMapPanelsSelfRequestFilter.Value)
            {
                //���� ��������� � ��������� �������
                ref CProvinceCore pC = ref pCPool.Value.Get(provinceEntity);
                ref CProvinceDisplayedMapPanels provinceDisplayedMapPanels = ref provinceDisplayedMapPanelsPool.Value.Get(provinceEntity);

                //���� ��������� ����� ������������ ������� ������
                if (provinceDisplayedMapPanels.mainMapPanel != null)
                {
                    //��������� �
                    provinceDisplayedMapPanels.mainMapPanel.RefreshPanel(ref pC);
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
            //��� ������ ��������� � �������� �����
            foreach (int provinceEntity in provinceDisplayedMapPanelsFilter.Value)
            {
                //���� ��������� ������� �����
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
            //���� �������� ���������
            pHS.selfPE.Unpack(world.Value, out int provinceEntity);

            //���� ��������� �� ����� ���������� ������� �����
            if (provinceDisplayedMapPanelsPool.Value.Has(provinceEntity) == false)
            {
                //��������� ��������� ��������� ������� �����
                ref CProvinceDisplayedMapPanels provinceDisplayedMapPanels = ref provinceDisplayedMapPanelsPool.Value.Add(provinceEntity);

                //��������� ������ ����������
                provinceDisplayedMapPanels = new(0);

                //������ ����� ������ ������ ������� �����
                CProvinceDisplayedMapPanels.InstantiateMapPanelGroup(
                    ref pHS, ref provinceDisplayedMapPanels,
                    mapGenerationData.Value.hexasphereScale,
                    uIData.Value.mapPanelAltitude);
            }
        }

        void MapUIDeleteProvinceDisplayedMapPanels(
            EcsPackedEntity provincePE)
        {
            //���� �������� ��������� � ��������� ������� �����
            provincePE.Unpack(world.Value, out int provinceEntity);
            ref CProvinceDisplayedMapPanels provinceDisplayedMapPanels = ref provinceDisplayedMapPanelsPool.Value.Get(provinceEntity);

            //���� ������� �� ������� �� ����������
            if (provinceDisplayedMapPanels.IsEmpty == true)
            {
                //�������� ������ �������
                CProvinceDisplayedMapPanels.CacheMapPanelGroup(ref provinceDisplayedMapPanels);

                //������� � �������� ��������� ��������� ������� �����
                provinceDisplayedMapPanelsPool.Value.Del(provinceEntity);
            }
        }

        void MapUICreateProvinceMainMapPanel(
            ref RGameCreatePanel requestComp)
        {
            //���� PC � ��������� ������������ ���������
            requestComp.objectPE.Unpack(world.Value, out int provinceEntity);
            ref CProvinceCore pC = ref pCPool.Value.Get(provinceEntity);
            ref CProvinceHexasphere pHS = ref pHSPool.Value.Get(provinceEntity);

            //������ ��������� ������� �����, ���� ����������
            MapUICreateProvinceDisplayedMapPanels(ref pHS);

            //���� ��������� ������� �����
            ref CProvinceDisplayedMapPanels provinceDisplayedMapPanels = ref provinceDisplayedMapPanelsPool.Value.Get(provinceEntity);

            //������ ������� ������ �����
            UIPCMainMapPanel.InstantiatePanel(
                ref pC, ref provinceDisplayedMapPanels);
        }

        void MapUIDeleteProvinceMainMapPanel(
            EcsPackedEntity provincePE)
        {
            //���� ��������� ������� �����
            provincePE.Unpack(world.Value, out int provinceEntity);
            ref CProvinceDisplayedMapPanels provinceDisplayedMapPanels = ref provinceDisplayedMapPanelsPool.Value.Get(provinceEntity);

            //�������� ������� ������ �����
            UIPCMainMapPanel.CachePanel(ref provinceDisplayedMapPanels);

            //������� ��������� ������� �����, ���� ����������
            MapUIDeleteProvinceDisplayedMapPanels(provincePE);
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

            //����������� ������ � ������� ��������� ������
            MapUISetParentTaskForceMainMapPanel(ref tF, ref tFDisplayedMapPanels);
        }

        void MapUISetParentTaskForceMainMapPanel(
            ref CTaskForce tF, ref CTaskForceDisplayedMapPanels tFDisplayedMapPanels)
        {
            //���� ���������� ��������� ����������� ������ ����������
            if(tF.previousProvincePE.Unpack(world.Value, out int previousProvinceEntity))
            {
                //���� ��������� ������� ����� ���������� ��������� 
                ref CProvinceDisplayedMapPanels previousProvinceDisplayedMapPanels = ref provinceDisplayedMapPanelsPool.Value.Get(previousProvinceEntity);

                //���������� ������ ������ �� ����
                previousProvinceDisplayedMapPanels.CancelParentTaskForceMainMapPanel(tFDisplayedMapPanels.mainMapPanel);
            }

            //��������� ������� ��������� � ���������� ������� ����� ������
            tFDisplayedMapPanels.currentProvincePE = tF.currentProvincePE;

            //���� ������� ��������� ������
            tF.currentProvincePE.Unpack(world.Value, out int currentProvinceEntity);
            ref CProvinceHexasphere pHS = ref pHSPool.Value.Get(currentProvinceEntity);

            //������ ��������� ������� �����, ���� ����������
            MapUICreateProvinceDisplayedMapPanels(ref pHS);

            //���� ��������� ������� �����
            ref CProvinceDisplayedMapPanels currentProvinceDisplayedMapPanels = ref provinceDisplayedMapPanelsPool.Value.Get(currentProvinceEntity);

            //����������� � ���� ������� ������ ����� ������
            currentProvinceDisplayedMapPanels.SetParentTaskForceMainMapPanel(tFDisplayedMapPanels.mainMapPanel);
        }

        void MapUIDeleteTaskForceMainMapPanel(
            EcsPackedEntity tFPE)
        {
            //���� ��������� ������� �����
            tFPE.Unpack(world.Value, out int tFEntity);
            ref CTaskForceDisplayedMapPanels tFDisplayedMapPanels = ref tFDisplayedMapPanelsPool.Value.Get(tFEntity);

            //���� ��������� ������� ����� ������� ��������� ������
            tFDisplayedMapPanels.currentProvincePE.Unpack(world.Value, out int currentProvinceEntity);
            ref CProvinceDisplayedMapPanels provinceDisplayedMapPanels = ref provinceDisplayedMapPanelsPool.Value.Get(currentProvinceEntity);

            //���������� ������ ������ �� ����
            provinceDisplayedMapPanels.CancelParentTaskForceMainMapPanel(tFDisplayedMapPanels.mainMapPanel);

            //�������� ������� ������ �����
            UITFMainMapPanel.CachePanel(ref tFDisplayedMapPanels);

            //������� ��������� ������� ����� � ������, ���� ����������
            MapUIDeleteTaskForceDisplayedMapPanels(tFPE);

            //������� ��������� ������� ����� � ���������, ���� ����������
            MapUIDeleteProvinceDisplayedMapPanels(tFDisplayedMapPanels.currentProvincePE);
        }

        void ParentAndAlignToProvince(
            GameObject go,
            ref CProvinceHexasphere pHS,
            float altitude = 0)
        {
            //���� ����� ���������
            Vector3 provinceCenter = pHS.GetProvinceCenter() * mapGenerationData.Value.hexasphereScale;

            //���� ������ �� ����� ����
            if(altitude != 0)
            {
                Vector3 direction = provinceCenter.normalized * altitude;
                go.transform.position = provinceCenter + direction;
            }
            //�����
            else
            {
                go.transform.position = provinceCenter;
            }

            //����������� ������ � ���������
            go.transform.SetParent(pHS.selfObject.transform, true);
            go.transform.LookAt(pHS.selfObject.transform.position);
        }
        #endregion

        #region GUI
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

        readonly EcsFilterInject<Inc<CProvinceCore, CNonDeletedUI>> provinceNonDeletedUIFilter = default;
        readonly EcsFilterInject<Inc<CProvinceDisplayedGUIPanels, CNonDeletedUI>> provinceDisplayedNonDeletedGUIFilter = default;
        readonly EcsFilterInject<Inc<CProvinceDisplayedGUIPanels>, Exc<CNonDeletedUI>> provinceDisplayedDeletedGUIFilter = default;
        void GUICreateProvinceGUIPanels(
            ref CProvinceCore pC)
        {
            //���� �������� ���������
            pC.selfPE.Unpack(world.Value, out int provinceEntity);
            
            //���� ��������� �� ����� ���������� ������� GUI
            if(provinceDisplayedGUIPanelsPool.Value.Has(provinceEntity) == false)
            {
                //��������� ��� ��������� ������� GUI
                ref CProvinceDisplayedGUIPanels provinceDisplayedGUIPanels = ref provinceDisplayedGUIPanelsPool.Value.Add(provinceEntity);
            }
        }

        void GUIDeleteProvinceGUIPanels(
            EcsPackedEntity provincePE)
        {
            //���� �������� ��������� � ��������� ������� GUI
            provincePE.Unpack(world.Value, out int provinceEntity);
            ref CProvinceDisplayedGUIPanels provinceDisplayedGUIPanels = ref provinceDisplayedGUIPanelsPool.Value.Get(provinceEntity);

            //���� ������� �� ������� �� ����������
            if(provinceDisplayedGUIPanels.mASbpnProvincesTabSummaryPanel == null)
            {
                //������� � �������� ��������� ��������� ������� GUI
                provinceDisplayedGUIPanelsPool.Value.Del(provinceEntity);
            }
        }

        readonly EcsFilterInject<Inc<CTaskForce, CNonDeletedUI>> taskForceNonDeletedUIFilter = default;
        readonly EcsFilterInject<Inc<CTaskForceDisplayedGUIPanels, CNonDeletedUI>> taskForceDisplayedNonDeletedGUIFilter = default;
        readonly EcsFilterInject<Inc<CTaskForceDisplayedGUIPanels>, Exc<CNonDeletedUI>> taskForceDisplayedDeletedGUIFilter = default;
        void GUICreateTaskForceGUIPanels(
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

                //�����, ���� ������������� ����������� ������� ������
                else if (requestComp.requestType >= ObjectPanelActionRequestType.CountryOverview
                    && requestComp.requestType <= ObjectPanelActionRequestType.CountryOverview)
                {
                    //���� ������
                    requestComp.objectPE.Unpack(world.Value, out int countryEntity);
                    ref CCountry country = ref countryPool.Value.Get(countryEntity);

                    //���� ������������� ����������� �������� �������
                    if (requestComp.requestType == ObjectPanelActionRequestType.CountryOverview)
                    {
                        //���������� �������� ������� ������
                        CountrySbpnShowOverviewTab(ref country);
                    }
                }
                //�����, ���� ������������� ����������� ������� ���������
                else if (requestComp.requestType >= ObjectPanelActionRequestType.ProvinceOverview
                    && requestComp.requestType <= ObjectPanelActionRequestType.ProvinceOverview)
                {
                    //���� ���������
                    requestComp.objectPE.Unpack(world.Value, out int provinceEntity);
                    ref CProvinceHexasphere pHS = ref pHSPool.Value.Get(provinceEntity);
                    ref CProvinceCore pC = ref pCPool.Value.Get(provinceEntity);

                    //���� ������������� ����������� �������� �������
                    if (requestComp.requestType == ObjectPanelActionRequestType.ProvinceOverview)
                    {
                        //���������� �������� ������� ���������
                        ProvinceSbpnShowOverviewTab(ref pHS, ref pC);
                    }
                }
                //�����, ���� ������������� ����������� ������� ������� �����
                else if (requestComp.requestType >= ObjectPanelActionRequestType.MapAreaOverview
                    && requestComp.requestType <= ObjectPanelActionRequestType.MapAreaProvinces)
                {
                    //���� �������
                    requestComp.objectPE.Unpack(world.Value, out int mAEntity);
                    ref CMapArea mA = ref mAPool.Value.Get(mAEntity);

                    //���� ������������� ����������� �������� �������
                    if (requestComp.requestType == ObjectPanelActionRequestType.MapAreaOverview)
                    {
                        //���������� �������� ������� �������
                        MapAreaSbpnShowOverviewTab(ref mA);
                    }
                    //�����, ���� ������������� ����������� ������� ���������
                    else if (requestComp.requestType == ObjectPanelActionRequestType.MapAreaProvinces)
                    {
                        //���������� ������� ��������� �������
                        MapAreaSbpnShowProvincesTab(ref mA, requestComp.secondObjectPE);
                    }
                }
                //�����, ���� ������������� ����������� ������� ��������� ������
                else if(requestComp.requestType >= ObjectPanelActionRequestType.FleetManagerFleets
                    && requestComp.requestType <= ObjectPanelActionRequestType.FleetManagerFleets)
                {
                    //���� ������
                    requestComp.objectPE.Unpack(world.Value, out int countryEntity);
                    ref CCountry country = ref countryPool.Value.Get(countryEntity);

                    //���� ������������� ����������� ������� ������
                    if (requestComp.requestType == ObjectPanelActionRequestType.FleetManagerFleets)
                    {
                        //���������� ������� ������
                        FleetManagerSbpnShowFleetsTab(ref country);
                    }
                }

                gameObjectPanelRequestPool.Value.Del(requestEntity);
            }
        }

        void ObjectPnClose()
        {
            //���� ������ �������
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //���� ������� ��������� ������
            if (objectPanel.activeSubpanelType == ObjectSubpanelType.Country)
            {
                //�������� �������� �������
                objectPanel.countrySubpanel.HideActiveTab();
            }
            //�����, ���� ������� ��������� ���������
            else if (objectPanel.activeSubpanelType == ObjectSubpanelType.Province)
            {
                //�������� �������� �������
                objectPanel.provinceSubpanel.HideActiveTab();
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

        #region CountrySubpanel
        void CountrySbpnShowTab(
            UIASubpanelTab currentCountryTab,
            ref CCountry currentCountry,
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

            //���� ��������� ������
            UICountrySubpanel countrySubpanel = objectPanel.countrySubpanel;

            //���������� ��������� ������, ���� ����������
            ObjectPnOpenSubpanel(
                countrySubpanel, ObjectSubpanelType.Country,
                out isSamePanel,
                out isSameSubpanel);

            //���� ������� ����������� �������
            if (countrySubpanel.activeTab == currentCountryTab)
            {
                //���� ���� ������� �� �� ���������
                if(isSameSubpanel == true)
                {
                    //��������, ��� ���� ������� �� �� �������
                    isSameTab = true;

                    //���� ������� ���� ������� ��� ��� �� ������
                    if (countrySubpanel.activeTab.objectPE.EqualsTo(currentCountry.selfPE) == true)
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
                countrySubpanel.tabGroup.OnTabSelected(currentCountryTab.selfTabButton);

                //��������� � ��� �������� �������
                countrySubpanel.activeTab = currentCountryTab;
            }

            //���� ��� �������� ��� �� ������
            if (isSameObject == true)
            {

            }
            //�����
            else
            {
                //��������� PE ������� ������
                countrySubpanel.activeTab.objectPE = currentCountry.selfPE;

                //���������� �������� ������ - �������� ������
                objectPanel.objectName.text = currentCountry.selfIndex.ToString();
            }
        }

        void CountrySbpnShowOverviewTab(
            ref CCountry country)
        {
            //���� ��������� ������
            UICountrySubpanel countrySubpanel = sOUI.Value.gameWindow.objectPanel.countrySubpanel;

            //���� �������� �������
            Game.GUI.Object.Country.UIOverviewTab overviewTab = countrySubpanel.overviewTab;

            //���������� �������� �������
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
            //�������� �� ��������� ������������
            isSameTab = false;
            isSameObject = false;

            //���� ������ �������
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //���� ��������� ���������
            UIProvinceSubpanel provinceSubpanel = objectPanel.provinceSubpanel;

            //���������� ��������� ���������, ���� ����������
            ObjectPnOpenSubpanel(
                provinceSubpanel, ObjectSubpanelType.Province,
                out isSamePanel,
                out isSameSubpanel);

            //���� ������� ����������� �������
            if (provinceSubpanel.activeTab == currentProvinceTab)
            {
                //���� ���� ������� �� �� ���������
                if (isSameSubpanel == true)
                {
                    //��������, ��� ���� ������� �� �� �������
                    isSameTab = true;

                    //���� ������� ���� ������� ��� ��� �� ���������
                    if (provinceSubpanel.activeTab.objectPE.EqualsTo(currentPC.selfPE) == true)
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
                provinceSubpanel.tabGroup.OnTabSelected(currentProvinceTab.selfTabButton);

                //��������� � ��� �������� �������
                provinceSubpanel.activeTab = currentProvinceTab;
            }

            //���� ��� �������� ��� �� ������
            if (isSameObject == true)
            {

            }
            //�����
            else
            {
                //��������� PE ������� ���������
                provinceSubpanel.activeTab.objectPE = currentPC.selfPE;

                //���������� �������� ������ - �������� ���������
                objectPanel.objectName.text = currentPC.Index.ToString();
            }
        }

        void ProvinceSbpnShowOverviewTab(
            ref CProvinceHexasphere pHS, ref CProvinceCore pC)
        {
            //���� ��������� ���������
            UIProvinceSubpanel provinceSubpanel = sOUI.Value.gameWindow.objectPanel.provinceSubpanel;

            //���� �������� �������
            Game.GUI.Object.Province.UIOverviewTab overviewTab = provinceSubpanel.overviewTab;

            //���������� �������� �������
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
            //�������� �� ��������� ������������
            isSameTab = false;
            isSameObject = false;

            //���� ������ �������
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //���� ��������� ������� �����
            UIMapAreaSubpanel mASubpanel = objectPanel.mapAreaSubpanel;

            //���������� ��������� �������, ���� ����������
            ObjectPnOpenSubpanel(
                mASubpanel, ObjectSubpanelType.MapArea,
                out isSamePanel,
                out isSameSubpanel);

            //���� ������� ����������� �������
            if (mASubpanel.activeTab == currentMATab)
            {
                //���� ���� ������� �� �� ���������
                if (isSameSubpanel == true)
                {
                    //��������, ��� ���� ������� �� �� �������
                    isSameTab = true;

                    //���� ������� ���� ������� ��� ��� �� �������
                    if (mASubpanel.activeTab.objectPE.EqualsTo(currentMA.selfPE) == true)
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
                mASubpanel.tabGroup.OnTabSelected(currentMATab.selfTabButton);

                //��������� � ��� �������� �������
                mASubpanel.activeTab = currentMATab;
            }

            //���� ��� �������� ��� �� ������
            if (isSameObject == true)
            {

            }
            //�����
            else
            {
                //��������� PE ������� �������
                mASubpanel.activeTab.objectPE = currentMA.selfPE;

                //���������� �������� ������ - �������� �������
                objectPanel.objectName.text = currentMA.selfPE.Id.ToString();
            }
        }

        void MapAreaSbpnShowOverviewTab(
            ref CMapArea mA)
        {
            //���� ��������� ������� �����
            UIMapAreaSubpanel mASubpanel = sOUI.Value.gameWindow.objectPanel.mapAreaSubpanel;

            //���� �������� �������
            Game.GUI.Object.MapArea.UIOverviewTab overviewTab = mASubpanel.overviewTab;

            //���������� �������� �������
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
            //���� ��������� ������� �����
            UIMapAreaSubpanel mASubpanel = sOUI.Value.gameWindow.objectPanel.mapAreaSubpanel;

            //���� ������� ���������
            Game.GUI.Object.MapArea.UIProvincesTab provincesTab = mASubpanel.provincesTab;

            //���������� ������� ���������
            MapAreaSbpnShowTab(
                provincesTab,
                ref mA,
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
                //���������, ����� ��������� ������ ���� ���������� � ���� ������
                //��� ������ ��������� �������
                for (int a = 0; a < mA.provincePEs.Length; a++)
                {
                    //���� �������� ��������� � ��������� ��������� "����������� UI"
                    mA.provincePEs[a].Unpack(world.Value, out int provinceEntity);
                    ref CNonDeletedUI provinceNonDeletedUI = ref nonDeletedUIPool.Value.Add(provinceEntity);
                }

                //������� �������� ������ ������� ��������� � ������ ���������, ������� �� ������ ���� � ������
                //��� ������ ��������� � ������������� �������� GUI, �� ��� "������������ UI"
                foreach (int provinceEntity in provinceDisplayedDeletedGUIFilter.Value)
                {
                    //���� ��������� ������� GUI
                    ref CProvinceDisplayedGUIPanels provinceDisplayedGUIPanels = ref provinceDisplayedGUIPanelsPool.Value.Get(provinceEntity);

                    //���� � ��������� ���� �������� ������ ������� ���������
                    if (provinceDisplayedGUIPanels.mASbpnProvincesTabSummaryPanel != null)
                    {
                        //������� �������� ������
                        MapAreaSbpnProvincesTabDeleteProvinceSummaryPanel(provinceDisplayedGUIPanels.mASbpnProvincesTabSummaryPanel.selfPE);
                    }
                }

                //��������� ������ ���������, ������� ��� ����� �������� ������
                //��� ������ ��������� � �������� GUI � "����������� UI"
                foreach (int provinceEntity in provinceDisplayedNonDeletedGUIFilter.Value)
                {
                    //���� ��������� � ��������� ������� GUI
                    ref CProvinceCore pC = ref pCPool.Value.Get(provinceEntity);
                    ref CProvinceEconomy pE = ref pEPool.Value.Get(provinceEntity);
                    ref CProvinceDisplayedGUIPanels provinceDisplayedGUIPanels = ref provinceDisplayedGUIPanelsPool.Value.Get(provinceEntity);

                    //���� � ��������� ���� �������� ������ ������� ���������
                    if (provinceDisplayedGUIPanels.mASbpnProvincesTabSummaryPanel != null)
                    {
                        //��������� � ������
                        provinceDisplayedGUIPanels.mASbpnProvincesTabSummaryPanel.RefreshPanel(ref pC, ref pE);

                        //� ������� � ��������� "����������� UI"
                        nonDeletedUIPool.Value.Del(provinceEntity);
                    }
                    //����� ������ ������ �����������, �� ��� ��������� �� ��������� ����, � ������� ������ ���������
                }

                //������ �������� ������ ��� ���������, ������� �� ����� ��, �� ������
                //��� ������ ��������� � "����������� UI"
                foreach (int provinceEntity in provinceNonDeletedUIFilter.Value)
                {
                    //���� ���������
                    ref CProvinceCore pC = ref pCPool.Value.Get(provinceEntity);
                    ref CProvinceEconomy pE = ref pEPool.Value.Get(provinceEntity);

                    //������ �������� ������
                    MapAreaSbpnProvincesTabCreateProvinceSummaryPanel(ref pC, ref pE);

                    //� ������� � ��������� "����������� UI"
                    nonDeletedUIPool.Value.Del(provinceEntity);
                }
            }

            //���� PE ������� ��������� �� �����
            if (currentPCPE.Unpack(world.Value, out int currentProvinceEntity))
            {
                //���� �������� ������� ��������� � ��������� ������� GUI
                ref CProvinceDisplayedGUIPanels currentProvinceDisplayedGUIPanels = ref provinceDisplayedGUIPanelsPool.Value.Get(currentProvinceEntity);

                //���������� ������ �� ������� ���������
                provincesTab.scrollView.FocusOnItem(currentProvinceDisplayedGUIPanels.mASbpnProvincesTabSummaryPanel.selfRect);
            }
        }

        void MapAreaSbpnCreateProvinceSummaryPanel(
            ref RGameCreatePanel requestComp)
        {
            //���� ���������
            requestComp.objectPE.Unpack(world.Value, out int provinceEntity);
            ref CProvinceCore pC = ref pCPool.Value.Get(provinceEntity);
            ref CProvinceEconomy pE = ref pEPool.Value.Get(provinceEntity);

            //������ �������� ������
            MapAreaSbpnProvincesTabCreateProvinceSummaryPanel(ref pC, ref pE);
        }

        void MapAreaSbpnProvincesTabCreateProvinceSummaryPanel(
            ref CProvinceCore pC, ref CProvinceEconomy pE)
        {
            //���� �������� ���������
            pC.selfPE.Unpack(world.Value, out int provinceEntity);

            //������ ��������� ������� GUI, ���� ����������
            GUICreateProvinceGUIPanels(ref pC);

            //���� ��������� ������� GUI
            ref CProvinceDisplayedGUIPanels provinceDisplayedGUIPanels = ref provinceDisplayedGUIPanelsPool.Value.Get(provinceEntity);

            //���� ������� ���������
            UIProvincesTab provincesTab = sOUI.Value.gameWindow.objectPanel.mapAreaSubpanel.provincesTab;

            //������ �������� ������ ������� ���������
            Game.GUI.Object.MapArea.Provinces.UIProvinceSummaryPanel.InstantiatePanel(
                ref pC, ref pE, ref provinceDisplayedGUIPanels,
                provincesTab.layoutGroup);
        }

        void MapAreaSbpnProvincesTabDeleteProvinceSummaryPanel(
            EcsPackedEntity provincePE)
        {
            //���� ��������� � ��������� ������� GUI
            provincePE.Unpack(world.Value, out int provinceEntity);
            ref CProvinceDisplayedGUIPanels provinceDisplayedGUIPanels = ref provinceDisplayedGUIPanelsPool.Value.Get(provinceEntity);

            //�������� �������� ������ ������� ���������
            Game.GUI.Object.MapArea.Provinces.UIProvinceSummaryPanel.CachePanel(ref provinceDisplayedGUIPanels);

            //������� ��������� ������� GUI, ���� ����������
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

                    //���� ������� ���� ������� ��� ��� �� ������
                    if (fleetManagerSubpanel.activeTab.objectPE.EqualsTo(currentCountry.selfPE) == true)
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
                //��������� PE ������� ������
                fleetManagerSubpanel.activeTab.objectPE = currentCountry.selfPE;
            }
        }

        void FleetManagerSbpnShowFleetsTab(
            ref CCountry country)
        {
            //���� ��������� ��������� ������
            UIFleetManagerSubpanel fleetManagerSubpanel = sOUI.Value.gameWindow.objectPanel.fleetManagerSubpanel;

            //���� ������� ������
            UIFleetsTab fleetsTab = fleetManagerSubpanel.fleetsTab;

            //���������� ������� ������
            FleetManagerSbpnShowTab(
                fleetsTab,
                ref country,
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
                //��� ������ ����������� ������ ������
                for (int a = 0; a < country.ownedTaskForces.Count; a++)
                {
                    //���� �������� ����������� ������ � ��������� ��������� "����������� UI"
                    country.ownedTaskForces[a].Unpack(world.Value, out int tFEntity);
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
            GUICreateTaskForceGUIPanels(ref tF);

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
            ref CTaskForceDisplayedGUIPanels tFDisplayedGUIPanels = ref tFDisplayedGUIPanelsPool.Value.Get(tFEntity);

            //�������� �������� ������ ������� ������
            Game.GUI.Object.FleetManager.Fleets.UITaskForceSummaryPanel.CachePanel(ref tFDisplayedGUIPanels);

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