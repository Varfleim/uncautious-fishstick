
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
            EcsPackedEntity objectPE, EcsPackedEntity secondObjectPE)
        {
            this.requestType = requestType;

            this.objectPE = objectPE;
            this.secondObjectPE = secondObjectPE;
        }

        public readonly ObjectPanelActionRequestType requestType;

        public readonly EcsPackedEntity objectPE;
        public readonly EcsPackedEntity secondObjectPE;
    }
}