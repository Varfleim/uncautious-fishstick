
namespace SO.UI.MainMenu.Events
{
    public enum MainMenuActionType : byte
    {
        None,
        OpenGame
    }

    public readonly struct RMainMenuAction
    {
        public RMainMenuAction(MainMenuActionType actionType)
        {
            this.actionType = actionType;
        }

        public readonly MainMenuActionType actionType;
    }
}