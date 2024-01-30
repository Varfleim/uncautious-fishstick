
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Map;
using SO.Map.RFO.Events;

namespace SO.Map.RFO
{
    public class SRFOInitialization : IEcsRunSystem
    {
        //����
        readonly EcsWorldInject world = default;


        //�����
        readonly EcsPoolInject<CRegion> regionPool = default;

        //���������
        readonly EcsPoolInject<CRegionFO> rFOPool = default;


        //������� ���������
        readonly EcsFilterInject<Inc<CRegion, SRRFOCreating>> rFOCreatingSelfRequestFilter = default;
        readonly EcsPoolInject<SRRFOCreating> rFOCreatingSelfRequestPool = default;

        public void Run(IEcsSystems systems)
        {
            //��� ������� ������� � ������������ �������� RFO
            foreach (int regionEntity in rFOCreatingSelfRequestFilter.Value)
            {
                //���� ������ � ����������
                ref CRegion region = ref regionPool.Value.Get(regionEntity);
                ref SRRFOCreating selfRequestComp = ref rFOCreatingSelfRequestPool.Value.Get(regionEntity);

                //������ RFO
                RFOCreating(
                    ref region, ref selfRequestComp);

                //������� ����������
                rFOCreatingSelfRequestPool.Value.Del(regionEntity);
            }
        }

        void RFOCreating(
            ref CRegion region, ref SRRFOCreating requestComp)
        {
            //���� �������� ������� � ��������� ��� ��������� RFO
            region.selfPE.Unpack(world.Value, out int regionEntity);
            ref CRegionFO rFO = ref rFOPool.Value.Add(regionEntity);

            //��������� �������� ������ RFO
            rFO = new(
                region.selfPE);
        }
    }
}