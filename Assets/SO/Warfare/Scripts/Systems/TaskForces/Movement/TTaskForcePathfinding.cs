
using System.Collections.Generic;

using Leopotam.EcsLite;
using Leopotam.EcsLite.Threads;

using SO.Map.Province;
using SO.Warfare.Fleet.Movement.Events;

namespace SO.Warfare.Fleet.Movement
{
    public struct TTaskForcePathfinding : IEcsThread<
        SRTaskForceFindPath,
        CTaskForce, CTaskForceMovement,
        CProvinceCore>
    {
        public EcsWorld world;

        public ProvincesData provincesData;

        int[] tFEntities;

        SRTaskForceFindPath[] tFFindPathSelfRequestPool;
        int[] tFFindPathSelfRequestIndices;

        CTaskForce[] tFPool;
        int[] tFIndices;

        CTaskForceMovement[] tFMovementPool;
        int[] tFMovementIndices;

        CProvinceCore[] pCPool;
        int[] pCIndices;

        public void Init(
            int[] entities,
            SRTaskForceFindPath[] pool1, int[] indices1,
            CTaskForce[] pool2, int[] indices2,
            CTaskForceMovement[] pool3, int[] indices3,
            CProvinceCore[] pool4, int[] indices4)
        {
            tFEntities = entities;

            tFFindPathSelfRequestPool = pool1;
            tFFindPathSelfRequestIndices = indices1;

            tFPool = pool2;
            tFIndices = indices2;

            tFMovementPool = pool3;
            tFMovementIndices = indices3;

            pCPool = pool4;
            pCIndices = indices4;
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
                tFMovement.pathProvincePEs.Clear();

                //Берём текущую провинцию группы
                tF.currentProvincePE.Unpack(world, out int startProvinceEntity);
                ref CProvinceCore startPC = ref pCPool[pCIndices[startProvinceEntity]];

                //Берём целевую провинцию
                selfRequestComp.targetProvincePE.Unpack(world, out int endProvinceEntity);
                ref CProvinceCore endPC = ref pCPool[pCIndices[endProvinceEntity]];

                //Находим путь
                List<int> path = provincesData.PathFindThreads(
                    world,
                    ref pCPool, ref pCIndices,
                    threadId,
                    ref startPC, ref endPC);

                //Если путь не пуст
                if(path != null)
                {
                    //Для каждой провинции в пути
                    for (int b = 0; b < path.Count; b++)
                    {
                        //Заносим провинцию в список PE
                        tFMovement.pathProvincePEs.Add(provincesData.provincePEs[path[b]]);

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