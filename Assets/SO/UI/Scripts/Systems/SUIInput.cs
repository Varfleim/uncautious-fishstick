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
        //����
        readonly EcsWorldInject world = default;
        EcsWorld uguiMapWorld;
        EcsWorld uguiUIWorld;


        //�����
        readonly EcsFilterInject<Inc<CRegionHexasphere>> regionFilter = default;
        readonly EcsPoolInject<CRegionHexasphere> rHSPool = default;
        readonly EcsPoolInject<CRegionCore> rCPool = default;

        //�������
        readonly EcsPoolInject<CFaction> factionPool = default;

        //������� ����
        readonly EcsPoolInject<CTaskForce> taskForcePool = default;
        readonly EcsPoolInject<CTaskForceDisplayedGUIPanels> taskForceDisplayedGUIPanelsPool = default;


        EcsFilter clickEventMapFilter;
        EcsPool<EcsUguiClickEvent> clickEventMapPool;

        EcsFilter clickEventUIFilter;
        EcsPool<EcsUguiClickEvent> clickEventUIPool;

        //������
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
            //���������� ��������� ������
            CheckButtons();

            //��� ������� ������� ����� �� ����������
            foreach (int clickEventUIEntity in clickEventUIFilter)
            {
                //���� �������
                ref EcsUguiClickEvent clickEvent = ref clickEventUIPool.Get(clickEventUIEntity);

                Debug.LogWarning("Click! " + clickEvent.WidgetName);

                //���� ������� ���� ����
                if(sOUI.Value.activeMainWindowType == MainWindowType.Game)
                {
                    //��������� ����� � ���� ����
                    GameClickAction(ref clickEvent);
                }
                //�����, ���� ������� ���� �������� ����
                else if(sOUI.Value.activeMainWindowType == MainWindowType.MainMenu)
                {
                    MainMenuClickAction(ref clickEvent);
                }

                uguiUIWorld.DelEntity(clickEventUIEntity);
            }

            //��� ������� ������� ����� �� �����
            foreach (int clickEventMapEntity in clickEventMapFilter)
            {
                //���� �������
                ref EcsUguiClickEvent clickEvent = ref clickEventMapPool.Get(clickEventMapEntity);

                Debug.LogWarning("Click! " + clickEvent.WidgetName);

                uguiMapWorld.DelEntity(clickEventMapEntity);
            }

            //���� ������� �����-���� ����� �����
            if (inputData.Value.mapMode != MapMode.None)
            {
                //���� �������� ������
                CameraInputRotating();

                //���� ����������� ������
                CameraInputZoom();
            }

            //������ ����
            InputOther();

            //���� ����
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
            //���������� ��������� ���
            inputData.Value.leftMouseButtonClick = Input.GetMouseButtonDown(0);
            inputData.Value.leftMouseButtonPressed = inputData.Value.leftMouseButtonClick || Input.GetMouseButton(0);
            inputData.Value.leftMouseButtonRelease = Input.GetMouseButtonUp(0);

            //���������� ��������� ���
            inputData.Value.rightMouseButtonClick = Input.GetMouseButtonDown(1);
            inputData.Value.rightMouseButtonPressed = inputData.Value.leftMouseButtonClick || Input.GetMouseButton(1);
            inputData.Value.rightMouseButtonRelease = Input.GetMouseButtonUp(1);

            //���������� ��������� LeftShift
            inputData.Value.leftShiftKeyPressed = Input.GetKey(KeyCode.LeftShift);
        }

        void CameraInputRotating()
        {
            //���� Y-��������
            float yRotationDelta = Input.GetAxis("Horizontal");
            //���� ��� �� ����� ����, �� ���������
            if (yRotationDelta != 0f)
            {
                CameraAdjustYRotation(yRotationDelta);
            }

            //���� X-��������
            float xRotationDelta = Input.GetAxis("Vertical");
            //���� ��� �� ����� ����, �� ���������
            if (xRotationDelta != 0f)
            {
                CameraAdjustXRotation(xRotationDelta);
            }
        }

        void CameraAdjustYRotation(
            float rotationDelta)
        {
            //������������ ���� ��������
            inputData.Value.rotationAngleY -= rotationDelta * inputData.Value.rotationSpeed * UnityEngine.Time.deltaTime;

            //����������� ����
            if (inputData.Value.rotationAngleY < 0f)
            {
                inputData.Value.rotationAngleY += 360f;
            }
            else if (inputData.Value.rotationAngleY >= 360f)
            {
                inputData.Value.rotationAngleY -= 360f;
            }

            //��������� ��������
            inputData.Value.mapCamera.localRotation = Quaternion.Euler(
                0f, inputData.Value.rotationAngleY, 0f);
        }

        void CameraAdjustXRotation(
            float rotationDelta)
        {
            //������������ ���� ��������
            inputData.Value.rotationAngleX += rotationDelta * inputData.Value.rotationSpeed * UnityEngine.Time.deltaTime;

            //����������� ����
            inputData.Value.rotationAngleX = Mathf.Clamp(
                inputData.Value.rotationAngleX, inputData.Value.minAngleX, inputData.Value.maxAngleX);

            //��������� ��������
            inputData.Value.swiwel.localRotation = Quaternion.Euler(
                inputData.Value.rotationAngleX, 0f, 0f);
        }

        void CameraInputZoom()
        {
            //���� �������� ������� ����
            float zoomDelta = Input.GetAxis("Mouse ScrollWheel");

            //���� ��� �� ����� ����
            if (zoomDelta != 0)
            {
                //��������� �����������
                CameraAdjustZoom(zoomDelta);
            }
        }

        void CameraAdjustZoom(
            float zoomDelta)
        {
            //������������ ����������� ������
            inputData.Value.zoom = Mathf.Clamp01(inputData.Value.zoom + zoomDelta);

            //������������ ���������� ����������� � ��������� ���
            float zoomDistance = Mathf.Lerp(
                inputData.Value.stickMinZoom, inputData.Value.stickMaxZoom, inputData.Value.zoom);
            inputData.Value.stick.localPosition = new(0f, 0f, zoomDistance);
        }

        void InputOther()
        {
            //����� �����
            if(Input.GetKeyUp(KeyCode.Space))
            {
                //���� ���� �������
                if (runtimeData.Value.isGameActive == true)
                {
                    //����������� ��������� �����
                    GameActionRequest(GameActionType.PauseOn);
                }
                //�����
                else
                {
                    //����������� ���������� �����
                    GameActionRequest(GameActionType.PauseOff);
                }
            }

            //�������� ����
            if(Input.GetKeyUp(KeyCode.Escape))
            {
                //����������� ����� �� ����
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
            //���� ������ �� ��������� ��� �������� ����������
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject() == false)
            {
                //��������� �������������� ���� �� ������
                HexasphereCheckMouseInteraction();
            }
            //�����
            else
            {
                //������ ����� �� ��������� ��� �����������
                inputData.Value.isMouseOver = false;
            }
        }

        #region MainMenu
        void MainMenuClickAction(
            ref EcsUguiClickEvent clickEvent)
        {
            //���� ������������� �������� ���� ����
            if(clickEvent.WidgetName == "MainMenuNewGame")
            {
                //������ ������ �������� ���� ����
                MainMenuActionRequest(MainMenuActionType.OpenGame);
            }
            //�����, ���� ������������� ����� �� ����
            else if(clickEvent.WidgetName == "QuitGame")
            {
                //������ ������ ������ �� ����
                GeneralActionRequest(GeneralActionType.QuitGame);
            }
        }

        readonly EcsPoolInject<RMainMenuAction> mainMenuActionRequestPool = default;
        void MainMenuActionRequest(
            MainMenuActionType actionType)
        {
            //������ ����� �������� � ��������� �� ������ �������� � ������� ����
            int requestEntity = world.Value.NewEntity();
            ref RMainMenuAction requestComp = ref mainMenuActionRequestPool.Value.Add(requestEntity);

            //��������� ������ �������
            requestComp = new(actionType);
        }
        #endregion

        #region Game
        void GameClickAction(
            ref EcsUguiClickEvent clickEvent)
        {
            //���� ���� ����
            UIGameWindow gameWindow = sOUI.Value.gameWindow;

            //���� ������� ������
            inputData.Value.playerFactionPE.Unpack(world.Value, out int factionEntity);
            ref CFaction faction = ref factionPool.Value.Get(factionEntity);

            //���� ���� �� ����� ������� ������
            if (clickEvent.WidgetName == "PlayerFactionFlag")
            {
                //����������� ����������� ������ �������
                ObjectPnActionRequest(
                    uIData.Value.factionSubpanelDefaultTab,
                    faction.selfPE);
            }
            //�����, ���� ���� �� ������ ��������� ������ �� ������ ������� ������
            else if(clickEvent.WidgetName == "FleetManagerMBtnPn")
            {
                //����������� ����������� ��������� ������
                ObjectPnActionRequest(
                    uIData.Value.fleetManagerSubpanelDefaultTab,
                    faction.selfPE);
            }

            //�����, ���� ������� ������ �������
            else if (gameWindow.activeMainPanelType == MainPanelType.Object)
            {
                //��������� ����� � ������ �������
                ObjectPnClickAction(ref clickEvent);
            }
        }

        readonly EcsPoolInject<RGameAction> gameActionRequestPool = default;
        void GameActionRequest(
            GameActionType actionType)
        {
            //������ ����� �������� � ��������� �� ������ �������� � ����
            int requestEntity = world.Value.NewEntity();
            ref RGameAction requestComp = ref gameActionRequestPool.Value.Add(requestEntity);

            //��������� ������ �������
            requestComp = new(actionType);
        }

        readonly EcsPoolInject<RChangeMapMode> changeMapModeRequestTypePool = default;
        void MapChangeMapModeRequest(
            ChangeMapModeRequestType requestType)
        {
            //������ ����� �������� � ��������� �� ������ ����� ������ �����
            int requestEntity = world.Value.NewEntity();
            ref RChangeMapMode requestComp = ref changeMapModeRequestTypePool.Value.Add(requestEntity);

            //��������� ������ �������
            requestComp = new(
                requestType);
        }

        readonly EcsPoolInject<RTaskForceChangeMission> taskForceChangeMissionRequestPool = default;
        void TaskForceChangeMissionRequest(
            ref CTaskForce tF,
            TaskForceChangeMissionType requestType)
        {
            //������ ����� �������� � ��������� �� ������ ����� ������ ����������� ������
            int requestEntity = world.Value.NewEntity();
            ref RTaskForceChangeMission requestComp = ref taskForceChangeMissionRequestPool.Value.Add(requestEntity);

            //��������� ������ �������
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
            //������ ����� �������� � ��������� �� ������ �������� ����������� ������
            int requestEntity = world.Value.NewEntity();
            ref RTaskForceMovement requestComp = ref tFMovementRequestPool.Value.Add(requestEntity);

            //��������� ������ �������
            requestComp = new(
                tF.selfPE,
                targetPE, targetType);
        }

        #region ObjectPanel
        void ObjectPnClickAction(
            ref EcsUguiClickEvent clickEvent)
        {
            //���� ������� ������
            inputData.Value.playerFactionPE.Unpack(world.Value, out int factionEntity);
            ref CFaction faction = ref factionPool.Value.Get(factionEntity);

            //���� ������ �������
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //���� ������ ������ �������� ������ �������
            if(clickEvent.WidgetName == "ClosePanelObjPn")
            {
                //����������� �������� ������ �������
                ObjectPnActionRequest(ObjectPanelActionRequestType.Close);
            }

            //�����, ���� ������� ��������� �������
            else if(objectPanel.activeSubpanelType == ObjectSubpanelType.Faction)
            {
                //��������� ����� � ��������� �������
                FactionSbpnClickAction(ref clickEvent);
            }
            //�����, ���� ������� ��������� �������
            else if(objectPanel.activeSubpanelType == ObjectSubpanelType.Region)
            {
                //��������� ����� � ��������� �������
                RegionSbpnClickAction(ref clickEvent);
            }
            //�����, ���� ������� ��������� ��������� ������
            else if (objectPanel.activeSubpanelType == ObjectSubpanelType.FleetManager)
            {
                //��������� ����� � ��������� ��������� ������
                FleetManagerSbpnClickAction(ref clickEvent);
            }
        }

        readonly EcsPoolInject<RGameObjectPanelAction> gameObjectPanelRequestPool = default;
        void ObjectPnActionRequest(
            ObjectPanelActionRequestType requestType,
            EcsPackedEntity objectPE = new())
        {
            //������ ����� �������� � ��������� �� ������ �������� ������ �������
            int requestEntity = world.Value.NewEntity();
            ref RGameObjectPanelAction requestComp = ref gameObjectPanelRequestPool.Value.Add(requestEntity);

            //��������� ������ �������
            requestComp = new(
                requestType,
                objectPE);
        }

        #region FactionSubpanel
        void FactionSbpnClickAction(
            ref EcsUguiClickEvent clickEvent)
        {
            //���� ������ �������
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //���� ��������� �������
            UIFactionSubpanel factionSubpanel = objectPanel.factionSubpanel;

            //���� ������ ������ �������� �������
            if(clickEvent.WidgetName == "OverviewTabFactionSbpn")
            {
                //����������� ����������� �������� �������
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
            //���� ������ �������
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //���� ��������� �������
            UIRegionSubpanel regionSubpanel = objectPanel.regionSubpanel;

            //���� ������ ������ �������� �������
            if (clickEvent.WidgetName == "OverviewTabRegionSbpn")
            {
                //����������� ����������� �������� �������
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
            //���� ������ �������
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //���� ��������� ��������� ������
            UIFleetManagerSubpanel fleetManagerSubpanel = objectPanel.fleetManagerSubpanel;

            //���� ������ ������ ������� ������
            if (clickEvent.WidgetName == "FleetsTabFleetManagerSbpn")
            {
                //����������� ����������� ������� ������
                ObjectPnActionRequest(
                    ObjectPanelActionRequestType.FleetManagerFleets,
                    fleetManagerSubpanel.activeTab.objectPE);
            }

            //�����, ���� ������� ������� ������
            else if (fleetManagerSubpanel.activeTab == fleetManagerSubpanel.fleetsTab)
            {
                //��������� ����� �� ������� ������
                FMSbpnFleetsTabClickAction(ref clickEvent);
            }
        }

        void FMSbpnFleetsTabClickAction(
            ref EcsUguiClickEvent clickEvent)
        {
            //���� ������� ������
            inputData.Value.playerFactionPE.Unpack(world.Value, out int factionEntity);
            ref CFaction faction = ref factionPool.Value.Get(factionEntity);

            //���� ������ �������
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //���� ��������� ��������� ������
            UIFleetManagerSubpanel fleetManagerSubpanel = objectPanel.fleetManagerSubpanel;

            //���� �������� ������� ����� ��������� TaskForceSummaryPanel
            if(clickEvent.Sender.TryGetComponent(out Game.GUI.Object.FleetManager.Fleets.UITaskForceSummaryPanel taskForceSummaryPanel))
            {
                //���� ����� LShift
                if(inputData.Value.leftShiftKeyPressed == true)
                {
                    //�� ��� ����������� ������ ����� ��������� � ��������� ��� ������� 

                    //���� ������ ���������
                    if(taskForceSummaryPanel.toggle.isOn == false)
                    {
                        //������ � �������� � ������� � ������
                        taskForceSummaryPanel.toggle.isOn = true;
                        inputData.Value.activeTaskForcePEs.Add(taskForceSummaryPanel.selfPE);
                    }
                    //�����
                    else
                    {
                        //������ � ���������� � ������� �� ������
                        taskForceSummaryPanel.toggle.isOn = false;
                        inputData.Value.activeTaskForcePEs.Remove(taskForceSummaryPanel.selfPE);
                    }
                }
                //����� ������ ���
                else
                {
                    //���� ������ ���� ������� � � ������ ���� ������ ����� ������, �� ������� ������ � ���������� ������
                    //����� ��������� ������
                    bool isActivation
                        = taskForceSummaryPanel.toggle.isOn && (inputData.Value.activeTaskForcePEs.Count > 1)
                        || (taskForceSummaryPanel.toggle.isOn == false);

                    //��� ������ ��������� ������
                    for (int a = 0; a < inputData.Value.activeTaskForcePEs.Count; a++)
                    {
                        //���� ��������� ������� GUI
                        inputData.Value.activeTaskForcePEs[a].Unpack(world.Value, out int tFEntity);
                        ref CTaskForceDisplayedGUIPanels tFDisplayedGUIPanels = ref taskForceDisplayedGUIPanelsPool.Value.Get(tFEntity);

                        //������ ������ ������ ������ ����������
                        tFDisplayedGUIPanels.fMSbpnFleetsTabSummaryPanel.toggle.isOn = false;
                    }
                    //������� ������ ��������� �����
                    inputData.Value.activeTaskForcePEs.Clear();

                    //���� ��������� ������������ ������
                    if (isActivation == true)
                    {
                        //������ � �������� � ������� � ������ ��������
                        taskForceSummaryPanel.toggle.isOn = true;
                        inputData.Value.activeTaskForcePEs.Add(taskForceSummaryPanel.selfPE);
                    }
                }
            }
            //�����, ���� ������ ������ �������� ����� ����������� ������
            else if(clickEvent.WidgetName == "CreateNewTaskForceFMSbpnFleetsTab")
            {
                //����������� �������� ����� ������
                TaskForceCreatingRequest(ref faction);
            }
        }

        readonly EcsPoolInject<RTaskForceCreating> taskForceCreatingRequestPool = default;
        void TaskForceCreatingRequest(
            ref CFaction ownerFaction)
        {
            //������ ����� �������� � ��������� �� ������ �������� ����������� ������
            int requestEntity = world.Value.NewEntity();
            ref RTaskForceCreating requestComp = ref taskForceCreatingRequestPool.Value.Add(requestEntity);

            //��������� ������ �������
            requestComp = new(ownerFaction.selfPE);
        }
        #endregion
        #endregion

        #region Hexasphere
        void HexasphereCheckMouseInteraction()
        {
            //���� ������ ��������� ��� �����������
            if(HexasphereCheckMousePosition(out Vector3 position, out Ray ray, out EcsPackedEntity regionPE))
            {
                //���� ������ ��� ��������
                regionPE.Unpack(world.Value, out int regionEntity);
                ref CRegionHexasphere currentRHS = ref rHSPool.Value.Get(regionEntity);

                //���� ������ ���
                if(inputData.Value.leftMouseButtonClick)
                {
                    //����������� ����������� ��������� �������
                    ObjectPnActionRequest(
                        uIData.Value.regionSubpanelDefaultTab,
                        currentRHS.selfPE);
                }
                //�����, ���� ������ ���
                else if(inputData.Value.rightMouseButtonClick)
                {
                    //���� ������ �������� ������
                    if(sOUI.Value.gameWindow.objectPanel.activeSubpanelType == ObjectSubpanelType.FleetManager)
                    {
                        //��� ������ �������� ����������� ������
                        for(int a = 0; a < inputData.Value.activeTaskForcePEs.Count; a++)
                        {
                            //���� ������
                            inputData.Value.activeTaskForcePEs[a].Unpack(world.Value, out int tFEntity);
                            ref CTaskForce tF = ref taskForcePool.Value.Get(tFEntity);

                            //����������� ����������� ������
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
            //���������, ��������� �� ������ ��� �����������
            inputData.Value.isMouseOver = HexasphereGetHitPoint(out position, out ray);

            regionPE = new();

            //���� ������ ��������� ��� �����������
            if(inputData.Value.isMouseOver == true)
            {
                //���������� ������ �������, ��� ������� ��������� ������
                int regionIndex = RegionGetInRayDirection(
                    ray, position,
                    out Vector3 hitPosition);

                //���� ������ ������� ������ ����
                if (regionIndex >= 0)
                {
                    //���� ������
                    regionsData.Value.regionPEs[regionIndex].Unpack(world.Value, out int regionEntity);
                    ref CRegionHexasphere rHS = ref rHSPool.Value.Get(regionEntity);
                    ref CRegionCore rC = ref rCPool.Value.Get(regionEntity);

                    //���� �������� ������� �� ����� �������� ���������� �������������
                    if(inputData.Value.lastHighlightedRegionPE.EqualsTo(rC.selfPE) == false)
                    {
                        //���� ��������� ������������ ������ ����������
                        if(inputData.Value.lastHighlightedRegionPE.Unpack(world.Value, out int lastHighlightRegionEntity))
                        {
                            //����������� ���������� ��������� ��� ����
                            RegionHideHighlightRequest(
                                inputData.Value.lastHighlightedRegionPE,
                                RegionHighlightRequestType.Hover);
                        }

                        //��������� ������������ ������
                        inputData.Value.lastHighlightedRegionPE = rC.selfPE;

                        //����������� ��������� ��������� ��� ����
                        RegionShowHighlightRequest(
                            rC.selfPE,
                            RegionHighlightRequestType.Hover);
                    }

                    regionPE = rC.selfPE;

                    return true;
                }
                //�����, ���� ������ ������� ������ ���� � ��������� ������������ ������ ����������
                else if(regionIndex < 0 && inputData.Value.lastHighlightedRegionPE.Unpack(world.Value, out int lastHighlightRegionEntity))
                {
                    //����������� ���������� ��������� ��� ����
                    RegionHideHighlightRequest(
                        inputData.Value.lastHighlightedRegionPE,
                        RegionHighlightRequestType.Hover);

                    //������� ��������� ������������ ������
                    inputData.Value.lastHighlightedRegionPE = new();
                }
            }

            return false;
        }

        bool HexasphereGetHitPoint(
            out Vector3 position,
            out Ray ray)
        {
            //���� ��� �� ��������� ����
            ray = inputData.Value.camera.ScreenPointToRay(Input.mousePosition);

            //�������� �������
            return HexapshereGetHitPoint(ray, out position);
        }

        bool HexapshereGetHitPoint(
            Ray ray,
            out Vector3 position)
        {
            //���� ��� �������� �������
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                //���� ��� �������� ������� ����������
                if (hit.collider.gameObject == SceneData.HexashpereGO)
                {
                    //���������� ����� �������
                    position = hit.point;

                    //� ���������� true
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

            //���������� �������� ����� �������
            Vector3 minPoint = worldPosition;
            Vector3 maxPoint = worldPosition + 0.5f * mapGenerationData.Value.hexasphereScale * ray.direction;

            float rangeMin = mapGenerationData.Value.hexasphereScale * 0.5f;
            rangeMin *= rangeMin;

            float rangeMax = worldPosition.sqrMagnitude;

            float distance;
            Vector3 bestPoint = maxPoint;

            //�������� �����
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

            //���� ������ ������� 
            int nearestRegionIndex = RegionGetAtLocalPosition(SceneData.HexashpereGO.transform.InverseTransformPoint(worldPosition));
            //���� ������ ������ ����, ������� �� �������
            if (nearestRegionIndex < 0)
            {
                return -1;
            }

            //���������� ������ �������
            Vector3 currentPoint = worldPosition;

            //���� ��������� ������
            regionsData.Value.regionPEs[nearestRegionIndex].Unpack(world.Value, out int nearestRegionEntity);
            ref CRegionHexasphere nearestRHS = ref rHSPool.Value.Get(nearestRegionEntity);

            //���������� ������� ����� �������
            Vector3 regionTop = SceneData.HexashpereGO.transform.TransformPoint(
                nearestRHS.center * (1.0f + nearestRHS.ExtrudeAmount * mapGenerationData.Value.extrudeMultiplier));

            //���������� ������ ������� � ������ ����
            float regionHeight = regionTop.sqrMagnitude;
            float rayHeight = currentPoint.sqrMagnitude;

            float minDistance = 1e6f;
            distance = minDistance;

            //���������� ������ �������-���������
            int candidateRegionIndex = -1;

            const int NUM_STEPS = 10;
            //�������� �����
            for (int a = 1; a <= NUM_STEPS; a++)
            {
                distance = Mathf.Abs(rayHeight - regionHeight);

                //���� ���������� ������ ������������
                if (distance < minDistance)
                {
                    //��������� ����������� ���������� � ���������
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

                //��������� ��������� ������
                regionsData.Value.regionPEs[nearestRegionIndex].Unpack(world.Value, out nearestRegionEntity);
                nearestRHS = ref rHSPool.Value.Get(nearestRegionEntity);

                regionTop = SceneData.HexashpereGO.transform.TransformPoint(
                    nearestRHS.center * (1.0f + nearestRHS.ExtrudeAmount * mapGenerationData.Value.extrudeMultiplier));

                //���������� ������ ������� � ������ ����
                regionHeight = regionTop.sqrMagnitude;
                rayHeight = currentPoint.sqrMagnitude;
            }

            //���� ���������� ������ ������������
            if (distance < minDistance)
            {
                //��������� ����������� ���������� � ���������
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
            //���������, �� ��������� �� ��� ������
            if (inputData.Value.lastHitRegionIndex >= 0 && inputData.Value.lastHitRegionIndex < regionsData.Value.regionPEs.Length)
            {
                //���� ��������� ������
                regionsData.Value.regionPEs[inputData.Value.lastHitRegionIndex].Unpack(world.Value, out int lastHitRegionEntity);
                ref CRegionCore lastHitRC = ref rCPool.Value.Get(lastHitRegionEntity);

                //���������� ���������� �� ������ �������
                float distance = Vector3.SqrMagnitude(lastHitRC.center - localPosition);

                bool isValid = true;

                //��� ������� ��������� �������
                for (int a = 0; a < lastHitRC.neighbourRegionPEs.Length; a++)
                {
                    //���� �������� ������
                    lastHitRC.neighbourRegionPEs[a].Unpack(world.Value, out int neighbourRegionEntity);
                    ref CRegionHexasphere neighbourRHS = ref rHSPool.Value.Get(neighbourRegionEntity);

                    //���������� ���������� �� ������ �������
                    float otherDistance = Vector3.SqrMagnitude(neighbourRHS.center - localPosition);

                    //���� ��� ������ ���������� �� ���������� �������
                    if (otherDistance < distance)
                    {
                        //�������� ��� � ������� �� �����
                        isValid = false;
                        break;
                    }
                }

                //���� ��� ��������� ������
                if (isValid == true)
                {
                    return inputData.Value.lastHitRegionIndex;
                }
            }
            //�����
            else
            {
                //�������� ������ ���������� �������
                inputData.Value.lastHitRegionIndex = 0;
            }

            //������� ����������� ���� � ������������ ����������
            regionsData.Value.regionPEs[inputData.Value.lastHitRegionIndex].Unpack(world.Value, out int nearestRegionEntity);
            ref CRegionCore nearestRC = ref rCPool.Value.Get(nearestRegionEntity);

            float minDist = 1e6f;

            //��� ������� �������
            for (int a = 0; a < regionsData.Value.regionPEs.Length; a++)
            {
                //���� ��������� ������ 
                RegionGetNearestToPosition(
                    ref nearestRC.neighbourRegionPEs,
                    localPosition,
                    out float regionDistance).Unpack(world.Value, out int newNearestRegionEntity);

                //���� ���������� ������ ������������
                if (regionDistance < minDist)
                {
                    //��������� ������ � ����������� ���������� 
                    nearestRC = ref rCPool.Value.Get(newNearestRegionEntity);

                    minDist = regionDistance;
                }
                //����� ������� �� �����
                else
                {
                    break;
                }
            }

            //������ ���������� ������� - ��� ������ ����������
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

            //��� ������� ������� � �������
            for (int a = 0; a < regionPEs.Length; a++)
            {
                //���� ������
                regionPEs[a].Unpack(world.Value, out int regionEntity);
                ref CRegionHexasphere rHS = ref rHSPool.Value.Get(regionEntity);

                //���� ����� �������
                Vector3 center = rHS.center;

                //������������ ����������
                float distance
                    = (center.x - localPosition.x) * (center.x - localPosition.x)
                    + (center.y - localPosition.y) * (center.y - localPosition.y)
                    + (center.z - localPosition.z) * (center.z - localPosition.z);

                //���� ���������� ������ ������������
                if (distance < minDistance)
                {
                    //��������� ������ � ����������� ����������
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
            //������ ����� �������� � ��������� �� ������ ��������� ��������� �������
            int requestEntity = world.Value.NewEntity();
            ref RGameShowRegionHighlight requestComp = ref gameShowRegionHighlightRequestPool.Value.Add(requestEntity);

            //��������� ������ �������
            requestComp = new(
                regionPE,
                requestType);
        }

        readonly EcsPoolInject<RGameHideRegionHighlight> gameHideRegionHighlightRequestPool = default;
        void RegionHideHighlightRequest(
            EcsPackedEntity regionPE,
            RegionHighlightRequestType requestType)
        {
            //������ ����� �������� � ��������� �� ������ ���������� ��������� �������
            int requestEntity = world.Value.NewEntity();
            ref RGameHideRegionHighlight requestComp = ref gameHideRegionHighlightRequestPool.Value.Add(requestEntity);

            //��������� ������ �������
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
            //������ ����� �������� � ��������� �� ������ ������ ��������
            int requestEntity = world.Value.NewEntity();
            ref RGeneralAction requestComp = ref generalActionRequestPool.Value.Add(requestEntity);

            //��������� ������ �������
            requestComp = new(actionType);
        }
    }
}