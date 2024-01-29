
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.UI.Game;
using SO.UI.Game.Object;
using SO.UI.Game.Object.Events;

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

            //Если активна подпанель фракции
            if(objectPanel.activeObjectSubpanelType == ObjectSubpanelType.Faction) 
            {
                //Проверяем, требуется ли обновление в подпанели фракции
                FactionSbpnCheckRefresh();
            }
            //Иначе, если активна подпанель региона
            else if(objectPanel.activeObjectSubpanelType == ObjectSubpanelType.Region)
            {
                //Проверяем, требуется ли обновление в подпанели региона
                RegionSbpnCheckRefresh();
            }
        }

        readonly EcsPoolInject<RGameObjectPanelAction> gameObjectPanelRequestPool = default;
        void ObjPnActionRequest(
            ObjectPanelActionRequestType requestType,
            EcsPackedEntity objectPE = new(),
            bool isRefresh = true)
        {
            //Создаём новую сущность и назначаем ей запрос действия панели объекта
            int requestEntity = world.Value.NewEntity();
            ref RGameObjectPanelAction requestComp = ref gameObjectPanelRequestPool.Value.Add(requestEntity);

            //Заполняем данные запроса
            requestComp = new(
                requestType,
                objectPE,
                isRefresh);
        }

        void FactionSbpnCheckRefresh()
        {
            //Берём панель объекта
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //Берём подпанель фракции
            UIFactionSubpanel factionSubpanel = objectPanel.factionSubpanel;

            //Если активна обзорная вкладка
            if(factionSubpanel.tabGroup.selectedTab == factionSubpanel.overviewTab.selfTabButton)
            {
                //Запрашиваем обновление обзорной вкладки
                ObjPnActionRequest(
                    ObjectPanelActionRequestType.FactionOverview,
                    objectPanel.activeObjectPE);
            }
        }

        void RegionSbpnCheckRefresh()
        {
            //Берём панель объекта
            UIObjectPanel objectPanel = sOUI.Value.gameWindow.objectPanel;

            //Берём подпанель региона
            UIRegionSubpanel regionSubpanel = objectPanel.regionSubpanel;

            //Если активна обзорная вкладка
            if (regionSubpanel.tabGroup.selectedTab == regionSubpanel.overviewTab.selfTabButton)
            {
                //Запрашиваем обновление обзорной вкладки
                ObjPnActionRequest(
                    ObjectPanelActionRequestType.RegionOverview,
                    objectPanel.activeObjectPE);
            }
        }
        #endregion
        #endregion
    }
}