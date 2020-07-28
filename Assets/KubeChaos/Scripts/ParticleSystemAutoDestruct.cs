using UnityEngine;
using System.Collections;

/// <summary>
/// Simple auto destruct component for effect GameObjects like sparks / blood that use particle systems that helps them to be used with the ObjectPoolManager.
/// </summary>
public class ParticleSystemAutoDestruct : MonoBehaviour
{

    private ParticleSystem theParticleSystem;

    void OnEnable()
    {
        theParticleSystem = gameObject.GetComponent<ParticleSystem>();
    }

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () 
    {
	    if (theParticleSystem != null)
	    {
	        if (!theParticleSystem.IsAlive())
	        {
                // Send the particle back to the object pool by setting the gameobject to disabled.
	            gameObject.SetActive(false);
	        }
	    }
	}
}
