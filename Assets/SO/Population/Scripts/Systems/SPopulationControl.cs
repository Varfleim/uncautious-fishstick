
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

using SO.Map.Economy;
using SO.Map.Region;
using SO.Population.Events;

namespace SO.Population
{
    public class SPopulationControl : IEcsRunSystem
    {
        //����
        readonly EcsWorldInject world = default;


        //�����
        readonly EcsFilterInject<Inc<CRegionEconomy>> regionFilter = default;
        readonly EcsPoolInject<CRegionEconomy> rEPool = default;

        //���������
        readonly EcsPoolInject<CPopulation> pOPPool = default;

        public void Run(IEcsSystems systems)
        {
            //�������� ����� ���������
            PopulationCreating();
        }

        void PopulationCreating()
        {
            //��� ������� �������
            foreach(int currentRegionEntity in regionFilter.Value)
            {
                //���� ������
                ref CRegionEconomy currentRE = ref rEPool.Value.Get(currentRegionEntity);

                //���� � ������� ���� ���������� ������ ���������
                for(int a = 0; a < currentRE.orderedPOPs.Count; a++)
                {
                    //���� ���������� ���
                    DROrderedPopulation orderedPOP = currentRE.orderedPOPs[a];

                    //���������� ������, � ������� �������� ���
                    int targetRegionEntity;

                    //���� � ���� ��� �������� �������
                    if(orderedPOP.targetRegionPE.Unpack(world.Value, out targetRegionEntity) == false)
                    {
                        //��� �������� � ������� �������

                        //��������� � �������� �������� ������� ������
                        targetRegionEntity = currentRegionEntity;
                    }
                    //����� ��� �������� � ������� �������

                    //���� ������� ������
                    ref CRegionEconomy targetRE = ref rEPool.Value.Get(targetRegionEntity);

                    //���� � ������� ��� ���� ���������� ���
                    if(FindIdenticalPopulation(
                        ref targetRE,
                        ref orderedPOP,
                        out int oldPOPEntity))
                    {
                        //���� ���
                        ref CPopulation oldPOP = ref pOPPool.Value.Get(oldPOPEntity);

                        //��������� � ���� ���������
                        PopulationAddToOld(
                            ref orderedPOP,
                            ref oldPOP);
                    }
                    //�����
                    else
                    {
                        //������ ����� ��� � �������
                        PopulationCreateNew(
                            ref orderedPOP,
                            ref targetRE);
                    }
                }
                currentRE.orderedPOPs.Clear();
            }
        }

        bool FindIdenticalPopulation(
            ref CRegionEconomy rE,
            ref DROrderedPopulation orderedPOP,
            out int oldPOPEntity)
        {
            //����� �������������� ������
            oldPOPEntity = -1;

            //��� ������ ������ ��������� � �������
            for(int a = 0; a < rE.pOPs.Count; a++)
            {
                //���� ���
                rE.pOPs[a].Unpack(world.Value, out oldPOPEntity);
                ref CPopulation oldPOP = ref pOPPool.Value.Get(oldPOPEntity);

                //���� ������������ ��� ��������� ������������
                if(PopulationFunctions.IsPopulationIdentical(
                    ref orderedPOP, 
                    ref oldPOP))
                {
                    //����������, ��� ��� ���������� ���
                    return true;
                }
            }

            //����������, ��� ����������� ���� ���
            return false;
        }

        void PopulationCreateNew(
            ref DROrderedPopulation orderedPOP,
            ref CRegionEconomy rE)
        {
            //������ ����� �������� � ��������� �� ��������� ����
            int pOPEntity = world.Value.NewEntity();
            ref CPopulation pOP = ref pOPPool.Value.Add(pOPEntity);

            //��������� �������� ������ ����
            pOP = new(
                world.Value.PackEntity(pOPEntity),
                orderedPOP.socialClassIndex);

            //��������� ���� ��� ���������
            pOP.UpdatePopulation(ref orderedPOP);

            //������� PE ���� � ������ �������
            rE.pOPs.Add(pOP.selfPE);

            //������ �������, ���������� � �������� ������ ����
            ObjectNewCreatedEvent(pOP.selfPE, ObjectNewCreatedType.Population);
        }

        void PopulationAddToOld(
            ref DROrderedPopulation orderedPOP,
            ref CPopulation oldPOP)
        {
            //��������� ���� ����� ���������
            oldPOP.UpdatePopulation(ref orderedPOP);
        }

        readonly EcsPoolInject<EObjectNewCreated> objectNewCreatedEventPool = default;
        void ObjectNewCreatedEvent(
            EcsPackedEntity objectPE, ObjectNewCreatedType objectType)
        {
            //������ ����� �������� � ��������� �� ������� �������� ������ �������
            int eventEntity = world.Value.NewEntity();
            ref EObjectNewCreated eventComp = ref objectNewCreatedEventPool.Value.Add(eventEntity);

            //��������� ������ �������
            eventComp = new(objectPE, objectType);
        }
    }
}