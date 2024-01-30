
namespace SO.Map.Events
{
    public enum ChangeMapModeRequestType: byte
    {
        None,

        Exploration
    }

    public struct RChangeMapMode
    {
        public RChangeMapMode(
            ChangeMapModeRequestType requestType)
        {
            this.requestType = requestType;
        }

        public ChangeMapModeRequestType requestType;
    }
}