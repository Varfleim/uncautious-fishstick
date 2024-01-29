
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.UI.Game;
using SO.UI.Game.Object;
using SO.UI.Game.Object.Events;

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

            //���� ������� ��������� �������
            if(objectPanel.activeObjectSubpanelType == ObjectSubpanelType.Faction) 
            {
                //���������, ��������� �� ���������� � ��������� �������
                FactionSbpnCheckRefresh();
            }
            //�����, ���� ������� ��������� �������
            else if(objectPanel.activeObjectSubpanelType == ObjectSubpanelType.Region)
            {
                //���������, ��������� �� ���������� � ��������� �������
                RegionSbpnCheckRefresh();
            }
        }

        readonly EcsPoolInject<RGameObjectPanelAction> gameObjectPanelRequestPool = default;
        void ObjPnActionRequest(
            ObjectPanelActionRequestType requestType,
            EcsPackedEntity objectPE = new(),
            bool isRefresh = true)
        {
            //������ ����� �������� � ��������� �� ������ �������� ������ �������
            int requestEntity = world.Value.NewEntity();
            ref RGameObjectPanelAction requestComp = ref gameObjectPanelRequestPool.Value.Add(requestEntity);

            //��������� ������ �������
            requestComp = new(
                requestType,
                objectPE,
                isRefresh);
        }

        void FactionSbpnCheckRefresh()
        {
            //���� ������ �������
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //���� ��������� �������
            UIFactionSubpanel factionSubpanel = objectPanel.factionSubpanel;

            //���� ������� �������� �������
            if(factionSubpanel.tabGroup.selectedTab == factionSubpanel.overviewTab.selfTabButton)
            {
                //����������� ���������� �������� �������
                ObjPnActionRequest(
                    ObjectPanelActionRequestType.FactionOverview,
                    objectPanel.activeObjectPE);
            }
        }

        void RegionSbpnCheckRefresh()
        {
            //���� ������ �������
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //���� ��������� �������
            UIRegionSubpanel regionSubpanel = objectPanel.regionSubpanel;

            //���� ������� �������� �������
            if (regionSubpanel.tabGroup.selectedTab == regionSubpanel.overviewTab.selfTabButton)
            {
                //����������� ���������� �������� �������
                ObjPnActionRequest(
                    ObjectPanelActionRequestType.RegionOverview,
                    objectPanel.activeObjectPE);
            }
        }
        #endregion
        #endregion
    }
}