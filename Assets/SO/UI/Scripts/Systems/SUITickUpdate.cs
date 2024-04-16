
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
            //�����, ���� ������� ��������� ���������
            else if(objectPanel.activeSubpanelType == ObjectSubpanelType.Province)
            {
                //���������, ��������� �� ���������� � ��������� ���������
                ProvinceSbpnCheckRefresh();
            }
            //�����, ���� ������� ��������� ������� �����
            else if (objectPanel.activeSubpanelType == ObjectSubpanelType.MapArea)
            {
                //���������, ��������� �� ���������� � ��������� �������
                MapAreaSbpnCheckRefresh();
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

        void ProvinceSbpnCheckRefresh()
        {
            //���� ������ �������
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //���� ��������� ���������
            UIProvinceSubpanel provinceSubpanel = objectPanel.provinceSubpanel;

            //���� ������� �������� �������
            if (provinceSubpanel.activeTab == provinceSubpanel.overviewTab)
            {
                //����������� ���������� �������� �������
                ObjPnActionRequest(
                    ObjectPanelActionRequestType.ProvinceOverview,
                    provinceSubpanel.activeTab.objectPE);
            }
        }

        void MapAreaSbpnCheckRefresh()
        {
            //���� ������ �������
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //���� ��������� ������� �����
            UIMapAreaSubpanel mASubpanel = objectPanel.mapAreaSubpanel;

            //���� ������� �������� �������
            if (mASubpanel.activeTab == mASubpanel.overviewTab)
            {
                //����������� ���������� �������� �������
                ObjPnActionRequest(
                    ObjectPanelActionRequestType.MapAreaOverview,
                    mASubpanel.activeTab.objectPE);
            }
            //�����, ���� ������� ������� ���������
            else if(mASubpanel.activeTab == mASubpanel.provincesTab)
            {
                //����������� ���������� ������� ���������
                ObjPnActionRequest(
                    ObjectPanelActionRequestType.MapAreaProvinces,
                    mASubpanel.activeTab.objectPE);
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