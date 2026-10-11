using System.IO;
using UnityEditor;
using UnityEngine;

namespace PerspectivePuzzle.Editor
{
    public static class CoplayCharacterTextureSetup
    {
        const string DiffusePath = "Assets/Art/Models/Characters/Ch09_1001_Diffuse.png";
        const string NormalPath = "Assets/Art/Models/Characters/Ch09_1001_Normal.png";
        const string SpecularPath = "Assets/Art/Models/Characters/Ch09_1001_Specular.png";
        const string GlossPath = "Assets/Art/Models/Characters/Ch09_1001_Glossiness.png";
        const string MaterialPath = "Assets/Art/Materials/Mat_Character_Ch09.mat";

        static readonly string[] CharacterFbxs =
        {
            "Assets/Art/Models/Characters/Character_Ch09_nonPBR.fbx",
            "Assets/Art/Characters/StandingIdle.fbx",
            "Assets/Art/Characters/Walk.fbx",
            "Assets/Art/Characters/Walking.fbx",
            "Assets/Art/Characters/Carry.fbx",
            "Assets/Art/Characters/Falling.fbx",
            "Assets/Art/Characters/Fall.fbx"
        };

        [MenuItem("Tools/Coplay/Setup Character Textures")]
        public static void SetupTexturesAndMaterials()
        {
            Debug.Log("[Coplay] Starting Character Texture & Material Setup...");
            AssetDatabase.Refresh();

            // 1. Configure Diffuse Texture
            var diffImporter = AssetImporter.GetAtPath(DiffusePath) as TextureImporter;
            if (diffImporter != null)
            {
                diffImporter.textureType = TextureImporterType.Default;
                diffImporter.sRGBTexture = true;
                diffImporter.maxTextureSize = 2048;
                diffImporter.textureCompression = TextureImporterCompression.CompressedHQ;
                diffImporter.SaveAndReimport();
            }

            // 2. Configure Normal Texture
            var normImporter = AssetImporter.GetAtPath(NormalPath) as TextureImporter;
            if (normImporter != null)
            {
                normImporter.textureType = TextureImporterType.NormalMap;
                normImporter.sRGBTexture = false;
                normImporter.maxTextureSize = 2048;
                normImporter.textureCompression = TextureImporterCompression.CompressedHQ;
                normImporter.SaveAndReimport();
            }

            // 3. Configure Specular Texture
            var specImporter = AssetImporter.GetAtPath(SpecularPath) as TextureImporter;
            if (specImporter != null)
            {
                specImporter.textureType = TextureImporterType.Default;
                specImporter.sRGBTexture = true;
                specImporter.maxTextureSize = 2048;
                specImporter.SaveAndReimport();
            }

            // 4. Create or Update Material
            Texture2D diffTex = AssetDatabase.LoadAssetAtPath<Texture2D>(DiffusePath);
            Texture2D normTex = AssetDatabase.LoadAssetAtPath<Texture2D>(NormalPath);
            Texture2D specTex = AssetDatabase.LoadAssetAtPath<Texture2D>(SpecularPath);

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (mat == null)
            {
                Shader shader = Shader.Find("Standard (Specular setup)") ?? Shader.Find("Standard");
                mat = new Material(shader);
                mat.name = "Mat_Character_Ch09";
                AssetDatabase.CreateAsset(mat, MaterialPath);
            }

            mat.SetColor("_Color", Color.white);
            if (diffTex != null)
                mat.SetTexture("_MainTex", diffTex);
            if (normTex != null)
            {
                mat.EnableKeyword("_NORMALMAP");
                mat.SetTexture("_BumpMap", normTex);
                mat.SetFloat("_BumpScale", 0.85f);
            }
            if (specTex != null && mat.HasProperty("_SpecGlossMap"))
            {
                mat.EnableKeyword("_SPECGLOSSMAP");
                mat.SetTexture("_SpecGlossMap", specTex);
                mat.SetFloat("_Glossiness", 0.55f);
            }
            else
            {
                mat.SetFloat("_Glossiness", 0.4f);
            }

            EditorUtility.SetDirty(mat);

            // Also update Mat_Player.mat so any existing references get the textures!
            Material matPlayer = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Mat_Player.mat");
            if (matPlayer != null)
            {
                matPlayer.SetColor("_Color", Color.white);
                if (diffTex != null) matPlayer.SetTexture("_MainTex", diffTex);
                if (normTex != null)
                {
                    matPlayer.EnableKeyword("_NORMALMAP");
                    matPlayer.SetTexture("_BumpMap", normTex);
                }
                EditorUtility.SetDirty(matPlayer);
            }

            // 5. Remap Material in FBX files
            foreach (string fbx in CharacterFbxs)
            {
                var modelImporter = AssetImporter.GetAtPath(fbx) as ModelImporter;
                if (modelImporter != null)
                {
                    modelImporter.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                    modelImporter.materialLocation = ModelImporterMaterialLocation.External;
                    modelImporter.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "Ch09_body"), mat);
                    modelImporter.SaveAndReimport();
                    Debug.Log($"[Coplay] Remapped Ch09_body in {fbx} to {MaterialPath}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            UpdatePlayerPrefab();

            Debug.Log("[Coplay] Character Texture & Material Setup completed successfully!");
        }

        [MenuItem("Tools/Coplay/Update Player Prefab")]
        public static void UpdatePlayerPrefab()
        {
            string prefabPath = "Assets/Prefabs/Characters/Player.prefab";
            GameObject prefabContents = PrefabUtility.LoadPrefabContents(prefabPath);
            Transform placeholder = prefabContents.transform.Find("Placeholder_Body");
            if (placeholder != null)
            {
                Object.DestroyImmediate(placeholder.gameObject);
            }

            Transform existingBody = prefabContents.transform.Find("Body");
            if (existingBody != null)
            {
                Object.DestroyImmediate(existingBody.gameObject);
            }

            GameObject idleFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Characters/StandingIdle.fbx");
            if (idleFbx != null)
            {
                GameObject body = (GameObject)PrefabUtility.InstantiatePrefab(idleFbx, prefabContents.transform);
                body.name = "Body";
                body.transform.localPosition = Vector3.zero;
                body.transform.localRotation = Quaternion.identity;
                body.transform.localScale = Vector3.one * 0.95f;

                Material mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Mat_Character_Ch09.mat");
                if (mat != null)
                {
                    foreach (var r in body.GetComponentsInChildren<Renderer>(true))
                        r.sharedMaterial = mat;
                }

                foreach (var c in body.GetComponentsInChildren<Collider>(true))
                    Object.DestroyImmediate(c);
            }

            PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabPath);
            PrefabUtility.UnloadPrefabContents(prefabContents);
            Debug.Log("[Coplay] Player.prefab updated with textured Ch09 character!");
        }
    }
}

