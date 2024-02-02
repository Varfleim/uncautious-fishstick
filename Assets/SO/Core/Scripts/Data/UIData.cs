
using UnityEngine;
using UnityEngine.UI;

using SO.UI.Game.Map;
using SO.Map;

namespace SO.UI
{
    public class UIData : MonoBehaviour
    {
        public GORegion regionPrefab;
        public VerticalLayoutGroup mapPanelGroup;
        public float mapPanelAltitude;

        public UIRCMainMapPanel rCMainMapPanelPrefab;
    }
}