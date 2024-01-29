
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Faction;
using SO.Economy.RFO.Events;

namespace SO.Economy.RFO
{
    public class SRFOControl : IEcsRunSystem
    {
        //����
        readonly EcsWorldInject world = default;


        //�������
        readonly EcsPoolInject<CFaction> factionPool = default;

        //���������
        readonly EcsPoolInject<CRegionFO> rFOPool = default;


        //������� ���������
        readonly EcsFilterInject<Inc<RRFOChangeOwner>> rFOChangeOwnerRequestFilter = default;
        readonly EcsPoolInject<RRFOChangeOwner> rFOChangeOwnerRequestPool = default;

        public void Run(IEcsSystems systems)
        {
            //����� ���������� RFO
            RFOChangeOwners();
        }

        void RFOChangeOwners()
        {
            //��� ������� ������� ����� ��������� RFO
            foreach(int requestEntity in rFOChangeOwnerRequestFilter.Value)
            {
                //���� ������
                ref RRFOChangeOwner requestComp = ref rFOChangeOwnerRequestPool.Value.Get(requestEntity);

                //���� �������, ������� ���������� ���������� RFO
                requestComp.factionPE.Unpack(world.Value, out int factionEntity);
                ref CFaction faction = ref factionPool.Value.Get(factionEntity);

                //���� RFO
                requestComp.regionPE.Unpack(world.Value, out int regionEntity);
                ref CRegionFO rFO = ref rFOPool.Value.Get(regionEntity);

                //���� ����� ��������� ���������� ��� �������������
                if(requestComp.actionType == RegionChangeOwnerType.Initialization)
                {
                    RFOChangeOwnerInitialization();
                }

                //��������� �������-��������� RFO
                rFO.ownerFactionPE = faction.selfPE;

                //������� PE RFO � ������ �������
                faction.ownedRFOPEs.Add(rFO.selfPE);

                rFOChangeOwnerRequestPool.Value.Del(requestEntity);
            }
        }

        void RFOChangeOwnerInitialization()
        {

        }
    }
}