namespace SCM.UI.Events
{
    public enum GeneralActionType : byte
    {
        None,
        QuitGame
    }

    public readonly struct RGeneralAction
    {
        public RGeneralAction(
            GeneralActionType actionType)
        {
            this.actionType = actionType;
        }

        public readonly GeneralActionType actionType;
    }
}