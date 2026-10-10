using PerspectivePuzzle.Movement;
using UnityEngine;

namespace PerspectivePuzzle.Level1
{
    public class WalkerVisual : MonoBehaviour
    {
        public PlayerMovement movement;
        Vector3 rest;

        void Awake()
        {
            rest = transform.localPosition;
        }

        void LateUpdate()
        {
            if (movement != null && movement.IsMoving)
            {
                float bob = Mathf.Abs(Mathf.Sin(Time.time * 9f)) * 0.08f;
                transform.localPosition = rest + Vector3.up * bob;
            }
            else
            {
                transform.localPosition = rest;
            }
        }
    }
}
