using CrowdMatch;
using DG.Tweening;
using UnityEngine;

public class UIRainBowPanel : MonoBehaviour
{
    public ParticleSystem[] rainPartics;
    public ParticleSystem blastRainbow;

    private void Awake()
    {
        rainBowStop();
    }

    void rainBowStop()
    {
        foreach (var item in rainPartics)
        {
            item.Stop();
        }
        blastRainbow.Stop();
    }


    public void rainBowPlay()
    {
        AudioManager.Instance.Play("WinPart");
        for (int i = 0; i < rainPartics.Length; i++)
        {
            var rainVFX = rainPartics[i];
            var time = i / 2 * 0.2f;
            DOVirtual.DelayedCall(time, () =>
            {
                rainVFX.Play();
            });
        }

        DOVirtual.DelayedCall(1, () =>
        {
            blastRainbow.Play();
        });
    }
}
