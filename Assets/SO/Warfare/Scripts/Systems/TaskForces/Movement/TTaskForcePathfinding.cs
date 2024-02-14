
using System.Collections.Generic;

using Leopotam.EcsLite;
using Leopotam.EcsLite.Threads;

using SO.Map;
using SO.Warfare.Fleet.Movement.Events;

namespace SO.Warfare.Fleet.Movement
{
    public struct TTaskForcePathfinding : IEcsThread<
        SRTaskForceFindPath,
        CTaskForce, CTaskForceMovement,
        CRegionCore>
    {
        public EcsWorld world;

        public RegionsData regionsData;

        int[] tFEntities;

        SRTaskForceFindPath[] tFFindPathSelfRequestPool;
        int[] tFFindPathSelfRequestIndices;

        CTaskForce[] tFPool;
        int[] tFIndices;

        CTaskForceMovement[] tFMovementPool;
        int[] tFMovementIndices;

        CRegionCore[] rCPool;
        int[] rCIndices;

        public void Init(
            int[] entities,
            SRTaskForceFindPath[] pool1, int[] indices1,
            CTaskForce[] pool2, int[] indices2,
            CTaskForceMovement[] pool3, int[] indices3,
            CRegionCore[] pool4, int[] indices4)
        {
            tFEntities = entities;

            tFFindPathSelfRequestPool = pool1;
            tFFindPathSelfRequestIndices = indices1;

            tFPool = pool2;
            tFIndices = indices2;

            tFMovementPool = pool3;
            tFMovementIndices = indices3;

            rCPool = pool4;
            rCIndices = indices4;
        }

        public void Execute(int threadId, int fromIndex, int beforeIndex)
        {
            //Для каждой оперативной группы с самозапросом поиска пути
            for(int a = fromIndex; a < beforeIndex; a++)
            {
                //Берём самозапрос, группу и компонент движения
                int tFEntity = tFEntities[a];
                ref SRTaskForceFindPath selfRequestComp = ref tFFindPathSelfRequestPool[tFFindPathSelfRequestIndices[tFEntity]];
                ref CTaskForce tF = ref tFPool[tFIndices[tFEntity]];
                ref CTaskForceMovement tFMovement = ref tFMovementPool[tFMovementIndices[tFEntity]];

                //Очищаем данные движения
                tFMovement.pathRegionPEs.Clear();

                //Берём текущий регион группы
                tF.currentRegionPE.Unpack(world, out int startRegionEntity);
                ref CRegionCore startRC = ref rCPool[rCIndices[startRegionEntity]];

                //Берём целевой регион
                selfRequestComp.targetRegionPE.Unpack(world, out int endRegionEntity);
                ref CRegionCore endRC = ref rCPool[rCIndices[endRegionEntity]];

                //Находим путь
                List<int> path = regionsData.PathFindThreads(
                    world,
                    ref rCPool, ref rCIndices,
                    threadId,
                    ref startRC, ref endRC);

                //Если путь не пуст
                if(path != null)
                {
                    //Для каждого региона в пути
                    for(int b = 0; b < path.Count; b++)
                    {
                        //Заносим регион в список PE
                        tFMovement.pathRegionPEs.Add(regionsData.regionPEs[path[b]]);

                        UnityEngine.Debug.LogWarning(path[b]);
                    }
                }

                //Возвращаем список в пул
                ListPool<int>.Add(
                    threadId,
                    path);
            }
        }
    }
}