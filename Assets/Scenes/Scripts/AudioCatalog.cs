using UnityEngine;

[CreateAssetMenu(fileName = "AudioCatalog", menuName = "Circuito/Audio Catalog")]
public class AudioCatalog : ScriptableObject
{
    public AudioClip startClip;
    public AudioClip startFondoClip;
    public AudioClip paintingAmbientClip;
    public AudioClip paintingLoopClip;
    public AudioClip paintingSecondaryClip;
    public AudioClip finishClip;
    public AudioClip resultsClip;
    public AudioClip victoryClip;
    public AudioClip defeatClip;
    public AudioClip uiClickClip;
}
