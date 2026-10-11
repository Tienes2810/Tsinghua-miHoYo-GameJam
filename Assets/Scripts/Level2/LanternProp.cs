using System.Collections;
using PerspectivePuzzle.Audio;
using UnityEngine;

namespace PerspectivePuzzle.Level2
{
    public class LanternProp : MonoBehaviour
    {
        [SerializeField] int lanternIndex = 1;
        [SerializeField] Light pointLight;
        [SerializeField] Renderer flameRend;
        [SerializeField] Color unlitColor = new Color(0.35f, 0.2f, 0.15f, 1f);
        [SerializeField] Color litColor = new Color(1f, 0.65f, 0.2f, 1f);

        public bool IsLit { get; private set; }
        public int LanternIndex => lanternIndex;

        public static LanternProp Create(Transform parent, string name, Vector3 localPos, int index)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;

            // Frame/Housing
            var frame = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            frame.name = "Frame";
            frame.transform.SetParent(go.transform, false);
            frame.transform.localScale = new Vector3(0.5f, 0.6f, 0.5f);
            Destroy(frame.GetComponent<Collider>());
            var frameRend = frame.GetComponent<Renderer>();
            var frameMat = new Material(Shader.Find("Standard"));
            frameMat.color = new Color(0.25f, 0.12f, 0.08f);
            frameRend.material = frameMat;

            // Flame / Core
            var core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.name = "FlameCore";
            core.transform.SetParent(go.transform, false);
            core.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            core.transform.localScale = Vector3.one * 0.35f;
            Destroy(core.GetComponent<Collider>());
            var coreRend = core.GetComponent<Renderer>();
            var coreMat = new Material(Shader.Find("Standard"));
            coreMat.color = new Color(0.4f, 0.22f, 0.15f);
            coreRend.material = coreMat;

            // Light
            var lightGo = new GameObject("LanternLight");
            lightGo.transform.SetParent(go.transform, false);
            lightGo.transform.localPosition = Vector3.up * 0.1f;
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.72f, 0.35f);
            light.intensity = 0f;
            light.range = 5.5f;

            var prop = go.AddComponent<LanternProp>();
            prop.lanternIndex = index;
            prop.pointLight = light;
            prop.flameRend = coreRend;
            prop.SetLit(false);

            return prop;
        }

        public void SetLit(bool lit)
        {
            IsLit = lit;
            if (flameRend != null && flameRend.material != null)
            {
                flameRend.material.color = lit ? litColor : unlitColor;
                if (lit)
                {
                    flameRend.material.EnableKeyword("_EMISSION");
                    flameRend.material.SetColor("_EmissionColor", litColor * 1.5f);
                }
            }
            if (pointLight != null)
                pointLight.intensity = lit ? 2.5f : 0f;
        }

        public void LightUp()
        {
            if (IsLit)
                return;
            StartCoroutine(LightUpRoutine());
        }

        IEnumerator LightUpRoutine()
        {
            IsLit = true;
            if (GameAudio.Instance != null)
                GameAudio.Instance.PlaySuccess();

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 0.8f;
                float ease = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
                if (pointLight != null)
                    pointLight.intensity = Mathf.Lerp(0f, 3.2f, ease);
                if (flameRend != null && flameRend.material != null)
                {
                    Color c = Color.Lerp(unlitColor, litColor, ease);
                    flameRend.material.color = c;
                    flameRend.material.EnableKeyword("_EMISSION");
                    flameRend.material.SetColor("_EmissionColor", c * (ease * 2.2f));
                }
                yield return null;
            }

            if (pointLight != null)
                pointLight.intensity = 2.4f;
        }

        void Update()
        {
            if (IsLit && pointLight != null)
            {
                // Subtle flame flicker
                float flicker = Mathf.PerlinNoise(Time.time * 6f, lanternIndex * 10f) * 0.4f;
                pointLight.intensity = 2.2f + flicker;
            }
        }
    }
}
