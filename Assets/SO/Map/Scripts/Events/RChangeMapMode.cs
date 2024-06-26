namespace SO.Map.Events
{
    public enum ChangeMapModeRequestType : byte
    {
        None,
        Terrain,
        MapArea,
        Country,
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