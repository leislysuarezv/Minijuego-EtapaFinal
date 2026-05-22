using UnityEngine;

public class Finish : MonoBehaviour
{
    public ScoreManager scoreManager;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (scoreManager == null || ScoreManager.CurrentPhase != ScoreManager.GamePhase.Painting)
        {
            return;
        }

        if (other.CompareTag("Player"))
        {
            scoreManager.RegisterPlayerFinished();
        }
    }
}
