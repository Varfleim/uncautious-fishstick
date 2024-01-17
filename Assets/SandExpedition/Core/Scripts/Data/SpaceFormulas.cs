
using System.Collections.Generic;

using UnityEngine;

namespace SandOcean.Map
{
    public class SpaceFormulas
    {
        public static Vector3 PolarToCartesian(float distance, float angle)
        {
            //��������� ���������� �� �������� � �������������
            Vector3 cartesianPosition = new Vector3(distance * Mathf.Cos(angle), distance * Mathf.Sin(angle), 0);

            return cartesianPosition;
        }

        public static float RandomAngle()
        {
            //�������� ���������� �������� ����� 0 � 2*PI
            float angle = UnityEngine.Random.Range(0, 2 * Mathf.PI);

            return angle;
        }

        public static float SpiralAngle(float armAngle, float sectorAngle)
        {
            //��������� ���������� ���� ��� ����������� ����������� ����
            float angle = armAngle + sectorAngle;

            return angle;
        }

        public static Vector3 RandomPosition(float minRadius, float maxRadius)
        {
            //�������� ���������� ���������� �� �������� ��������
            float distance = UnityEngine.Random.Range(minRadius, maxRadius);
            //�������� ���������� ����
            float angle = RandomAngle();

            //��������� ���������� �� �������� � �������������
            Vector3 cartesianPosition = PolarToCartesian(distance, angle);

            return cartesianPosition;
        }

        public static List<Vector2> GetRandomVector2Points(
            int number,
            float maxCoordinateX,
            float maxCoordinateY)
        {
            List<Vector2> points
                = new();

            for (int a = 0; a < number; a++)
            {
                points.Add(
                    new Vector2(
                        UnityEngine.Random.Range(
                            0,
                            maxCoordinateX),
                        UnityEngine.Random.Range(
                            0,
                            maxCoordinateY)));
            }

            return points;
        }
    }
}