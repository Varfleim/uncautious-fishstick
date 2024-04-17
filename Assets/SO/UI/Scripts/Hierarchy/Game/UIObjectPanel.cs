
using UnityEngine;

using TMPro;

using SO.UI.Game.GUI.Object;

namespace SO.UI.Game.GUI
{
    public enum ObjectSubpanelType : byte
    {
        None,

        Country,
        State,
        FleetManager
    }

    public class UIObjectPanel : MonoBehaviour
    {
        public RectTransform titlePanel;
        public TextMeshProUGUI objectName;

        public ObjectSubpanelType activeSubpanelType;
        public UIAObjectSubpanel activeSubpanel;

        public UICountrySubpanel countrySubpanel;
        public UIStateSubpanel stateSubpanel;
        public UIFleetManagerSubpanel fleetManagerSubpanel;

        public void HideActiveSubpanel()
        {
            //—крываем активную подпанель
            activeSubpanel.gameObject.SetActive(false);

            //”казываем, что нет активной подпанели
            activeSubpanelType = ObjectSubpanelType.None;
            activeSubpanel = null;
        }
    }
}