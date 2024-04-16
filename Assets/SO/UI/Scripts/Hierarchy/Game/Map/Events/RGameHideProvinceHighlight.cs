
using Leopotam.EcsLite;

namespace SO.UI.Game.Map.Events
{
    public readonly struct RGameHideProvinceHighlight
    {
        public RGameHideProvinceHighlight(
            EcsPackedEntity provincePE,
            ProvinceHighlightRequestType requestType)
        {
            this.provincePE = provincePE;

            this.requestType = requestType;
        }

        public readonly EcsPackedEntity provincePE;

        public readonly ProvinceHighlightRequestType requestType;
    }
}