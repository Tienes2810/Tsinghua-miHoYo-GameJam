using UnityEngine;

namespace PerspectivePuzzle.Visual
{
    public class ClickRipple : MonoBehaviour
    {
        public static void Spawn(Vector3 worldPos, Color color, float maxRadius = 0.55f, float duration = 0.45f)
        {
            var go = new GameObject("ClickRipple");
            go.transform.position = worldPos + Vector3.up * 0.04f;
            var ripple = go.AddComponent<ClickRipple>();
            ripple.StartRipple(color, maxRadius, duration);
        }

        LineRenderer line;
        float elapsed;
        float totalTime;
        float targetRadius;
        Color baseColor;

        public void StartRipple(Color color, float radius, float duration)
        {
            baseColor = color;
            targetRadius = radius;
            totalTime = duration;

            line = gameObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = 36;
            line.startWidth = 0.035f;
            line.endWidth = 0.035f;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;

            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = color;
            line.material = mat;

            UpdateCircle(0.05f, 1f);
        }

        void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / totalTime);
            float currentRadius = Mathf.Lerp(0.05f, targetRadius, Mathf.Sqrt(t));
            float alpha = 1f - t;

            UpdateCircle(currentRadius, alpha);

            if (t >= 1f)
            {
                Destroy(gameObject);
            }
        }

        void UpdateCircle(float radius, float alpha)
        {
            if (line == null)
                return;

            Color c = baseColor;
            c.a *= alpha;
            line.startColor = c;
            line.endColor = c;

            int count = line.positionCount;
            float step = 2f * Mathf.PI / count;
            for (int i = 0; i < count; i++)
            {
                float angle = i * step;
                Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                line.SetPosition(i, pos);
            }
        }
    }
}
