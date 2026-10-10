#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace PerspectivePuzzle.Level1
{
    /// <summary>
    /// Collapses the full-house export into a handful of meshes.
    /// The source FBX is thousands of objects, which is why a 5070 still stalls.
    /// </summary>
    public static class HouseGameplayBaker
    {
        const string SourcePath = "Assets/Art/Models/Building/House_Full.fbx";
        const int MaxTextures = 12;
        const int TextureSize = 1024;

        struct Key
        {
            public int textureId;
            public int r;
            public int g;
            public int b;
        }

        public static string Ensure(string prefabPath)
        {
            if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(prefabPath)))
                return "gameplay-prefab-ready";
            return Bake(prefabPath);
        }

        public static string Bake(string prefabPath)
        {
            var importer = AssetImporter.GetAtPath(SourcePath) as ModelImporter;
            if (importer == null)
                return "missing " + SourcePath;

            importer.importCameras = false;
            importer.importLights = false;
            importer.importBlendShapes = false;
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.isReadable = true;
            importer.SaveAndReimport();

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath);
            if (prefab == null)
                return "source failed to import";

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject instance = Object.Instantiate(prefab);
            instance.name = "BakeSource";
            try
            {
                return BakeInstance(instance, prefabPath);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        static string BakeInstance(GameObject instance, string prefabPath)
        {
            var filters = instance.GetComponentsInChildren<MeshFilter>(true);
            var score = new Dictionary<Key, float>();
            var grouped = new Dictionary<Key, List<CombineInstance>>();
            int skipped = 0;

            foreach (MeshFilter filter in filters)
            {
                Mesh mesh = filter.sharedMesh;
                Renderer renderer = filter.GetComponent<Renderer>();
                if (mesh == null || renderer == null || !mesh.isReadable)
                {
                    skipped++;
                    continue;
                }

                Material source = renderer.sharedMaterial;
                Key key = KeyOf(source);
                if (!grouped.TryGetValue(key, out List<CombineInstance> list))
                {
                    list = new List<CombineInstance>();
                    grouped[key] = list;
                    score[key] = 0f;
                }

                score[key] += renderer.bounds.size.sqrMagnitude;
                list.Add(new CombineInstance
                {
                    mesh = mesh,
                    transform = filter.transform.localToWorldMatrix
                });
            }

            if (grouped.Count == 0)
                return "no readable meshes, skipped=" + skipped;

            List<Key> keep = new List<Key>(score.Keys);
            keep.Sort((a, b) => score[b].CompareTo(score[a]));
            if (keep.Count > MaxTextures)
                keep.RemoveRange(MaxTextures, keep.Count - MaxTextures);
            var keepSet = new HashSet<Key>(keep);
            Key fallback = keep[0];

            var merged = new Dictionary<Key, List<CombineInstance>>();
            foreach (KeyValuePair<Key, List<CombineInstance>> pair in grouped)
            {
                Key destination = keepSet.Contains(pair.Key) ? pair.Key : fallback;
                if (!merged.TryGetValue(destination, out List<CombineInstance> list))
                {
                    list = new List<CombineInstance>();
                    merged[destination] = list;
                }

                list.AddRange(pair.Value);
            }

            string dir = Path.GetDirectoryName(prefabPath).Replace('\\', '/');
            Directory.CreateDirectory(Path.Combine(Directory.GetParent(Application.dataPath).FullName, dir));
            AssetDatabase.Refresh();

            var textureCache = new Dictionary<int, Texture2D>();
            var root = new GameObject("House_Gameplay");
            int parts = 0;
            int tris = 0;
            foreach (KeyValuePair<Key, List<CombineInstance>> pair in merged)
            {
                Mesh combined = CombineInBatches(pair.Value);
                if (combined == null)
                    continue;

                tris += combined.triangles.Length / 3;
                combined.Optimize();
                combined.UploadMeshData(true);
                string meshPath = dir + "/HousePart_" + parts + ".asset";
                AssetDatabase.CreateAsset(combined, meshPath);

                Material material = MakeMaterial(pair.Key, filters, textureCache, dir, parts);
                var child = new GameObject("Part_" + parts);
                child.transform.SetParent(root.transform, false);
                child.AddComponent<MeshFilter>().sharedMesh = combined;
                child.AddComponent<MeshRenderer>().sharedMaterial = material;
                parts++;
            }

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            return "baked parts=" + parts + " tris=" + tris + " fromMeshes=" + filters.Length + " skipped=" + skipped;
        }

        static Mesh CombineInBatches(List<CombineInstance> source)
        {
            const int batch = 64;
            Mesh running = null;
            int index = 0;
            while (index < source.Count)
            {
                int count = Mathf.Min(batch, source.Count - index);
                var slice = new List<CombineInstance>(count + 1);
                if (running != null)
                {
                    slice.Add(new CombineInstance
                    {
                        mesh = running,
                        transform = Matrix4x4.identity
                    });
                }

                for (int i = 0; i < count; i++)
                    slice.Add(source[index + i]);
                index += count;

                var mesh = new Mesh
                {
                    name = "HouseCombined",
                    indexFormat = IndexFormat.UInt32
                };
                mesh.CombineMeshes(slice.ToArray(), true, true, false);
                if (running != null)
                    Object.DestroyImmediate(running);
                running = mesh;
            }

            if (running != null)
                running.RecalculateBounds();
            return running;
        }

        static Material MakeMaterial(Key key, MeshFilter[] filters, Dictionary<int, Texture2D> cache, string dir, int part)
        {
            Shader shader = Shader.Find("Standard") ?? Shader.Find("Diffuse");
            var material = new Material(shader);
            material.color = new Color(key.r / 4f, key.g / 4f, key.b / 4f, 1f);
            Texture source = FindTexture(key.textureId, filters);
            if (source != null)
            {
                if (!cache.TryGetValue(source.GetInstanceID(), out Texture2D small))
                {
                    small = Resize(source, TextureSize);
                    string png = dir + "/Tex_" + part + ".png";
                    File.WriteAllBytes(ToFull(png), small.EncodeToPNG());
                    Object.DestroyImmediate(small);
                    AssetDatabase.ImportAsset(png);
                    var textureImporter = AssetImporter.GetAtPath(png) as TextureImporter;
                    if (textureImporter != null)
                    {
                        textureImporter.maxTextureSize = TextureSize;
                        textureImporter.textureCompression = TextureImporterCompression.Compressed;
                        textureImporter.mipmapEnabled = true;
                        textureImporter.SaveAndReimport();
                    }

                    small = AssetDatabase.LoadAssetAtPath<Texture2D>(png);
                    cache[source.GetInstanceID()] = small;
                }

                material.mainTexture = small;
            }

            string matPath = dir + "/Mat_" + part + ".mat";
            AssetDatabase.CreateAsset(material, matPath);
            return material;
        }

        static Texture FindTexture(int textureId, MeshFilter[] filters)
        {
            if (textureId == 0)
                return null;
            foreach (MeshFilter filter in filters)
            {
                Renderer renderer = filter.GetComponent<Renderer>();
                if (renderer == null || renderer.sharedMaterial == null)
                    continue;
                Texture texture = MainTexture(renderer.sharedMaterial);
                if (texture != null && texture.GetInstanceID() == textureId)
                    return texture;
            }

            return null;
        }

        static Texture2D Resize(Texture source, int size)
        {
            float scale = size / (float)Mathf.Max(source.width, source.height);
            int width = Mathf.Max(1, Mathf.RoundToInt(source.width * Mathf.Min(1f, scale)));
            int height = Mathf.Max(1, Mathf.RoundToInt(source.height * Mathf.Min(1f, scale)));
            RenderTexture rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Graphics.Blit(source, rt);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            var copy = new Texture2D(width, height, TextureFormat.RGBA32, true, false);
            copy.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            copy.Apply(true, false);
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            return copy;
        }

        static Key KeyOf(Material material)
        {
            Texture texture = MainTexture(material);
            Color color = Color.white;
            if (material != null && material.HasProperty("_Color"))
                color = material.color;
            return new Key
            {
                textureId = texture != null ? texture.GetInstanceID() : 0,
                r = Mathf.Clamp(Mathf.RoundToInt(color.r * 4f), 0, 4),
                g = Mathf.Clamp(Mathf.RoundToInt(color.g * 4f), 0, 4),
                b = Mathf.Clamp(Mathf.RoundToInt(color.b * 4f), 0, 4)
            };
        }

        static Texture MainTexture(Material material)
        {
            if (material == null)
                return null;
            if (material.mainTexture != null)
                return material.mainTexture;
            string[] names = { "_MainTex", "_BaseMap", "_BaseColorMap" };
            for (int i = 0; i < names.Length; i++)
            {
                if (!material.HasProperty(names[i]))
                    continue;
                Texture texture = material.GetTexture(names[i]);
                if (texture != null)
                    return texture;
            }

            return null;
        }

        static string ToFull(string assetPath)
        {
            return Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath);
        }
    }
}
#endif
