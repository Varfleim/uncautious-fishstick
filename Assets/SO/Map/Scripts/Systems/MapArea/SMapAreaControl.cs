
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Country;
using SO.Map.Events;

namespace SO.Map.MapArea
{
    public class SMapAreaControl : IEcsRunSystem
    {
        //����
        readonly EcsWorldInject world = default;


        //�����
        readonly EcsPoolInject<CMapArea> mAPool = default;


        //������
        readonly EcsPoolInject<CCountry> countryPool = default;

        public void Run(IEcsSystems systems)
        {
            //����� ���������� �������� �����
            MapAreaChangeOwner();
        }

        readonly EcsFilterInject<Inc<RMapAreaChangeOwner>> mAChangeOwnerRequestFilter = default;
        readonly EcsPoolInject<RMapAreaChangeOwner> mAChangeOwnerRequestPool = default;
        void MapAreaChangeOwner()
        {
            //��� ������� ������� ����� ��������� ������� �����
            foreach (int requestEntity in mAChangeOwnerRequestFilter.Value)
            {
                //���� ������
                ref RMapAreaChangeOwner requestComp = ref mAChangeOwnerRequestPool.Value.Get(requestEntity);

                //���� ������, ������� ���������� ���������� �������
                requestComp.countryPE.Unpack(world.Value, out int countryEntity);
                ref CCountry newOwnerCountry = ref countryPool.Value.Get(countryEntity);

                //���� �������
                requestComp.mAPE.Unpack(world.Value, out int mAEntity);
                ref CMapArea mA = ref mAPool.Value.Get(mAEntity);

                //���� ����� ��������� ���������� ��� �������������
                if(requestComp.requestType == MapAreaChangeOwnerType.Initialization)
                {
                    MapAreaChangeOwnerInitialization();
                }


                //����������� �������, ����� ��� �������� ������� �� ������, ��������� ��� ���������� ��� PE
                //������ �������, ���������� � ����� ��������� �������
                MapAreaChangeOwnerEvent(
                    mA.selfPE,
                    newOwnerCountry.selfPE, mA.ownerCountryPE);


                //��������� ������-��������� �������
                mA.ownerCountryPE = newOwnerCountry.selfPE;

                //����
                //������� PE ������� � ������ ������
                newOwnerCountry.ownedMAPEs.Add(mA.selfPE);
                //����

                mAChangeOwnerRequestPool.Value.Del(requestEntity);
            }
        }

        void MapAreaChangeOwnerInitialization()
        {

        }

        readonly EcsPoolInject<EMapAreaChangeOwner> mAChangeOwnerEventPool = default;
        void MapAreaChangeOwnerEvent(
            EcsPackedEntity mAPE,
            EcsPackedEntity newOwnerCountryPE, EcsPackedEntity oldOwnerCountryPE)
        {
            //������ ����� �������� � ��������� �� ������� ����� ��������� ������� �����
            int eventEntity = world.Value.NewEntity();
            ref EMapAreaChangeOwner eventComp = ref mAChangeOwnerEventPool.Value.Add(eventEntity);

            //��������� ������ �������
            eventComp = new(
                mAPE,
                newOwnerCountryPE, oldOwnerCountryPE);
        }
    }
}