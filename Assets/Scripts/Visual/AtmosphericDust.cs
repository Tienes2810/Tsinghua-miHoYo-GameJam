using UnityEngine;

namespace PerspectivePuzzle.Visual
{
    public class AtmosphericDust : MonoBehaviour
    {
        public static AtmosphericDust Create(Transform parent, Color tint)
        {
            var go = new GameObject("AtmosphericDust");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 6f, 0f);
            var dust = go.AddComponent<AtmosphericDust>();
            dust.Setup(tint);
            return dust;
        }

        void Setup(Color tint)
        {
            var ps = gameObject.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = 9f;
            main.startSpeed = 0.25f;
            main.startSize = 0.08f;
            main.maxParticles = 120;
            main.startColor = tint;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 12f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(20f, 14f, 20f);

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.x = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f);
            vel.y = new ParticleSystem.MinMaxCurve(-0.08f, 0.12f);
            vel.z = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f);

            var col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(tint, 0f), new GradientColorKey(tint, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.6f, 0.3f), new GradientAlphaKey(0.6f, 0.7f), new GradientAlphaKey(0f, 1f) }
            );
            col.color = grad;

            var rend = GetComponent<ParticleSystemRenderer>();
            rend.material = new Material(Shader.Find("Sprites/Default"));
        }
    }
}
