using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace PerspectivePuzzle.Level1
{
    public enum InteractKind
    {
        Clue,
        Cabinet,
        Speaker
    }

    public class RoomInteractable : MonoBehaviour
    {
        public InteractKind kind;
        public string line;
        public Transform[] spillPieces;
        public RoomInteractable speaker;
        public Vector3 speakerDropLocal;

        public bool Used { get; set; }
        public bool SpeakerReady { get; private set; }

        public void SetSpeakerReady(bool ready)
        {
            SpeakerReady = ready;
            if (speaker != null)
                speaker.gameObject.SetActive(ready);
        }

        public IEnumerator PlayCabinet()
        {
            if (Used)
                yield break;

            Used = true;
            if (spillPieces != null)
            {
                for (int i = 0; i < spillPieces.Length; i++)
                {
                    Transform piece = spillPieces[i];
                    if (piece == null)
                        continue;
                    Vector3 target = piece.position + Vector3.down * 0.35f + piece.right * (0.08f * (i + 1));
                    piece.DOMove(target, 0.45f).SetEase(Ease.InQuad).SetLink(piece.gameObject);
                    piece.DORotate(new Vector3(18f, 40f * i, 12f), 0.45f, RotateMode.LocalAxisAdd).SetLink(piece.gameObject);
                }
            }

            yield return new WaitForSeconds(0.5f);
            if (speaker != null)
            {
                speaker.gameObject.SetActive(true);
                speaker.transform.localPosition = speakerDropLocal;
                speaker.SetSpeakerReady(true);
                SpeakerReady = true;
            }
        }
    }
}
