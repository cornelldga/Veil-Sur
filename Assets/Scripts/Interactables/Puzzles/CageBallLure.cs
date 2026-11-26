using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A tennis ball inside the cage will send dog mutants to waiting point.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class CageBallLure : MonoBehaviour
{
    [SerializeField] private Transform[] insideTargets;
    [SerializeField] private GameObject doorwayBlocker;
    [SerializeField] private float staySeconds = 3f;

    private TennisBall lastBall;
    private bool luring;

    private void OnTriggerEnter(Collider other)
    {
        TennisBall ball = other.GetComponentInParent<TennisBall>();
        if (ball == null || ball == lastBall || doorwayBlocker.activeSelf || luring || insideTargets.Length == 0) return;
        lastBall = ball;
        luring = true;
        StartCoroutine(LureDogs());
    }

    private IEnumerator LureDogs()
    {
        var visitors = new List<EnemyMover>();
        foreach (EnemyMover dog in FindObjectsByType<EnemyMover>())
        {
            if (!dog.isActiveAndEnabled) continue;
            if (!dog.TryGetComponent<IdleDogStates>(out _) && !dog.TryGetComponent<DogSniff>(out _)) continue;
            if (dog.VisitCage(insideTargets[visitors.Count % insideTargets.Length].position))
                visitors.Add(dog);
        }

        // Start the timer after all dogs show up
        while (visitors.Exists(dog => dog != null && dog.IsVisitingCage && !dog.IsWaitingInCage))
            yield return null;

        yield return new WaitForSeconds(staySeconds);
        foreach (EnemyMover dog in visitors)
            if (dog != null) dog.EndCageVisit();

        luring = false;
    }
}
