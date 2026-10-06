using PerspectivePuzzle.Puzzle;
using UnityEngine;

namespace PerspectivePuzzle.Core
{
    public class GreyboxHud : MonoBehaviour
    {
        [SerializeField] GameFlowManager flow;
        [SerializeField] BuildingRotator rotator;
        [SerializeField] PerspectiveConnector connector;

        void OnGUI()
        {
            const int width = 640;
            GUILayout.BeginArea(new Rect(16f, 16f, width, 150f), GUI.skin.box);
            GUILayout.Label("Khung thử — hình khối sẽ được thay bằng model.");
            GUILayout.Label(PhaseText());
            if (rotator != null && connector != null)
            {
                string state = connector.IsAligned ? "ĐÃ NỐI" : "chưa nối";
                GUILayout.Label(
                    $"Góc nhà {rotator.CurrentYaw:0.0}°  /  mục tiêu {connector.AlignAngle:0.0}° (±{connector.Tolerance:0.#}°)  {state}");
            }
            GUILayout.Label("Chuột phải hoặc Q/E: xoay nhà. Chuột trái: đi tới node, lấy đồ, lật cầu thang.");
            GUILayout.EndArea();
        }

        string PhaseText()
        {
            if (flow == null)
                return "Chưa gắn GameFlowManager.";

            switch (flow.Phase)
            {
                case GamePhase.Round1FindItem:
                    return "Vòng 1 — Xoay nhà cho đến khi đường đứt nối lại, đi vào phòng và bấm kỷ vật.";
                case GamePhase.Round2ReachBalcony:
                    return "Vòng 2 — Cầu thang bị chặn. Bấm cầu thang để lật, rồi đi ra ban công.";
                default:
                    return "Xong — nhân vật đã ra ban công.";
            }
        }
    }
}
