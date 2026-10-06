using PerspectivePuzzle.Interaction;
using PerspectivePuzzle.Movement;
using PerspectivePuzzle.Puzzle;
using UnityEngine;

namespace PerspectivePuzzle.Core
{
    public enum GamePhase
    {
        Round1FindItem,
        Round2ReachBalcony,
        Complete
    }

    public class GameFlowManager : MonoBehaviour
    {
        [SerializeField] PlayerMovement player;
        [SerializeField] StairFlipper stair;
        [SerializeField] QuestItem item;
        [SerializeField] PathNode itemNode;
        [SerializeField] PathNode balconyNode;
        [SerializeField] GamePhase phase = GamePhase.Round1FindItem;

        public GamePhase Phase => phase;

        void OnEnable()
        {
            if (player != null)
                player.Arrived += OnArrived;
        }

        void Start()
        {
            if (stair != null)
                stair.SetInteractionEnabled(phase == GamePhase.Round2ReachBalcony);
        }

        void OnDisable()
        {
            if (player != null)
                player.Arrived -= OnArrived;
        }

        public void TryCollect(QuestItem clicked)
        {
            if (phase != GamePhase.Round1FindItem || clicked != item || item == null || player == null)
                return;
            if (player.IsMoving || player.CurrentNode != itemNode)
                return;

            item.Collect();
            phase = GamePhase.Round2ReachBalcony;
            if (stair != null)
            {
                stair.SetBlocked(true);
                stair.SetInteractionEnabled(true);
            }
        }

        void OnArrived(PathNode node)
        {
            if (phase == GamePhase.Round2ReachBalcony && node == balconyNode)
                phase = GamePhase.Complete;
        }
    }
}
