namespace SCM.UI.Game.Events
{
    public enum GameActionType : byte
    {
        None,
        PauseOn,
        PauseOff
    }

    public readonly struct RGameAction
    {
        public RGameAction(
            GameActionType actionType)
        {
            this.actionType = actionType;
        }

        public readonly GameActionType actionType;
    }
}