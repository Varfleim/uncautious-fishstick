using System;
using System.Collections.Generic;

using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Country;
using SO.Map.Events;
using SO.Map.Economy;

namespace SO.Map.Region
{
    public class SRegionControl : IEcsRunSystem
    {
        //����
        readonly EcsWorldInject world = default;


        //�����
        readonly EcsPoolInject<CRegionCore> rCPool = default;
        readonly EcsPoolInject<CRegionEconomy> rEPool = default;

        //������
        readonly EcsPoolInject<CCountry> countryPool = default;

        public void Run(IEcsSystems systems)
        {
            //����� ���������� ��������
            RegionChangeOwners();
        }

        readonly EcsFilterInject<Inc<RRegionChangeOwner>> regionChangeOwnerRequestFilter = default;
        readonly EcsPoolInject<RRegionChangeOwner> regionChangeOwnerRequestPool = default;
        void RegionChangeOwners()
        {
            //��� ������� ������� ����� ��������� �������
            foreach (int requestEntity in regionChangeOwnerRequestFilter.Value)
            {
                //���� ������
                ref RRegionChangeOwner requestComp = ref regionChangeOwnerRequestPool.Value.Get(requestEntity);

                //���� ������, ������� ���������� ���������� �������
                requestComp.countryPE.Unpack(world.Value, out int countryEntity);
                ref CCountry country = ref countryPool.Value.Get(countryEntity);

                //���� ������
                requestComp.regionPE.Unpack(world.Value, out int regionEntity);
                ref CRegionCore rC = ref rCPool.Value.Get(regionEntity);

                //���� ����� ��������� ���������� ��� �������������
                if (requestComp.requestType == RegionChangeOwnerType.Initialization)
                {
                    RCChangeOwnerInitialization();
                }


                //������ �������, ���������� � ����� ��������� �������
                RegionChangeOwnerEvent(
                    rC.selfPE,
                    country.selfPE, rC.ownerCountryPE);


                //��������� ������-��������� �������
                rC.ownerCountryPE = country.selfPE;

                //����
                //������� PE ������� � ������ ������
                country.ownedRCPEs.Add(rC.selfPE);

                //���� ������������� ��������� �������
                ref CRegionEconomy rE = ref rEPool.Value.Get(regionEntity);

                //������ �������� ������ ���������
                for(int a = 0; a < 5; a++)
                {
                    //������ ����� ������ �������� ����
                    Population.Events.DROrderedPopulation orderedPOP = new(
                        rE.selfPE,
                        0,
                        100);

                    //������� ��� � ������ ���������� �����
                    rE.orderedPOPs.Add(orderedPOP);
                }
                //����

                regionChangeOwnerRequestPool.Value.Del(requestEntity);
            }
        }

        void RCChangeOwnerInitialization()
        {

        }

        readonly EcsPoolInject<ERegionChangeOwner> regionChangeOwnerEventPool = default;
        void RegionChangeOwnerEvent(
            EcsPackedEntity regionPE,
            EcsPackedEntity newOwnerCountryPE, EcsPackedEntity oldOwnerCountryPE = new())
        {
            //������ ����� �������� � ��������� �� ������� ����� ��������� RC
            int eventEntity = world.Value.NewEntity();
            ref ERegionChangeOwner eventComp = ref regionChangeOwnerEventPool.Value.Add(eventEntity);

            //��������� ������ �������
            eventComp = new(
                regionPE,
                newOwnerCountryPE, oldOwnerCountryPE);
        }
    }
}