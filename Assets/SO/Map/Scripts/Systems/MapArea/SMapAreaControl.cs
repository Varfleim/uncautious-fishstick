
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Map.Events.MapArea;
using SO.Map.Events.State;

namespace SO.Map.MapArea
{
    public class SMapAreaControl : IEcsRunSystem
    {
        //����
        readonly EcsWorldInject world = default;


        //�����
        readonly EcsPoolInject<CMapArea> mAPool = default;

        public void Run(IEcsSystems systems)
        {
            //����� ���������� ��� �����
            MapAreasChangeOwner();
        }

        readonly EcsFilterInject<Inc<RMapAreaChangeOwner>> mAChangeOwnerRequestFilter = default;
        readonly EcsPoolInject<RMapAreaChangeOwner> mAChangeOwnerRequestPool = default;
        void MapAreasChangeOwner()
        {
            //��� ������� ������� ����� ��������� ���� �����
            foreach (int requestEntity in mAChangeOwnerRequestFilter.Value)
            {
                //���� ������
                ref RMapAreaChangeOwner requestComp = ref mAChangeOwnerRequestPool.Value.Get(requestEntity);

                //���� ����
                requestComp.mAPE.Unpack(world.Value, out int mAEntity);
                ref CMapArea mA = ref mAPool.Value.Get(mAEntity);

                //������ ��������� ����
                MapAreaChangeOwner(
                    ref mA,
                    ref requestComp);

                mAChangeOwnerRequestPool.Value.Del(requestEntity);
            }
        }

        void MapAreaChangeOwner(
            ref CMapArea mA,
            ref RMapAreaChangeOwner requestComp)
        {
            //���� ����� ��������� ���������� ��� �������������
            if (requestComp.requestType == MapAreaChangeOwnerType.Initialization)
            {
                MapAreaChangeOwnerInitialization();
            }

            //����������� ����� ��������� ���� ��������, �� ������� ������� ���� �����
            MapAreaAllStatesChangeOwner(
                ref mA,
                requestComp.newOwnerCountryPE);

            //��������� ����������� �������� ���������� �������� ���������,
            //������� ����� ��������� ������ ���������� �� ������ ���������,
            //����� ���� �������� ���������
        }

        void MapAreaChangeOwnerInitialization()
        {

        }

        void MapAreaAllStatesChangeOwner(
            ref CMapArea mA,
            EcsPackedEntity newOwnerCountryPE)
        {
            //��� ������ ������� � ����
            for (int a = 0; a < mA.states.Count; a++)
            {
                //����������� ����� ��������� �������
                StateChangeOwnerRequest(
                    newOwnerCountryPE,
                    mA.states[a].statePE,
                    StateChangeOwnerType.Test);
            }
        }

        readonly EcsPoolInject<RStateChangeOwner> stateChangeOwnerRequestPool = default;
        void StateChangeOwnerRequest(
            EcsPackedEntity targetCountryPE,
            EcsPackedEntity statePE,
            StateChangeOwnerType requestType)
        {
            //������ ����� �������� � ��������� �� ������ ����� ��������� �������
            int requestEntity = world.Value.NewEntity();
            ref RStateChangeOwner requestComp = ref stateChangeOwnerRequestPool.Value.Add(requestEntity);

            //��������� ������ �������
            requestComp = new(
                targetCountryPE,
                statePE,
                requestType);
        }
    }
}