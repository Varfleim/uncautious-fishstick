
using UnityEditor;

using UnityEngine;

namespace SandOcean
{
    public class TextureArrayWizard : ScriptableWizard
    {
        public Texture2D[] textures;

        [MenuItem("Assets/Create/Texture Array")]
        static void CreateWizard()
        {
            ScriptableWizard.DisplayWizard<TextureArrayWizard>(
                "Create Texture Array", "Create");
        }

        void OnWizardCreate()
        {
            //���� ������ ������� ����
            if (textures.Length == 0)
            {
                //������� �� �������
                return;
            }

            string path = EditorUtility.SaveFilePanelInProject(
                "Save Texture Array", "Texture Array", "asset", "Save Texture Array");

            if (path.Length == 0)
            {
                return;
            }

            //������ ������ �������
            Texture2D t = textures[0];
            Texture2DArray textureArray = new Texture2DArray(
                t.width, t.height, textures.Length, t.format, t.mipmapCount > 1);
            //����������� ������ �������
            textureArray.anisoLevel = t.anisoLevel;
            textureArray.filterMode = t.filterMode;
            textureArray.wrapMode = t.wrapMode;

            //��� ������ ��������
            for (int a = 0; a < textures.Length; a++)
            {
                //��� ������ ���-�����
                for (int b = 0; b < t.mipmapCount; b++)
                {
                    Graphics.CopyTexture(textures[a], 0, b, textureArray, a, b);
                }
            }

            AssetDatabase.CreateAsset(textureArray, path);
        }
    }
}