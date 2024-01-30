
using Leopotam.EcsLite;

namespace SO.Map.RFO
{
    public struct DFactionRFO
    {
        public DFactionRFO(
            EcsPackedEntity fRFOPE)
        {
            this.fRFOPE = fRFOPE;
        }

        public EcsPackedEntity fRFOPE;
    }
}