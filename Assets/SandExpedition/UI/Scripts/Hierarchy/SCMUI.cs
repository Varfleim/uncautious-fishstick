
using UnityEngine;

namespace SCM.UI
{
    public enum MainWindowType : byte
    {
        None,
        MainMenu,
        Game
    }

    public class SCMUI : MonoBehaviour
    {
        public MainWindowType activeMainWindowType;

        public UIAMainWindow activeMainWindow;

        public UIMainMenuWindow mainMenuWindow;

        public UIGameWindow gameWindow;
    }
}