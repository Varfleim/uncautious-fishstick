
using UnityEngine;

using TMPro;

using Leopotam.EcsLite;

using SO.UI.Game.GUI.Object;

namespace SO.UI.Game.GUI
{
    public enum ObjectSubpanelType : byte
    {
        None,

        Character,
        StrategicArea,
        Region,
        FleetManager
    }

    public class UIObjectPanel : MonoBehaviour
    {
        public RectTransform titlePanel;
        public TextMeshProUGUI objectName;

        public ObjectSubpanelType activeSubpanelType;
        public UIAObjectSubpanel activeSubpanel;

        public UICharacterSubpanel characterSubpanel;
        public UIRegionSubpanel regionSubpanel;
        public UIStrategicAreaSubpanel strategicAreaSubpanel;
        public UIFleetManagerSubpanel fleetManagerSubpanel;

        public void HideActiveSubpanel()
        {
            //�������� �������� ���������
            activeSubpanel.gameObject.SetActive(false);

            //���������, ��� ��� �������� ���������
            activeSubpanelType = ObjectSubpanelType.None;
            activeSubpanel = null;
        }
    }
}