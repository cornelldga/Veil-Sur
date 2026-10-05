using Unity.IO.LowLevel.Unsafe;
using UnityEngine;

/// <summary>
/// Implemented by sources of detectable sound. Contains a method that notifies mutants of that sound in
/// a sphere.
/// </summary>
public class SoundEvent : MonoBehaviour
{   
    /// <summary>
    /// Called whenever something makes a sound that a mutant can hear. Notifies mutants in
    /// an area and causes them to begin investigating. 
    /// </summary>
    /// <param name="pos">Where the sound originated from.</param>
    /// <param name="radius">The base radius of the sphere in which mutants detect the sound.</param>
    protected void RegisterSoundEvent(Vector3 pos, float radius)
    {
        Collider[] collidersInside = Physics.OverlapSphere(transform.position, radius);

        foreach (Collider col in collidersInside)
        {
            if (col.gameObject.CompareTag("Mutant"))
            {
                // EnemyMover mutant = col.GetComponent<EnemyMover>();
                //mutant.RegisterDetectionEvent(pos);
                EnemySearchlight mutant = col.GetComponent<EnemySearchlight>();
                mutant.ReportSense(pos, Priority.Sound);
            }
        }
    }
}