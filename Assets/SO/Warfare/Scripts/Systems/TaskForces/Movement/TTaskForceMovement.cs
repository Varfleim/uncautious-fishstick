
using Leopotam.EcsLite;
using Leopotam.EcsLite.Threads;

using SO.Map.Region;

namespace SO.Warfare.Fleet.Movement
{
    public struct TTaskForceMovement : IEcsThread<
        CTaskForceMovement, CTaskForce,
        CRegionCore>
    {
        public EcsWorld world;

        int[] tFEntities;

        CTaskForce[] tFPool;
        int[] tFIndices;

        CTaskForceMovement[] tFMovementPool;
        int[] tFMovementIndices;

        CRegionCore[] regionPool;
        int[] regionIndices;

        public void Init(
            int[] entities,
            CTaskForceMovement[] pool1, int[] indices1,
            CTaskForce[] pool2, int[] indices2,
            CRegionCore[] pool3, int[] indices3)
        {
            tFEntities = entities;

            tFMovementPool = pool1;
            tFMovementIndices = indices1;

            tFPool = pool2;
            tFIndices = indices2;

            regionPool = pool3;
            regionIndices = indices3;
        }

        public void Execute(int threadId, int fromIndex, int beforeIndex)
        {
            //Для кажлой оперативной группы с компонентом движения
            for (int a = fromIndex; a < beforeIndex; a++)
            {
                //Берём компонент движения и группу
                int tFEntity = tFEntities[a];
                ref CTaskForceMovement tFMovement = ref tFMovementPool[tFMovementIndices[tFEntity]];
                ref CTaskForce tF = ref tFPool[tFIndices[tFEntity]];

                //Если предыдущий регион не пуст
                if(tF.previousRegionPE.Unpack(world, out int previousRegionEntity))
                {
                    //Обнуляем предыдущий регион
                    tF.previousRegionPE = new();
                }

                //Если маршрут группы не пуст
                if(tFMovement.pathRegionPEs.Count > 0)
                {
                    //Берём последний регион в маршруте, то есть следующий регион пути
                    tFMovement.pathRegionPEs[tFMovement.pathRegionPEs.Count - 1].Unpack(world, out int nextRegionEntity);
                    ref CRegionCore nextRegion = ref regionPool[regionIndices[nextRegionEntity]];

                    //Рассчитываем скорость с учётом состава оперативной группы и особенностей следующего региона
                    float movementSpeed = 50;

                    //Прибавляем скорость к пройденному расстоянию
                    tFMovement.traveledDistance += movementSpeed;

                    //Если пройденное расстояние больше или равно расстоянию между регионами
                    if (tFMovement.traveledDistance >= RegionsData.regionDistance)
                    {
                        //То группа переходит в следующий регион

                        //Отмечаем, что группа завершила перемещение
                        tFMovement.isTraveled = true;

                        //Обнуляем пройденное расстояние
                        tFMovement.traveledDistance = 0;

                        UnityEngine.Debug.LogWarning("Finish 1! " + nextRegion.Index + " ! " + tF.selfPE.Id);
                    }
                }
                //Иначе
                else
                {
                    //Группа уже находится в целевом регионе (что возможно только при изначально нулевом пути)

                    //Отмечаем, что группа завершила движение
                    tFMovement.isTraveled = true;

                    //Обнуляем пройденное расстояние
                    tFMovement.traveledDistance = 0;

                    UnityEngine.Debug.LogWarning("Finish 2! " + tF.selfPE.Id);
                }
            }
        }
    }
}