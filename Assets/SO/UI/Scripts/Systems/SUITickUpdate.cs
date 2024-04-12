
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.UI.Game.GUI;
using SO.UI.Game.GUI.Object;
using SO.UI.Game.GUI.Object.Events;

namespace SO.UI
{
    public class SUITickUpdate : IEcsRunSystem
    {
        //Миры
        readonly EcsWorldInject world = default;


        //Данные
        readonly EcsCustomInject<SOUI> sOUI = default;

        public void Run(IEcsSystems systems)
        {
            //Если активно окно игры
            if(sOUI.Value.activeMainWindowType == MainWindowType.Game)
            {
                //Проверяем, требуется ли обновление в окне игры
                GameCheckRefresh();
            }
        }

        #region Game
        void GameCheckRefresh()
        {
            //Берём окно игры
            UIGameWindow gameWindow = sOUI.Value.gameWindow;

            //Если активна панель объекта
            if(gameWindow.activeMainPanelType == MainPanelType.Object)
            {
                //Проверяем, требуется ли обновление в панели объекта
                ObjPnCheckRefresh();
            }
        }

        #region ObjectPanel
        void ObjPnCheckRefresh()
        {
            //Берём панель объекта
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //Если активна подпанель страны
            if (objectPanel.activeSubpanelType == ObjectSubpanelType.Country) 
            {
                //Проверяем, требуется ли обновление в подпанели страны
                CountrySbpnCheckRefresh();
            }
            //Иначе, если активна подпанель региона
            else if(objectPanel.activeSubpanelType == ObjectSubpanelType.Region)
            {
                //Проверяем, требуется ли обновление в подпанели региона
                RegionSbpnCheckRefresh();
            }
            //Иначе, если активна подпанель стратегической области
            else if(objectPanel.activeSubpanelType == ObjectSubpanelType.StrategicArea)
            {
                //Проверяем, требуется ли обновление в подпанели области
                StrategicAreaSbpnCheckRefresh();
            }
            //Иначе, если активна подпанель менеджера флотов
            else if(objectPanel.activeSubpanelType == ObjectSubpanelType.FleetManager)
            {
                //Проверяем, требуется ли обновление в менеджере флотов
                FleetManagerSbpnCheckRefresh();
            }
        }

        readonly EcsPoolInject<RGameObjectPanelAction> gameObjectPanelRequestPool = default;
        void ObjPnActionRequest(
            ObjectPanelActionRequestType requestType,
            EcsPackedEntity objectPE = new(), EcsPackedEntity secondObjectPE = new())
        {
            //Создаём новую сущность и назначаем ей запрос действия панели объекта
            int requestEntity = world.Value.NewEntity();
            ref RGameObjectPanelAction requestComp = ref gameObjectPanelRequestPool.Value.Add(requestEntity);

            //Заполняем данные запроса
            requestComp = new(
                requestType,
                objectPE, secondObjectPE);
        }
        
        void CountrySbpnCheckRefresh()
        {
            //Берём панель объекта
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //Берём подпанель страны
            UICountrySubpanel countrySubpanel = objectPanel.countrySubpanel;

            //Если активна обзорная вкладка
            if(countrySubpanel.activeTab == countrySubpanel.overviewTab)
            {
                //Запрашиваем обновление обзорной вкладки
                ObjPnActionRequest(
                    ObjectPanelActionRequestType.CountryOverview,
                    countrySubpanel.activeTab.objectPE);
            }
        }

        void RegionSbpnCheckRefresh()
        {
            //Берём панель объекта
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //Берём подпанель региона
            UIRegionSubpanel regionSubpanel = objectPanel.regionSubpanel;

            //Если активна обзорная вкладка
            if (regionSubpanel.activeTab == regionSubpanel.overviewTab)
            {
                //Запрашиваем обновление обзорной вкладки
                ObjPnActionRequest(
                    ObjectPanelActionRequestType.RegionOverview,
                    regionSubpanel.activeTab.objectPE);
            }
        }

        void StrategicAreaSbpnCheckRefresh()
        {
            //Берём панель объекта
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //Берём подпанель стратегической области
            UIStrategicAreaSubpanel sASubpanel = objectPanel.strategicAreaSubpanel;

            //Если активна обзорная вкладка
            if (sASubpanel.activeTab == sASubpanel.overviewTab)
            {
                //Запрашиваем обновление обзорной вкладки
                ObjPnActionRequest(
                    ObjectPanelActionRequestType.StrategicAreaOverview,
                    sASubpanel.activeTab.objectPE);
            }
            //Иначе, если активна вкладка регионов
            else if(sASubpanel.activeTab == sASubpanel.regionsTab)
            {
                //Запрашиваем обновление вкладки регионов
                ObjPnActionRequest(
                    ObjectPanelActionRequestType.StrategicAreaRegions,
                    sASubpanel.activeTab.objectPE);
            }
        }

        void FleetManagerSbpnCheckRefresh()
        {
            //Берём панель объекта
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //Берём подпанель менеджера флотов
            UIFleetManagerSubpanel fleetManagerSubpanel = objectPanel.fleetManagerSubpanel;

            //Если активна вкладка флотов
            if (fleetManagerSubpanel.activeTab == fleetManagerSubpanel.fleetsTab)
            {
                //Запрашиваем обновление вкладки флотов
                ObjPnActionRequest(
                    ObjectPanelActionRequestType.FleetManagerFleets,
                    fleetManagerSubpanel.activeTab.objectPE);
            }
        }
        #endregion
        #endregion
    }
}