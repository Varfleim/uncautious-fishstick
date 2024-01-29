
using UnityEngine;

namespace SO.UI
{
    public class TextureScaler
    {
        public static void Scale(
            Texture2D texture,
            int width, int height,
            FilterMode mode = FilterMode.Trilinear)
        {
            //���� �������� ��������
            RenderTexture currentActiveRT = RenderTexture.active;

            //������ ������������� ��� �����
            Rect texRect = new Rect(0, 0, width, height);
            //����������� ���������� ��������
            _gpu_scale(
                texture,
                width, height,
                mode);

            //��������� ��������
            texture.Reinitialize(width, height);
            texture.ReadPixels(texRect, 0, 0, true);
            texture.Apply(true);

            //���������� ��������
            RenderTexture.active = currentActiveRT;
        }

        static void _gpu_scale(
            Texture2D source,
            int width, int height,
            FilterMode fmode)
        {
            //"��� ����� �������� �������� � VRAM, ������ ��� �� �������� � ������� ��"
            //We need the source texture in VRAM because we render with it
            source.filterMode = fmode;
            source.Apply(true);

            //"���������� RTT ��� ������ �������� � ������������������"
            //Using RTT for best quality and performance. Thanks, Unity 5
            RenderTexture rtt = new RenderTexture(
                width, height,
                32);

            //"������������� RTT � �������, ����� ��������� � ����"
            //Set the RTT in order to render to it
            Graphics.SetRenderTarget(rtt);

            //"������������� 2D-������� � ��������� 0..1, ����� ������ �� ����� ���� ������������ � �������"
            //Setup 2D matrix in range 0..1, so nobody needs to care about sized
            GL.LoadPixelMatrix(0, 1, 1, 0);

            //"����� ������� � ������ ��������, ����� ��������� RTT"
            //Then clear & draw the texture to fill the entire RTT.
            GL.Clear(true, true,
                new Color(0, 0, 0, 0));
            Graphics.DrawTexture(
                new Rect(0, 0, 1, 1),
                source);

        }
    }
}