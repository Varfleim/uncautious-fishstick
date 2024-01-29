
using UnityEngine;

using SO.UI.Game;

namespace SO.UI
{
    public enum MainPanelType : byte
    {
        None,

        Object
    }

    public class UIGameWindow : UIAMainWindow
    {
        public MainPanelType activeMainPanelType;
        public GameObject activeMainPanel;

        public UIObjectPanel objectPanel;
    }
}