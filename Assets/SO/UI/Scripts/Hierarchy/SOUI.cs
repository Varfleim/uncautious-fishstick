
using UnityEngine;

namespace SO.UI
{
    public enum MainWindowType : byte
    {
        None,
        MainMenu,
        Game
    }

    public class SOUI : MonoBehaviour
    {
        public MainWindowType activeMainWindowType;

        public UIAMainWindow activeMainWindow;

        public UIMainMenuWindow mainMenuWindow;

        public UIGameWindow gameWindow;
    }
}