
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
            //Иначе, если активна подпанель провинции
            else if(objectPanel.activeSubpanelType == ObjectSubpanelType.Province)
            {
                //Проверяем, требуется ли обновление в подпанели провинции
                ProvinceSbpnCheckRefresh();
            }
            //Иначе, если активна подпанель области карты
            else if (objectPanel.activeSubpanelType == ObjectSubpanelType.MapArea)
            {
                //Проверяем, требуется ли обновление в подпанели области
                MapAreaSbpnCheckRefresh();
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

        void ProvinceSbpnCheckRefresh()
        {
            //Берём панель объекта
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //Берём подпанель провинции
            UIProvinceSubpanel provinceSubpanel = objectPanel.provinceSubpanel;

            //Если активна обзорная вкладка
            if (provinceSubpanel.activeTab == provinceSubpanel.overviewTab)
            {
                //Запрашиваем обновление обзорной вкладки
                ObjPnActionRequest(
                    ObjectPanelActionRequestType.ProvinceOverview,
                    provinceSubpanel.activeTab.objectPE);
            }
        }

        void MapAreaSbpnCheckRefresh()
        {
            //Берём панель объекта
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //Берём подпанель области карты
            UIMapAreaSubpanel mASubpanel = objectPanel.mapAreaSubpanel;

            //Если активна обзорная вкладка
            if (mASubpanel.activeTab == mASubpanel.overviewTab)
            {
                //Запрашиваем обновление обзорной вкладки
                ObjPnActionRequest(
                    ObjectPanelActionRequestType.MapAreaOverview,
                    mASubpanel.activeTab.objectPE);
            }
            //Иначе, если активна вкладка провинций
            else if(mASubpanel.activeTab == mASubpanel.provincesTab)
            {
                //Запрашиваем обновление вкладки провинций
                ObjPnActionRequest(
                    ObjectPanelActionRequestType.MapAreaProvinces,
                    mASubpanel.activeTab.objectPE);
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