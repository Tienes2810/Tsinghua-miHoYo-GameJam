using System;
using UnityEngine;

namespace PerspectivePuzzle.Interaction
{
    public class QuestItem : MonoBehaviour
    {
        public bool Collected { get; private set; }
        public event Action CollectedEvent;

        public void Collect()
        {
            if (Collected)
                return;

            Collected = true;
            gameObject.SetActive(false);
            CollectedEvent?.Invoke();
        }
    }
}
