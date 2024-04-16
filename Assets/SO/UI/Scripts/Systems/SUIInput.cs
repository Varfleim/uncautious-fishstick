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
        //����
        readonly EcsWorldInject world = default;
        EcsWorld uguiMapWorld;
        EcsWorld uguiUIWorld;


        //�����
        readonly EcsPoolInject<CProvinceHexasphere> pHSPool = default;
        readonly EcsPoolInject<CProvinceCore> pCPool = default;

        readonly EcsFilterInject<Inc<CProvinceCore, CProvinceDisplayedGUIPanels>> provinceDisplayedGUIPanelsFilter = default;
        readonly EcsPoolInject<CProvinceDisplayedGUIPanels> provinceDisplayedGUIPanelsPool = default;

        readonly EcsPoolInject<CMapArea> mAPool = default;

        //������
        readonly EcsPoolInject<CCountry> countryPool = default;

        //������� ����
        readonly EcsPoolInject<CTaskForce> taskForcePool = default;
        readonly EcsPoolInject<CTaskForceDisplayedGUIPanels> tFDisplayedGUIPanelsPool = default;


        EcsFilter clickEventMapFilter;
        EcsPool<EcsUguiClickEvent> clickEventMapPool;

        EcsFilter clickEventUIFilter;
        EcsPool<EcsUguiClickEvent> clickEventUIPool;

        //������
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
                    ChangeMapModeRequestType.Country);
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

            //���� ������ ������
            inputData.Value.playerCountryPE.Unpack(world.Value, out int countryEntity);
            ref CCountry country = ref countryPool.Value.Get(countryEntity);

            //���� ���� �� ����� ������ ������
            if (clickEvent.WidgetName == "PlayerCountryFlag")
            {
                //����������� ����������� ������ ������
                ObjectPnActionRequest(
                    uIData.Value.countrySubpanelDefaultTab,
                    country.selfPE);
            }
            //�����, ���� ���� �� ������ ��������� ������ �� ������ ������� ������
            else if(clickEvent.WidgetName == "FleetManagerMBtnPn")
            {
                //����������� ����������� ��������� ������
                ObjectPnActionRequest(
                    uIData.Value.fleetManagerSubpanelDefaultTab,
                    country.selfPE);
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
            //���� ������ ������
            inputData.Value.playerCountryPE.Unpack(world.Value, out int countryEntity);
            ref CCountry country = ref countryPool.Value.Get(countryEntity);

            //���� ������ �������
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //���� ������ ������ �������� ������ �������
            if(clickEvent.WidgetName == "ClosePanelObjPn")
            {
                //����������� �������� ������ �������
                ObjectPnActionRequest(ObjectPanelActionRequestType.Close);
            }

            //�����, ���� ������� ��������� ������
            else if (objectPanel.activeSubpanelType == ObjectSubpanelType.Country)
            {
                //��������� ����� � ��������� ������
                CountrySbpnClickAction(ref clickEvent);
            }
            //�����, ���� ������� ��������� ���������
            else if(objectPanel.activeSubpanelType == ObjectSubpanelType.Province)
            {
                //��������� ����� � ��������� ���������
                ProvinceSbpnClickAction(ref clickEvent);
            }
            //�����, ���� ������� ��������� ������� �����
            else if (objectPanel.activeSubpanelType == ObjectSubpanelType.MapArea)
            {
                //��������� ����� � ��������� �������
                MapAreaSbpnClickAction(ref clickEvent);
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
            EcsPackedEntity objectPE = new(), EcsPackedEntity secondObjectPE = new())
        {
            //������ ����� �������� � ��������� �� ������ �������� ������ �������
            int requestEntity = world.Value.NewEntity();
            ref RGameObjectPanelAction requestComp = ref gameObjectPanelRequestPool.Value.Add(requestEntity);

            //��������� ������ �������
            requestComp = new(
                requestType,
                objectPE, secondObjectPE);
        }

        #region CountrySubpanel
        void CountrySbpnClickAction(
            ref EcsUguiClickEvent clickEvent)
        {
            //���� ������ �������
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //���� ��������� ������
            UICountrySubpanel countrySubpanel = objectPanel.countrySubpanel;

            //���� ������ ������ �������� �������
            if(clickEvent.WidgetName == "OverviewTabCountrySbpn")
            {
                //����������� ����������� �������� �������
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
            //���� ������ �������
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //���� ��������� ���������
            UIProvinceSubpanel provinceSubpanel = objectPanel.provinceSubpanel;

            //���� ������ ������ �������� �������
            if (clickEvent.WidgetName == "OverviewTabProvinceSbpn")
            {
                //����������� ����������� �������� �������
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
            //���� ������ �������
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //���� ��������� ������� �����
            UIMapAreaSubpanel mASubpanel = objectPanel.mapAreaSubpanel;

            //���� ������ ������ �������� �������
            if (clickEvent.WidgetName == "OverviewTabMapAreaSbpn")
            {
                //����������� ����������� �������� �������
                ObjectPnActionRequest(
                    ObjectPanelActionRequestType.MapAreaOverview,
                    mASubpanel.activeTab.objectPE);
            }
            //�����, ���� ������ ������ ������� ���������
            else if(clickEvent.WidgetName == "ProvincesTabMapAreaSbpn")
            {
                //����������� ����������� ������� ���������
                ObjectPnActionRequest(
                    ObjectPanelActionRequestType.MapAreaProvinces,
                    mASubpanel.activeTab.objectPE);
            }

            //����
            //���� ������ ������ ������� �������
            else if(clickEvent.WidgetName == "ConquerMapAreaSbpn")
            {
                //����������� ����� ��������� ������� �� ������ ������
                MapAreaChangeOwnerRequest(
                    inputData.Value.playerCountryPE,
                    mASubpanel.activeTab.objectPE);
            }
            //����

            //�����, ���� ������� ������� ���������
            else if(mASubpanel.activeTab == mASubpanel.provincesTab)
            {
                Debug.LogWarning("!");

                //����
                //��� ������ ��������� � ������������� �������� GUI
                foreach(int provinceEntity in provinceDisplayedGUIPanelsFilter.Value)
                {
                    //���� ������ ���������
                    ref CProvinceDisplayedGUIPanels provinceDisplayedGUIPanels = ref provinceDisplayedGUIPanelsPool.Value.Get(provinceEntity);

                    //���� � ��������� ���� �������� ������ ������� ���������
                    if(provinceDisplayedGUIPanels.mASbpnProvincesTabSummaryPanel != null)
                    {
                        Debug.LogWarning("!");

                        Debug.LogWarning(clickEvent.Sender.transform.parent);

                        //���� ��� �������� ������������ �������� ��������� �������
                        if (provinceDisplayedGUIPanels.mASbpnProvincesTabSummaryPanel.transform == clickEvent.Sender.transform.parent)
                        {
                            Debug.LogWarning("!");

                            //���� ���������
                            ref CProvinceCore pC = ref pCPool.Value.Get(provinceEntity);

                            //�� ����������� ����� ��������� ���������
                            ProvinceChangeOwnerRequest(
                                inputData.Value.playerCountryPE,
                                pC.selfPE,
                                ProvinceChangeOwnerType.Test);

                            break;
                        }
                    }
                }
                //����
            }
        }

        readonly EcsPoolInject<RMapAreaChangeOwner> mAChangeOwnerRequestPool = default;
        void MapAreaChangeOwnerRequest(
            EcsPackedEntity countryPE,
            EcsPackedEntity mAPE)
        {
            //������ ����� �������� � ��������� �� ������ ����� ��������� ������� �����
            int requestEntity = world.Value.NewEntity();
            ref RMapAreaChangeOwner requestComp = ref mAChangeOwnerRequestPool.Value.Add(requestEntity);

            //��������� ������ �������
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
            //������ ����� �������� � ��������� �� ������ ����� ��������� ���������
            int requestEntity = world.Value.NewEntity();
            ref RProvinceChangeOwner requestComp = ref provinceChangeOwnerRequestPool.Value.Add(requestEntity);

            //��������� ������ �������
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
            //���� ������ ������
            inputData.Value.playerCountryPE.Unpack(world.Value, out int countryEntity);
            ref CCountry country = ref countryPool.Value.Get(countryEntity);

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
                        ref CTaskForceDisplayedGUIPanels tFDisplayedGUIPanels = ref tFDisplayedGUIPanelsPool.Value.Get(tFEntity);

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
                TaskForceCreatingRequest(ref country);
            }
        }

        readonly EcsPoolInject<RTaskForceCreating> taskForceCreatingRequestPool = default;
        void TaskForceCreatingRequest(
            ref CCountry ownerCountry)
        {
            //������ ����� �������� � ��������� �� ������ �������� ����������� ������
            int requestEntity = world.Value.NewEntity();
            ref RTaskForceCreating requestComp = ref taskForceCreatingRequestPool.Value.Add(requestEntity);

            //��������� ������ �������
            requestComp = new(ownerCountry.selfPE);
        }
        #endregion
        #endregion

        #region Hexasphere
        void HexasphereCheckMouseInteraction()
        {
            //���� ������ ��������� ��� �����������
            if(HexasphereCheckMousePosition(out Vector3 position, out Ray ray, out EcsPackedEntity provincePE))
            {
                //���� ��������� ��� ��������
                provincePE.Unpack(world.Value, out int provinceEntity);
                ref CProvinceHexasphere currentPHS = ref pHSPool.Value.Get(provinceEntity);

                //����
                ref CProvinceCore currentPC = ref pCPool.Value.Get(provinceEntity);

                //���� ������������ ������� ����� ���������
                currentPC.ParentMapAreaPE.Unpack(world.Value, out int mAEntity);
                ref CMapArea mA = ref mAPool.Value.Get(mAEntity);
                //����

                //���� ������ ���
                if(inputData.Value.leftMouseButtonClick)
                {
                    //����������� ����������� ��������� ���������
                    //ObjectPnActionRequest(
                    //    uIData.Value.provinceSubpanelDefaultTab,
                    //    currentPHS.selfPE);

                    //����������� ����������� ��������� �������
                    ObjectPnActionRequest(
                        uIData.Value.mapAreaSubpanelDefaultTab,
                        mA.selfPE, currentPHS.selfPE);
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
            //���������, ��������� �� ������ ��� �����������
            inputData.Value.isMouseOver = HexasphereGetHitPoint(out position, out ray);

            provincePE = new();

            //���� ������ ��������� ��� �����������
            if(inputData.Value.isMouseOver == true)
            {
                //���������� ������ ���������, ��� ������� ��������� ������
                int provinceIndex = ProvinceGetInRayDirection(
                    ray, position,
                    out Vector3 hitPosition);

                //���� ������ ��������� ������ ����
                if (provinceIndex >= 0)
                {
                    //���� ���������
                    provincesData.Value.provincePEs[provinceIndex].Unpack(world.Value, out int provinceEntity);
                    ref CProvinceHexasphere pHS = ref pHSPool.Value.Get(provinceEntity);
                    ref CProvinceCore pC = ref pCPool.Value.Get(provinceEntity);

                    //���� �������� ��������� �� ����� �������� ���������� �������������
                    if(inputData.Value.lastHighlightedProvincePE.EqualsTo(pC.selfPE) == false)
                    {
                        //���� ��������� ������������ ��������� ����������
                        if(inputData.Value.lastHighlightedProvincePE.Unpack(world.Value, out int lastHighlightProvinceEntity))
                        {
                            //����������� ���������� ��������� ��� ����
                            ProvinceHideHighlightRequest(
                                inputData.Value.lastHighlightedProvincePE,
                                ProvinceHighlightRequestType.Hover);
                        }

                        //��������� ������������ ���������
                        inputData.Value.lastHighlightedProvincePE = pC.selfPE;

                        //����������� ��������� ��������� ��� ����
                        ProvinceShowHighlightRequest(
                            pC.selfPE,
                            ProvinceHighlightRequestType.Hover);
                    }

                    provincePE = pC.selfPE;

                    return true;
                }
                //�����, ���� ������ ��������� ������ ���� � ��������� ������������ ��������� ����������
                else if(provinceIndex < 0 && inputData.Value.lastHighlightedProvincePE.Unpack(world.Value, out int lastHighlightProvinceEntity))
                {
                    //����������� ���������� ��������� ��� ����
                    ProvinceHideHighlightRequest(
                        inputData.Value.lastHighlightedProvincePE,
                        ProvinceHighlightRequestType.Hover);

                    //������� ��������� ������������ ���������
                    inputData.Value.lastHighlightedProvincePE = new();
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

        int ProvinceGetInRayDirection(
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

            //���� ������ ��������� 
            int nearestProvinceIndex = ProvinceGetAtLocalPosition(SceneData.HexashpereGO.transform.InverseTransformPoint(worldPosition));
            //���� ������ ������ ����, ������� �� �������
            if (nearestProvinceIndex < 0)
            {
                return -1;
            }

            //���������� ������ ���������
            Vector3 currentPoint = worldPosition;

            //���� ��������� ���������
            provincesData.Value.provincePEs[nearestProvinceIndex].Unpack(world.Value, out int nearestProvinceEntity);
            ref CProvinceHexasphere nearestPHS = ref pHSPool.Value.Get(nearestProvinceEntity);

            //���������� ������� ����� ���������
            Vector3 provinceTop = SceneData.HexashpereGO.transform.TransformPoint(
                nearestPHS.center * (1.0f + nearestPHS.ExtrudeAmount * mapGenerationData.Value.extrudeMultiplier));

            //���������� ������ ��������� � ������ ����
            float provinceHeight = provinceTop.sqrMagnitude;
            float rayHeight = currentPoint.sqrMagnitude;

            float minDistance = 1e6f;
            distance = minDistance;

            //���������� ������ ���������-���������
            int candidateProvinceIndex = -1;

            const int NUM_STEPS = 10;
            //�������� �����
            for (int a = 1; a <= NUM_STEPS; a++)
            {
                distance = Mathf.Abs(rayHeight - provinceHeight);

                //���� ���������� ������ ������������
                if (distance < minDistance)
                {
                    //��������� ����������� ���������� � ���������
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

                //��������� ��������� ���������
                provincesData.Value.provincePEs[nearestProvinceIndex].Unpack(world.Value, out nearestProvinceEntity);
                nearestPHS = ref pHSPool.Value.Get(nearestProvinceEntity);

                provinceTop = SceneData.HexashpereGO.transform.TransformPoint(
                    nearestPHS.center * (1.0f + nearestPHS.ExtrudeAmount * mapGenerationData.Value.extrudeMultiplier));

                //���������� ������ ��������� � ������ ����
                provinceHeight = provinceTop.sqrMagnitude;
                rayHeight = currentPoint.sqrMagnitude;
            }

            //���� ���������� ������ ������������
            if (distance < minDistance)
            {
                //��������� ����������� ���������� � ���������
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
            //���������, �� ��������� �� ��� ���������
            if (inputData.Value.lastHitProvinceIndex >= 0 && inputData.Value.lastHitProvinceIndex < provincesData.Value.provincePEs.Length)
            {
                //���� ��������� ���������
                provincesData.Value.provincePEs[inputData.Value.lastHitProvinceIndex].Unpack(world.Value, out int lastHitProvinceEntity);
                ref CProvinceCore lastHitPC = ref pCPool.Value.Get(lastHitProvinceEntity);

                //���������� ���������� �� ������ ���������
                float distance = Vector3.SqrMagnitude(lastHitPC.center - localPosition);

                bool isValid = true;

                //��� ������ �������� ���������
                for (int a = 0; a < lastHitPC.neighbourProvincePEs.Length; a++)
                {
                    //���� �������� ���������
                    lastHitPC.neighbourProvincePEs[a].Unpack(world.Value, out int neighbourProvinceEntity);
                    ref CProvinceHexasphere neighbourPHS = ref pHSPool.Value.Get(neighbourProvinceEntity);

                    //���������� ���������� �� ������ ���������
                    float otherDistance = Vector3.SqrMagnitude(neighbourPHS.center - localPosition);

                    //���� ��� ������ ���������� �� ��������� ���������
                    if (otherDistance < distance)
                    {
                        //�������� ��� � ������� �� �����
                        isValid = false;
                        break;
                    }
                }

                //���� ��� ��������� ���������
                if (isValid == true)
                {
                    return inputData.Value.lastHitProvinceIndex;
                }
            }
            //�����
            else
            {
                //�������� ������ ��������� ���������
                inputData.Value.lastHitProvinceIndex = 0;
            }

            //������� ����������� ���� � ������������ ����������
            provincesData.Value.provincePEs[inputData.Value.lastHitProvinceIndex].Unpack(world.Value, out int nearestProvinceEntity);
            ref CProvinceCore nearestPC = ref pCPool.Value.Get(nearestProvinceEntity);

            float minDist = 1e6f;

            //��� ������ ���������
            for (int a = 0; a < provincesData.Value.provincePEs.Length; a++)
            {
                //���� ��������� ��������� 
                ProvinceGetNearestToPosition(
                    ref nearestPC.neighbourProvincePEs,
                    localPosition,
                    out float provinceDistance).Unpack(world.Value, out int newNearestProvinceEntity);

                //���� ���������� ������ ������������
                if (provinceDistance < minDist)
                {
                    //��������� ��������� � ����������� ���������� 
                    nearestPC = ref pCPool.Value.Get(newNearestProvinceEntity);

                    minDist = provinceDistance;
                }
                //����� ������� �� �����
                else
                {
                    break;
                }
            }

            //������ ��������� ��������� - ��� ������ ����������
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

            //��� ������ ��������� � �������
            for (int a = 0; a < provincePEs.Length; a++)
            {
                //���� ���������
                provincePEs[a].Unpack(world.Value, out int provinceEntity);
                ref CProvinceHexasphere pHS = ref pHSPool.Value.Get(provinceEntity);

                //���� ����� ���������
                Vector3 center = pHS.center;

                //������������ ����������
                float distance
                    = (center.x - localPosition.x) * (center.x - localPosition.x)
                    + (center.y - localPosition.y) * (center.y - localPosition.y)
                    + (center.z - localPosition.z) * (center.z - localPosition.z);

                //���� ���������� ������ ������������
                if (distance < minDistance)
                {
                    //��������� ��������� � ����������� ����������
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
            //������ ����� �������� � ��������� �� ������ ��������� ��������� ���������
            int requestEntity = world.Value.NewEntity();
            ref RGameShowProvinceHighlight requestComp = ref gameShowProvinceHighlightRequestPool.Value.Add(requestEntity);

            //��������� ������ �������
            requestComp = new(
                provincePE,
                requestType);
        }

        readonly EcsPoolInject<RGameHideProvinceHighlight> gameHideProvinceHighlightRequestPool = default;
        void ProvinceHideHighlightRequest(
            EcsPackedEntity provincePE,
            ProvinceHighlightRequestType requestType)
        {
            //������ ����� �������� � ��������� �� ������ ���������� ��������� ���������
            int requestEntity = world.Value.NewEntity();
            ref RGameHideProvinceHighlight requestComp = ref gameHideProvinceHighlightRequestPool.Value.Add(requestEntity);

            //��������� ������ �������
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
            //������ ����� �������� � ��������� �� ������ ������ ��������
            int requestEntity = world.Value.NewEntity();
            ref RGeneralAction requestComp = ref generalActionRequestPool.Value.Add(requestEntity);

            //��������� ������ �������
            requestComp = new(actionType);
        }
    }
}