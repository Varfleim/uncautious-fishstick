
using UnityEngine;
using UnityEngine.UI;

using SO.UI.Game.Map;
using SO.UI.Game.GUI.Object.Events;
using SO.Map.Hexasphere;

namespace SO.UI
{
    public class UIData : MonoBehaviour
    {
        public GOProvince provincePrefab;
        public GOProvinceRenderer provinceRendererPrefab;
        public VerticalLayoutGroup mapPanelGroup;
        public float mapPanelAltitude;

        public UIPCMainMapPanel pCMainMapPanelPrefab;

        public Game.GUI.Object.FleetManager.Fleets.UITaskForceSummaryPanel fMSbpnFleetsTabTaskForceSummaryPanelPrefab;
        public UITFMainMapPanel tFMainMapPanelPrefab;

        public ObjectPanelActionRequestType countrySubpanelDefaultTab;
        public ObjectPanelActionRequestType stateSubpanelDefaultTab;
        public ObjectPanelActionRequestType fleetManagerSubpanelDefaultTab;
    }
}