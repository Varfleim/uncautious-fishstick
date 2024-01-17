
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using Leopotam.EcsLite.ExtendedSystems;

namespace SCM
{
    public class SEventControl : IEcsRunSystem
    {
        //Миры
        readonly EcsWorldInject world = default;

        //Общие события
        readonly EcsFilterInject<Inc<RStartNewGame>> startNewGameRequestFilter = default;

        readonly EcsPoolInject<EcsGroupSystemState> ecsGroupSystemStatePool = default;

        public void Run(IEcsSystems systems)
        {
            //Для каждого запроса начала новой игры
            foreach(int requestEntity in startNewGameRequestFilter.Value)
            {
                //Запрашиваем выключение группы систем "NewGame"
                EcsGroupSystemStateEvent("NewGame", false);

                UnityEngine.Debug.LogWarning("Окончание запуска новой игры");

                world.Value.DelEntity(requestEntity);
            }
        }

        void EcsGroupSystemStateEvent(
            string systemGroupName,
            bool systemGroupState)
        {
            //Создаём новую сущность и назначаем ей событие смены состояния группы систем
            int eventEntity = world.Value.NewEntity();
            ref EcsGroupSystemState eventComp = ref ecsGroupSystemStatePool.Value.Add(eventEntity);

            //Указываем название группы систем и нужное состояние
            eventComp.Name = systemGroupName;
            eventComp.State = systemGroupState;
        }
    }
}