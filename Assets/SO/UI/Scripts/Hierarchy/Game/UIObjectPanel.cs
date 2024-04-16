
using UnityEngine;

using TMPro;

using SO.UI.Game.GUI.Object;

namespace SO.UI.Game.GUI
{
    public enum ObjectSubpanelType : byte
    {
        None,

        Country,
        MapArea,
        Province,
        FleetManager
    }

    public class UIObjectPanel : MonoBehaviour
    {
        public RectTransform titlePanel;
        public TextMeshProUGUI objectName;

        public ObjectSubpanelType activeSubpanelType;
        public UIAObjectSubpanel activeSubpanel;

        public UICountrySubpanel countrySubpanel;
        public UIMapAreaSubpanel mapAreaSubpanel;
        public UIProvinceSubpanel provinceSubpanel;
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