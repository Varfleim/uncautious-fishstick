
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.UI.Game.GUI;
using SO.UI.Game.GUI.Object;
using SO.UI.Game.GUI.Object.Events;

namespace SO.UI
{
    public class SUITickUpdate : IEcsRunSystem
    {
        //����
        readonly EcsWorldInject world = default;


        //������
        readonly EcsCustomInject<SOUI> sOUI = default;

        public void Run(IEcsSystems systems)
        {
            //���� ������� ���� ����
            if(sOUI.Value.activeMainWindowType == MainWindowType.Game)
            {
                //���������, ��������� �� ���������� � ���� ����
                GameCheckRefresh();
            }
        }

        #region Game
        void GameCheckRefresh()
        {
            //���� ���� ����
            UIGameWindow gameWindow = sOUI.Value.gameWindow;

            //���� ������� ������ �������
            if(gameWindow.activeMainPanelType == MainPanelType.Object)
            {
                //���������, ��������� �� ���������� � ������ �������
                ObjPnCheckRefresh();
            }
        }

        #region ObjectPanel
        void ObjPnCheckRefresh()
        {
            //���� ������ �������
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //���� ������� ��������� ������
            if (objectPanel.activeSubpanelType == ObjectSubpanelType.Country) 
            {
                //���������, ��������� �� ���������� � ��������� ������
                CountrySbpnCheckRefresh();
            }
            //�����, ���� ������� ��������� �������
            else if(objectPanel.activeSubpanelType == ObjectSubpanelType.Region)
            {
                //���������, ��������� �� ���������� � ��������� �������
                RegionSbpnCheckRefresh();
            }
            //�����, ���� ������� ��������� �������������� �������
            else if(objectPanel.activeSubpanelType == ObjectSubpanelType.StrategicArea)
            {
                //���������, ��������� �� ���������� � ��������� �������
                StrategicAreaSbpnCheckRefresh();
            }
            //�����, ���� ������� ��������� ��������� ������
            else if(objectPanel.activeSubpanelType == ObjectSubpanelType.FleetManager)
            {
                //���������, ��������� �� ���������� � ��������� ������
                FleetManagerSbpnCheckRefresh();
            }
        }

        readonly EcsPoolInject<RGameObjectPanelAction> gameObjectPanelRequestPool = default;
        void ObjPnActionRequest(
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
        
        void CountrySbpnCheckRefresh()
        {
            //���� ������ �������
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //���� ��������� ������
            UICountrySubpanel countrySubpanel = objectPanel.countrySubpanel;

            //���� ������� �������� �������
            if(countrySubpanel.activeTab == countrySubpanel.overviewTab)
            {
                //����������� ���������� �������� �������
                ObjPnActionRequest(
                    ObjectPanelActionRequestType.CountryOverview,
                    countrySubpanel.activeTab.objectPE);
            }
        }

        void RegionSbpnCheckRefresh()
        {
            //���� ������ �������
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //���� ��������� �������
            UIRegionSubpanel regionSubpanel = objectPanel.regionSubpanel;

            //���� ������� �������� �������
            if (regionSubpanel.activeTab == regionSubpanel.overviewTab)
            {
                //����������� ���������� �������� �������
                ObjPnActionRequest(
                    ObjectPanelActionRequestType.RegionOverview,
                    regionSubpanel.activeTab.objectPE);
            }
        }

        void StrategicAreaSbpnCheckRefresh()
        {
            //���� ������ �������
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //���� ��������� �������������� �������
            UIStrategicAreaSubpanel sASubpanel = objectPanel.strategicAreaSubpanel;

            //���� ������� �������� �������
            if (sASubpanel.activeTab == sASubpanel.overviewTab)
            {
                //����������� ���������� �������� �������
                ObjPnActionRequest(
                    ObjectPanelActionRequestType.StrategicAreaOverview,
                    sASubpanel.activeTab.objectPE);
            }
            //�����, ���� ������� ������� ��������
            else if(sASubpanel.activeTab == sASubpanel.regionsTab)
            {
                //����������� ���������� ������� ��������
                ObjPnActionRequest(
                    ObjectPanelActionRequestType.StrategicAreaRegions,
                    sASubpanel.activeTab.objectPE);
            }
        }

        void FleetManagerSbpnCheckRefresh()
        {
            //���� ������ �������
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //���� ��������� ��������� ������
            UIFleetManagerSubpanel fleetManagerSubpanel = objectPanel.fleetManagerSubpanel;

            //���� ������� ������� ������
            if (fleetManagerSubpanel.activeTab == fleetManagerSubpanel.fleetsTab)
            {
                //����������� ���������� ������� ������
                ObjPnActionRequest(
                    ObjectPanelActionRequestType.FleetManagerFleets,
                    fleetManagerSubpanel.activeTab.objectPE);
            }
        }
        #endregion
        #endregion
    }
}