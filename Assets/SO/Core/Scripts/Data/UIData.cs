
using UnityEngine;
using UnityEngine.UI;

using SO.UI.Game.Map;
using SO.UI.Game.GUI.Object.Events;
using SO.Map.Hexasphere;

namespace SO.UI
{
    public class UIData : MonoBehaviour
    {
        public GORegion regionPrefab;
        public GORegionRenderer regionRendererPrefab;
        public VerticalLayoutGroup mapPanelGroup;
        public float mapPanelAltitude;

        public UIRCMainMapPanel rCMainMapPanelPrefab;

        public Game.GUI.Object.FleetManager.Fleets.UITaskForceSummaryPanel fMSbpnFleetsTabTaskForceSummaryPanelPrefab;
        public UITFMainMapPanel tFMainMapPanelPrefab;

        public ObjectPanelActionRequestType characterSubpanelDefaultTab;
        public ObjectPanelActionRequestType regionSubpanelDefaultTab;
        public ObjectPanelActionRequestType fleetManagerSubpanelDefaultTab;
    }
}