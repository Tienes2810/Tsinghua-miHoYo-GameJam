using UnityEngine;

namespace PerspectivePuzzle.Level1
{
    /// <summary>
    /// Play-mode budget for machines weaker than the authoring GPU.
    /// Restored when the object is destroyed so editor quality is not left changed.
    /// </summary>
    public class PlayBudget : MonoBehaviour
    {
        float shadowDistance;
        int pixelLights;
        int antiAliasing;
        ShadowQuality shadows;
        bool captured;

        void Awake()
        {
            shadowDistance = QualitySettings.shadowDistance;
            pixelLights = QualitySettings.pixelLightCount;
            antiAliasing = QualitySettings.antiAliasing;
            shadows = QualitySettings.shadows;
            captured = true;

            QualitySettings.shadowDistance = Mathf.Min(shadowDistance, 28f);
            QualitySettings.pixelLightCount = 1;
            QualitySettings.antiAliasing = 0;
            QualitySettings.shadows = ShadowQuality.HardOnly;
            Application.targetFrameRate = 60;
        }

        void OnDestroy()
        {
            if (!captured)
                return;
            QualitySettings.shadowDistance = shadowDistance;
            QualitySettings.pixelLightCount = pixelLights;
            QualitySettings.antiAliasing = antiAliasing;
            QualitySettings.shadows = shadows;
        }
    }
}
