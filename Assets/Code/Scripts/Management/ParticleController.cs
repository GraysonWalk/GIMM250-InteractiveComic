using UnityEngine;

public class ParticleController : MonoBehaviour
{
    public ParticleSystem particleSystemToPlay;

    // Called from Animation Event
    public void PlayParticles()
    {
        if (particleSystemToPlay != null)
        {
            particleSystemToPlay.Play();
        }
    }

    public void StopParticles()
    {
        if (particleSystemToPlay != null)
        {
            particleSystemToPlay.Stop();
        }
    }
}
