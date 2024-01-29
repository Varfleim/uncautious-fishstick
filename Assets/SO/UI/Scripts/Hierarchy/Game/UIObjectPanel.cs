
using UnityEngine;

using TMPro;

using Leopotam.EcsLite;
using SO.UI.Game.Object;

namespace SO.UI.Game
{
    public enum ObjectSubpanelType : byte
    {
        None,

        Faction,
        Region
    }

    public class UIObjectPanel : MonoBehaviour
    {
        public RectTransform titlePanel;
        public TextMeshProUGUI objectName;

        public ObjectSubpanelType activeObjectSubpanelType;
        public UIAObjectSubpanel activeObjectSubpanel;
        public EcsPackedEntity activeObjectPE;

        public UIFactionSubpanel factionSubpanel;
        public UIRegionSubpanel regionSubpanel;
    }
}