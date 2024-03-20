
using Leopotam.EcsLite;

namespace SO.UI.Game.GUI.Object.Events
{
    public enum ObjectPanelActionRequestType : byte
    {
        CharacterOverview,

        RegionOverview,

        StrategicAreaOverview,
        StrategicAreaRegions,

        FleetManagerFleets,

        Close
    }

    public readonly struct RGameObjectPanelAction
    {
        public RGameObjectPanelAction(
            ObjectPanelActionRequestType requestType,
            EcsPackedEntity objectPE)
        {
            this.requestType = requestType;

            this.objectPE = objectPE;
        }

        public readonly ObjectPanelActionRequestType requestType;

        public readonly EcsPackedEntity objectPE;
    }
}