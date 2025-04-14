using UnityEngine;
using UnityEngine.VFX;

public class DestroyVFX : MonoBehaviour
{
	private VisualEffect visualEffect;
	private bool hasPlayed;

	void Start()
	{
		visualEffect = GetComponent<VisualEffect>();
	}

	void Update()
	{	
		// Remove when "sleeping"
        if (visualEffect.aliveParticleCount == 0 && hasPlayed)
        {

            // Reset the hasPlayed flag to prepare for the next time the effect is played.
            hasPlayed = false;

			Destroy(gameObject);
            return;
        }
       
        // If there are alive particles, mark the effect as having been played.
        if (visualEffect.aliveParticleCount > 0)
        {
            hasPlayed = true;
        }
	}
}