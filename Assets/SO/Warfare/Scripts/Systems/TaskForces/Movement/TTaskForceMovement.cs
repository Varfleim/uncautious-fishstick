
using Leopotam.EcsLite;
using Leopotam.EcsLite.Threads;

using SO.Map.Province;

namespace SO.Warfare.Fleet.Movement
{
    public struct TTaskForceMovement : IEcsThread<
        CTaskForceMovement, CTaskForce,
        CProvinceCore>
    {
        public EcsWorld world;

        int[] tFEntities;

        CTaskForce[] tFPool;
        int[] tFIndices;

        CTaskForceMovement[] tFMovementPool;
        int[] tFMovementIndices;

        CProvinceCore[] pCPool;
        int[] pCIndices;

        public void Init(
            int[] entities,
            CTaskForceMovement[] pool1, int[] indices1,
            CTaskForce[] pool2, int[] indices2,
            CProvinceCore[] pool3, int[] indices3)
        {
            tFEntities = entities;

            tFMovementPool = pool1;
            tFMovementIndices = indices1;

            tFPool = pool2;
            tFIndices = indices2;

            pCPool = pool3;
            pCIndices = indices3;
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

                //Если предыдущая провинция не пуста
                if(tF.previousProvincePE.Unpack(world, out int previousProvinceEntity))
                {
                    //Обнуляем предыдущую провинцию
                    tF.previousProvincePE = new();
                }

                //Если маршрут группы не пуст
                if(tFMovement.pathProvincePEs.Count > 0)
                {
                    //Берём последнюю провинцию в маршруте, то есть следующую провинцию пути
                    tFMovement.pathProvincePEs[tFMovement.pathProvincePEs.Count - 1].Unpack(world, out int nextProvinceEntity);
                    ref CProvinceCore nextProvince = ref pCPool[pCIndices[nextProvinceEntity]];

                    //Рассчитываем скорость с учётом состава оперативной группы и особенностей следующей провинции
                    float movementSpeed = 50;

                    //Прибавляем скорость к пройденному расстоянию
                    tFMovement.traveledDistance += movementSpeed;

                    //Если пройденное расстояние больше или равно расстоянию между провинциями
                    if (tFMovement.traveledDistance >= ProvincesData.provinceDistance)
                    {
                        //То группа переходит в следующую провинцию

                        //Отмечаем, что группа завершила перемещение
                        tFMovement.isTraveled = true;

                        //Обнуляем пройденное расстояние
                        tFMovement.traveledDistance = 0;

                        UnityEngine.Debug.LogWarning("Finish 1! " + nextProvince.Index + " ! " + tF.selfPE.Id);
                    }
                }
                //Иначе
                else
                {
                    //Группа уже находится в целевой провинции (что возможно только при изначально нулевом пути)

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