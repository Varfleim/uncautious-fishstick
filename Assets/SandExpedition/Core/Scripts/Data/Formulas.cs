
using System;

namespace SCM
{
    public class Formulas
    {
        public static System.Random random;

        public static double RandomGaussian(double mean, double standardDeviation)
        {
            double x1 = 1 - random.NextDouble();
            double x2 = 1 - random.NextDouble();

            double y1 = Math.Sqrt(-2.0 * Math.Log(x1)) * Math.Cos(2.0 * Math.PI * x2);
            return y1 * standardDeviation + mean;
        }

        public static double RandomHalfGaussian(double mean, double standardDeviation)
        {
            return Math.Abs(RandomGaussian(mean, standardDeviation));
        }
    }
}