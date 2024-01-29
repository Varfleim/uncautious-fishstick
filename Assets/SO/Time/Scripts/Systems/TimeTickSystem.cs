using System;

using UnityEngine;

namespace SO.Time
{
    public static class TimeTickSystem
    {
        public class OnTickEventArgs : EventArgs
        {
            public int tick;
        }
        public static event EventHandler<OnTickEventArgs> OnTick;

        public static double TickTimer
        {
            get
            {
                return TICK_TIMER_MAX;
            }
        }
        private const double TICK_TIMER_MAX = 1.0d;

        private static GameObject timeTickSystemObject;
        private static int tick;

        public static void Create()
        {
            if (timeTickSystemObject == null)
            {
                timeTickSystemObject = new GameObject("TimeTickSystem");
                timeTickSystemObject.AddComponent<TimeTickSystemObject>();
            }
        }

        private class TimeTickSystemObject : MonoBehaviour
        {
            private double tickTimer;

            private void Awake()
            {
                tick = 0;
            }

            private void Update()
            {
                tickTimer += UnityEngine.Time.deltaTime;
                if (tickTimer >= TICK_TIMER_MAX)
                {
                    tickTimer -= TICK_TIMER_MAX;
                    tick++;

                    if (OnTick != null)
                    {
                        OnTick(this, new OnTickEventArgs { tick = tick });
                    }
                }
            }
        }
    }
}