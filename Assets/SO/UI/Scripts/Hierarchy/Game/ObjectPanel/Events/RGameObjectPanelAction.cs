
using Leopotam.EcsLite;

namespace SO.UI.Game.Object.Events
{
    public enum ObjectPanelActionRequestType : byte
    {
        Faction,
        FactionOverview,

        Region,
        RegionOverview,

        Close
    }

    public readonly struct RGameObjectPanelAction
    {
        public RGameObjectPanelAction(
            ObjectPanelActionRequestType requestType, 
            EcsPackedEntity objectPE, 
            bool isRefresh)
        {
            this.requestType = requestType;
            
            this.objectPE = objectPE;
            
            this.isRefresh = isRefresh;
        }
        public readonly ObjectPanelActionRequestType requestType;

        public readonly EcsPackedEntity objectPE;

        public readonly bool isRefresh;
    }
}