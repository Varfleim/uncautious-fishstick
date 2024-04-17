
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Map.Generation;
using SO.Map.MapArea;
using SO.Map.Events.State;

namespace SO.Map.State
{
    public class SMapState : IEcsRunSystem
    {
        //����
        readonly EcsWorldInject world = default;


        //�����
        readonly EcsFilterInject<Inc<CMapArea>> mAFilter = default;
        readonly EcsPoolInject<CMapArea> mAPool = default;


        //������� �����
        readonly EcsFilterInject<Inc<RMapGenerating>> mapGeneratingRequestFilter = default;
        readonly EcsPoolInject<RMapGenerating> mapGeneratingRequestPool = default;

        public void Run(IEcsSystems systems)
        {
            //��� ������� ������� ��������� �����
            foreach (int requestEntity in mapGeneratingRequestFilter.Value)
            {
                //���� ������
                ref RMapGenerating requestComp = ref mapGeneratingRequestPool.Value.Get(requestEntity);

                //������ ������ �������
                MapAreaEmptyStatesCreating();
            }
        }

        void MapAreaEmptyStatesCreating()
        {
            //��� ������ ���� �����
            foreach(int mAEntity in mAFilter.Value)
            {
                //���� ����
                ref CMapArea mA = ref mAPool.Value.Get(mAEntity);

                //����������� ��������� �������� ������� ��� ��
                StateStartCreatingRequest(ref mA);
            }
        }

        readonly EcsPoolInject<RStateStartCreating> stateStartCreatingRequestPool = default;
        void StateStartCreatingRequest(
            ref CMapArea mA)
        {
            //������ ����� �������� � ��������� �� ������ ���������� �������� �������
            int requestEntity = world.Value.NewEntity();
            ref RStateStartCreating requestComp = ref stateStartCreatingRequestPool.Value.Add(requestEntity);

            //��������� ������ �������
            requestComp = new(
                mA.selfPE);
        }
    }
}