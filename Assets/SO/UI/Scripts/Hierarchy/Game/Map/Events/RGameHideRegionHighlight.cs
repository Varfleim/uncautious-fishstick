
using Leopotam.EcsLite;

namespace SO.UI.Game.Map.Events
{
    public readonly struct RGameHideRegionHighlight
    {
        public RGameHideRegionHighlight(
            EcsPackedEntity regionPE,
            RegionHighlightRequestType requestType)
        {
            this.regionPE = regionPE;

            this.requestType = requestType;
        }

        public readonly EcsPackedEntity regionPE;

        public readonly RegionHighlightRequestType requestType;
    }
}