#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using PerspectivePuzzle.Movement;
using PerspectivePuzzle.Puzzle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace PerspectivePuzzle.Level1
{
    public static class Level1StageBuilder
    {
        const string GameplayPrefab = "Assets/Art/Models/Building/Gameplay/House_Gameplay.prefab";
        const string MapGlb = "Assets/Art/Models/Incoming/My project/Assets/GOEMGEM.glb";
        const string WalkModel = "Assets/Art/Characters/Walk.fbx";
        const string FallModel = "Assets/Art/Characters/Fall.fbx";
        const string LevelScene = "Assets/Scenes/Level1.unity";
        const string PreviewScene = "Assets/Scenes/ModelPreview.unity";

        public static string Build()
        {
            string baked;
            GameObject prefab = LoadMap(out baked);
            if (prefab == null)
                return "BAKE FAILED " + baked;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("BuildingRoot");
            var rotator = root.AddComponent<BuildingRotator>();
            rotator.PointerDrives = true;

            var house = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
            house.name = "House";
            house.transform.localPosition = Vector3.zero;
            house.transform.localRotation = Quaternion.identity;
            house.transform.localScale = Vector3.one;
            string meshReport = Lighten(house);

            Bounds bounds = WorldBounds(house);
            float span = Mathf.Max(0.5f, bounds.size.magnitude);
            float unit = Mathf.Max(0.08f, bounds.size.y * 0.02f);

            var plinth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plinth.name = "YellowPlinth";
            plinth.transform.SetParent(root.transform, false);
            Vector3 localMin = root.transform.InverseTransformPoint(bounds.min);
            Vector3 localMax = root.transform.InverseTransformPoint(bounds.max);
            Vector3 localCenter = (localMin + localMax) * 0.5f;
            float footprint = Mathf.Max(localMax.x - localMin.x, localMax.z - localMin.z);
            plinth.transform.localPosition = new Vector3(localCenter.x, localMin.y - unit * 3f, localCenter.z);
            plinth.transform.localScale = new Vector3(footprint * 1.08f, unit * 0.12f, footprint * 1.08f);
            Paint(plinth.GetComponent<Renderer>(), new Color(0.72f, 0.62f, 0.36f));
            Object.DestroyImmediate(plinth.GetComponent<Collider>());

            var nodes = new GameObject("Nodes");
            nodes.transform.SetParent(root.transform, false);
            float y0 = localMin.y + unit * 1.4f;
            float yMid = Mathf.Lerp(localMin.y, localMax.y, 0.38f);
            float yGap = Mathf.Lerp(localMin.y, localMax.y, 0.52f);
            float x0 = Mathf.Lerp(localMin.x, localMax.x, 0.22f);
            float x1 = Mathf.Lerp(localMin.x, localMax.x, 0.4f);
            float xGap = Mathf.Lerp(localMin.x, localMax.x, 0.58f);
            float xRoom = Mathf.Lerp(localMin.x, localMax.x, 0.78f);
            float z0 = Mathf.Lerp(localMin.z, localMax.z, 0.2f);
            float z1 = Mathf.Lerp(localMin.z, localMax.z, 0.42f);
            float zGap = Mathf.Lerp(localMin.z, localMax.z, 0.55f);

            Waypoint markerA = MakeNode(nodes.transform, "A", new Vector3(x0, y0, z0), unit, false);
            Waypoint markerA2 = MakeNode(nodes.transform, "A2", new Vector3(x1, y0, z1), unit * 0.7f, false);
            Waypoint markerStair = MakeNode(nodes.transform, "Stair", new Vector3(x1, yMid, zGap), unit * 0.7f, false);
            Waypoint markerB = MakeNode(nodes.transform, "B", new Vector3(xGap, yGap, zGap), unit * 1.35f, true);
            Waypoint markerC = MakeNode(nodes.transform, "C", new Vector3(xRoom, yGap, zGap), unit * 1.35f, false);
            Waypoint markerSpeaker = MakeNode(nodes.transform, "SpeakerPoint", new Vector3(xRoom, yGap, zGap + unit * 3f), unit, false);
            float yRoof = Mathf.Lerp(localMin.y, localMax.y, 0.9f);
            Waypoint markerDoor = MakeNode(nodes.transform, "Door", new Vector3(xRoom, yGap, z0), unit, false);
            Waypoint markerDog = MakeNode(nodes.transform, "Dog", new Vector3(x1, yMid, z0), unit, false);
            Waypoint markerSafe = MakeNode(nodes.transform, "Safe", new Vector3(x0, yMid, zGap), unit, false);
            Waypoint markerF = MakeNode(nodes.transform, "F", new Vector3(x0, Mathf.Lerp(yMid, yRoof, 0.25f), z1), unit * 0.8f, false);
            Waypoint markerI = MakeNode(nodes.transform, "I", new Vector3(x1, Mathf.Lerp(yMid, yRoof, 0.45f), zGap), unit * 0.8f, false);
            Waypoint markerL = MakeNode(nodes.transform, "L", new Vector3(xGap, Mathf.Lerp(yMid, yRoof, 0.65f), z1), unit * 0.8f, false);
            Waypoint markerG = MakeNode(nodes.transform, "G", new Vector3(xRoom, Mathf.Lerp(yMid, yRoof, 0.82f), zGap), unit * 0.8f, false);
            Waypoint markerM = MakeNode(nodes.transform, "M", new Vector3(localCenter.x, yRoof, localCenter.z), unit * 0.8f, false);
            Waypoint markerPole = MakeNode(nodes.transform, "Pole", new Vector3(localCenter.x, localMax.y, localCenter.z), unit * 1.2f, true);
            var poleAnchor = new GameObject("PoleAnchor");
            poleAnchor.transform.SetParent(root.transform, false);
            poleAnchor.transform.localPosition = markerPole.transform.localPosition + Vector3.up * unit;

            PathNode a = markerA.GetComponent<PathNode>();
            PathNode a2 = markerA2.GetComponent<PathNode>();
            PathNode stair = markerStair.GetComponent<PathNode>();
            PathNode b = markerB.GetComponent<PathNode>();
            PathNode c = markerC.GetComponent<PathNode>();
            PathNode speakerNode = markerSpeaker.GetComponent<PathNode>();
            a.Link(a2);
            a2.Link(stair);
            stair.Link(b);

            float playerHeight = Mathf.Clamp(bounds.size.y * 0.055f, unit * 2.2f, span * 0.12f);
            var playerGo = new GameObject("Player");
            playerGo.transform.SetParent(root.transform, false);
            GameObject body = BuildBody(playerGo.transform, playerHeight);
            var hand = new GameObject("Hand");
            hand.transform.SetParent(playerGo.transform, false);
            hand.transform.localPosition = new Vector3(playerHeight * 0.22f, playerHeight * 0.62f, playerHeight * 0.2f);

            var movement = playerGo.AddComponent<PlayerMovement>();
            movement.PlaceAt(a);
            movement.SetPace(2.4f / Mathf.Max(1f, bounds.size.x));
            var bob = body.AddComponent<WalkerVisual>();
            bob.movement = movement;
            var actor = playerGo.AddComponent<ClipActor>();
            actor.movement = movement;
            actor.walkClip = FirstClip(WalkModel);
            actor.fallClip = FirstClip(FallModel);

            var room = new GameObject("RoomFocus");
            room.transform.SetParent(root.transform, false);
            room.transform.localPosition = new Vector3(xRoom, yGap + playerHeight, zGap);

            RoomInteractable table = Prop(root.transform, "Table", new Vector3(xRoom - unit * 2f, yGap + unit, zGap + unit * 2f),
                new Vector3(unit * 2.2f, unit * 0.8f, unit * 1.4f), new Color(0.45f, 0.28f, 0.18f),
                InteractKind.Clue, "Before every meal, Grandma would turn it on.");
            RoomInteractable shelf = Prop(root.transform, "Shelf", new Vector3(xRoom + unit * 2f, yGap + unit * 2f, zGap - unit),
                new Vector3(unit * 1.6f, unit * 2.4f, unit * 0.6f), new Color(0.32f, 0.34f, 0.3f),
                InteractKind.Clue, "Back then, the loudspeaker’s voice would echo through the halls even before Grandma called us to dinner.");
            RoomInteractable cabinet = Prop(root.transform, "Cabinet", new Vector3(xRoom, yGap + unit * 1.6f, zGap - unit * 2.5f),
                new Vector3(unit * 2f, unit * 3f, unit * 1.1f), new Color(0.4f, 0.24f, 0.16f),
                InteractKind.Cabinet, null);

            var spillA = GameObject.CreatePrimitive(PrimitiveType.Cube);
            spillA.name = "Dish";
            spillA.transform.SetParent(cabinet.transform, false);
            spillA.transform.localPosition = new Vector3(-0.2f, 0.35f, 0.55f);
            spillA.transform.localScale = Vector3.one * 0.18f;
            Object.DestroyImmediate(spillA.GetComponent<Collider>());
            var spillB = GameObject.CreatePrimitive(PrimitiveType.Cube);
            spillB.name = "Cup";
            spillB.transform.SetParent(cabinet.transform, false);
            spillB.transform.localPosition = new Vector3(0.22f, 0.4f, 0.5f);
            spillB.transform.localScale = Vector3.one * 0.14f;
            Object.DestroyImmediate(spillB.GetComponent<Collider>());

            var speakerGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            speakerGo.name = "Loudspeaker";
            speakerGo.transform.SetParent(cabinet.transform, false);
            speakerGo.transform.localPosition = new Vector3(0f, 0.2f, 0.2f);
            speakerGo.transform.localScale = new Vector3(0.22f, 0.16f, 0.22f);
            Paint(speakerGo.GetComponent<Renderer>(), new Color(0.15f, 0.15f, 0.16f));
            var speaker = speakerGo.AddComponent<RoomInteractable>();
            speaker.kind = InteractKind.Speaker;
            speakerGo.SetActive(false);
            cabinet.spillPieces = new[] { spillA.transform, spillB.transform };
            cabinet.speaker = speaker;
            cabinet.speakerDropLocal = new Vector3(0f, -0.2f, 0.9f);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var camera = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.14f, 0.145f, 0.16f);
            camera.nearClipPlane = Mathf.Max(0.01f, span * 0.002f);
            camera.farClipPlane = span * 30f;
            var rig = camGo.AddComponent<IsoRig>();
            rig.Frame(bounds.center, span * 0.34f, span * 1.35f);

            var sunGo = new GameObject("Sun");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.15f;
            sun.color = new Color(1f, 0.95f, 0.86f);
            sun.shadows = LightShadows.Hard;
            sun.shadowStrength = 0.7f;
            sunGo.transform.rotation = Quaternion.Euler(52f, -36f, 0f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.45f, 0.43f, 0.4f);

            var ribbonGo = new GameObject("Route");
            var ribbon = ribbonGo.AddComponent<RouteRibbon>();
            ribbon.Build(unit * 0.28f);

            var systems = new GameObject("_Level1");
            var board = systems.AddComponent<RotationBoard>();
            board.Bind(root.transform, movement);
            board.SetLinks(new List<YawLink>
            {
                new YawLink { from = b, to = c, yaw = StoryProgress.CorridorYaw, tolerance = StoryProgress.Tolerance },
                new YawLink { from = markerDoor.GetComponent<PathNode>(), to = markerSafe.GetComponent<PathNode>(), yaw = StoryProgress.StairYaw, tolerance = StoryProgress.Tolerance }
            });
            var presenter = systems.AddComponent<Level1Presenter>();
            var director = systems.AddComponent<Level1Director>();
            director.performanceBase = meshReport;
            director.Bind(
                movement, board, rotator, presenter, rig, ribbon,
                a, b, c, speakerNode, markerB, markerC, markerSpeaker,
                cabinet, hand.transform, room.transform,
                new List<PathNode> { a, a2, stair, b });
            director.BindRoute(
                markerDoor.GetComponent<PathNode>(),
                markerDog.GetComponent<PathNode>(),
                markerSafe.GetComponent<PathNode>(),
                markerF.GetComponent<PathNode>(),
                markerI.GetComponent<PathNode>(),
                markerL.GetComponent<PathNode>(),
                markerG.GetComponent<PathNode>(),
                markerM.GetComponent<PathNode>(),
                markerPole.GetComponent<PathNode>(),
                poleAnchor.transform);
            var input = systems.AddComponent<Level1Input>();
            input.view = camera;
            input.director = director;
            input.rotator = rotator;
            systems.AddComponent<PlayBudget>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, LevelScene);
            AddBuildScene(LevelScene);
            string preview = ReplacePreview(prefab);
            EditorSceneManager.OpenScene(LevelScene);
            return baked + " | " + meshReport + " | " + preview + " | scene=" + LevelScene;
        }

        static GameObject LoadMap(out string report)
        {
            if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(MapGlb)))
            {
                report = "map=" + MapGlb;
                return AssetDatabase.LoadAssetAtPath<GameObject>(MapGlb);
            }

            report = HouseGameplayBaker.Ensure(GameplayPrefab);
            return AssetDatabase.LoadAssetAtPath<GameObject>(GameplayPrefab);
        }

        static GameObject BuildBody(Transform parent, float playerHeight)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(WalkModel);
            if (model != null)
            {
                var body = (GameObject)PrefabUtility.InstantiatePrefab(model, parent);
                body.name = "Body";
                body.transform.localPosition = Vector3.zero;
                Bounds bounds = WorldBounds(body);
                float height = Mathf.Max(0.01f, bounds.size.y);
                body.transform.localScale = Vector3.one * (playerHeight / height);
                foreach (Collider col in body.GetComponentsInChildren<Collider>())
                    Object.DestroyImmediate(col);
                return body;
            }

            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "Body";
            capsule.transform.SetParent(parent, false);
            capsule.transform.localScale = new Vector3(playerHeight * 0.38f, playerHeight * 0.5f, playerHeight * 0.38f);
            Object.DestroyImmediate(capsule.GetComponent<Collider>());
            Paint(capsule.GetComponent<Renderer>(), new Color(0.93f, 0.9f, 0.82f));
            return capsule;
        }

        static AnimationClip FirstClip(string path)
        {
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__"))
                    return clip;
            }

            return null;
        }

        static void PrepareImporter(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
                return;

            bool dirty = false;
            if (importer.importCameras) { importer.importCameras = false; dirty = true; }
            if (importer.importLights) { importer.importLights = false; dirty = true; }
            if (importer.importBlendShapes) { importer.importBlendShapes = false; dirty = true; }
            if (importer.importAnimation) { importer.importAnimation = false; dirty = true; }
            if (importer.animationType != ModelImporterAnimationType.None)
            {
                importer.animationType = ModelImporterAnimationType.None;
                dirty = true;
            }

            if (importer.isReadable) { importer.isReadable = false; dirty = true; }
            if (importer.meshCompression != ModelImporterMeshCompression.Low)
            {
                importer.meshCompression = ModelImporterMeshCompression.Low;
                dirty = true;
            }

            if (dirty)
                importer.SaveAndReimport();
        }

        public static string Lighten(GameObject house)
        {
            int renderers = 0;
            int tris = 0;
            int cams = 0;
            int lights = 0;
            foreach (Camera cam in house.GetComponentsInChildren<Camera>(true))
            {
                cam.enabled = false;
                cams++;
            }

            foreach (Light light in house.GetComponentsInChildren<Light>(true))
            {
                light.enabled = false;
                lights++;
            }

            foreach (Animator animator in house.GetComponentsInChildren<Animator>(true))
                animator.enabled = false;

            foreach (Collider collider in house.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(collider);

            RepairGlbMaterials(house);

            foreach (Renderer renderer in house.GetComponentsInChildren<Renderer>(true))
            {
                renderers++;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                if (filter != null && filter.sharedMesh != null)
                    tris += filter.sharedMesh.triangles.Length / 3;
            }

            return "renderers=" + renderers + " tris=" + tris + " cams=" + cams + " lights=" + lights;
        }

        static string ReplacePreview(GameObject prefab)
        {
            if (string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(PreviewScene)))
                return "preview-missing";

            Scene preview = EditorSceneManager.OpenScene(PreviewScene, OpenSceneMode.Single);
            Vector3 spot = Vector3.zero;
            bool haveSpot = false;
            foreach (GameObject root in preview.GetRootGameObjects())
            {
                if (root.GetComponent<Camera>() != null || root.GetComponent<Light>() != null)
                    continue;
                int count = root.GetComponentsInChildren<Renderer>(true).Length;
                if (count < 4)
                    continue;
                if (!haveSpot)
                {
                    spot = root.transform.position;
                    haveSpot = true;
                }

                Object.DestroyImmediate(root);
            }

            var house = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            house.name = "House_Gameplay";
            house.transform.position = spot;
            string report = Lighten(house);
            EditorSceneManager.MarkSceneDirty(preview);
            EditorSceneManager.SaveScene(preview);
            return "preview " + report;
        }

        static Waypoint MakeNode(Transform parent, string id, Vector3 local, float radius, bool visible)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = id;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            go.transform.localScale = new Vector3(radius * 2f, 0.03f * radius, radius * 2f);
            go.AddComponent<PathNode>();
            Paint(go.GetComponent<Renderer>(), visible
                ? new Color(0.93f, 0.72f, 0.28f)
                : new Color(0.75f, 0.22f, 0.18f, 0.35f));
            var way = go.AddComponent<Waypoint>();
            way.marker = go.GetComponent<Renderer>();
            way.click = go.GetComponent<Collider>();
            way.Show(visible);
            return way;
        }

        static RoomInteractable Prop(Transform parent, string name, Vector3 local, Vector3 scale, Color color, InteractKind kind, string line)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            go.transform.localScale = scale;
            Paint(go.GetComponent<Renderer>(), color);
            var prop = go.AddComponent<RoomInteractable>();
            prop.kind = kind;
            prop.line = line;
            return prop;
        }

        public static string RepairOpenHouse()
        {
            GameObject house = GameObject.Find("House");
            if (house == null)
                return "no house";
            int replaced = RepairGlbMaterials(house);
            EditorSceneManager.MarkSceneDirty(house.scene);
            EditorSceneManager.SaveScene(house.scene);
            return "replaced=" + replaced + " scene=" + house.scene.path;
        }

        static int RepairGlbMaterials(GameObject house)
        {
            Shader shader = Shader.Find("Standard");
            if (shader == null || house == null)
                return 0;
            var cache = new Dictionary<Material, Material>();
            int replaced = 0;
            foreach (Renderer renderer in house.GetComponentsInChildren<Renderer>(true))
            {
                Material[] shared = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < shared.Length; i++)
                {
                    Material source = shared[i];
                    if (source == null || source.shader == null || source.shader.name != "Hidden/InternalErrorShader")
                        continue;
                    if (!cache.TryGetValue(source, out Material built))
                    {
                        built = ToStandard(source, shader);
                        cache[source] = built;
                    }

                    shared[i] = built;
                    changed = true;
                    replaced++;
                }

                if (changed)
                    renderer.sharedMaterials = shared;
            }

            return replaced;
        }

        static Material ToStandard(Material source, Shader shader)
        {
            var built = new Material(shader) { name = source.name };
            Texture albedo = SavedTex(source, "baseColorTexture");
            Color color = SavedColor(source, "baseColorFactor", Color.white);
            float metal = SavedFloat(source, "metallicFactor", 0f);
            float rough = SavedFloat(source, "roughnessFactor", 0.5f);
            float mode = SavedFloat(source, "_Mode", 0f);
            float cutoff = SavedFloat(source, "alphaCutoff", 0.5f);
            if (albedo != null)
                built.SetTexture("_MainTex", albedo);
            built.SetColor("_Color", color);
            built.SetFloat("_Metallic", Mathf.Clamp01(metal));
            built.SetFloat("_Glossiness", 1f - Mathf.Clamp01(rough));
            if (mode >= 1.5f)
            {
                built.SetFloat("_Mode", 2f);
                built.SetOverrideTag("RenderType", "Transparent");
                built.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                built.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                built.SetInt("_ZWrite", 0);
                built.EnableKeyword("_ALPHABLEND_ON");
                built.renderQueue = 3000;
            }
            else if (mode >= 0.5f)
            {
                built.SetFloat("_Mode", 1f);
                built.SetFloat("_Cutoff", cutoff);
                built.SetOverrideTag("RenderType", "TransparentCutout");
                built.EnableKeyword("_ALPHATEST_ON");
                built.renderQueue = 2450;
            }

            return built;
        }

        static Texture SavedTex(Material source, string key)
        {
            SerializedObject so = new SerializedObject(source);
            SerializedProperty slots = so.FindProperty("m_SavedProperties.m_TexEnvs");
            if (slots == null)
                return null;
            for (int i = 0; i < slots.arraySize; i++)
            {
                SerializedProperty entry = slots.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("first").stringValue != key)
                    continue;
                SerializedProperty tex = entry.FindPropertyRelative("second.m_Texture");
                return tex != null ? tex.objectReferenceValue as Texture : null;
            }

            return null;
        }

        static Color SavedColor(Material source, string key, Color fallback)
        {
            SerializedObject so = new SerializedObject(source);
            SerializedProperty slots = so.FindProperty("m_SavedProperties.m_Colors");
            if (slots == null)
                return fallback;
            for (int i = 0; i < slots.arraySize; i++)
            {
                SerializedProperty entry = slots.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("first").stringValue != key)
                    continue;
                SerializedProperty value = entry.FindPropertyRelative("second");
                if (value != null && value.propertyType == SerializedPropertyType.Color)
                    return value.colorValue;
            }

            return fallback;
        }

        static float SavedFloat(Material source, string key, float fallback)
        {
            SerializedObject so = new SerializedObject(source);
            SerializedProperty slots = so.FindProperty("m_SavedProperties.m_Floats");
            if (slots == null)
                return fallback;
            for (int i = 0; i < slots.arraySize; i++)
            {
                SerializedProperty entry = slots.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("first").stringValue != key)
                    continue;
                SerializedProperty value = entry.FindPropertyRelative("second");
                if (value != null && value.propertyType == SerializedPropertyType.Float)
                    return value.floatValue;
            }

            return fallback;
        }

        static void Paint(Renderer renderer, Color color)
        {
            if (renderer == null)
                return;
            Shader shader = Shader.Find("Standard") ?? Shader.Find("Diffuse");
            if (shader == null)
                return;
            var material = new Material(shader);
            if (material.HasProperty("_Color"))
                material.color = color;
            renderer.sharedMaterial = material;
        }

        static Bounds WorldBounds(GameObject house)
        {
            Renderer[] renderers = house.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return new Bounds(house.transform.position, Vector3.one);
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        static void AddBuildScene(string path)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(scene => scene.path == path);
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif
