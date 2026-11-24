using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Marks a mutant as one the player's heart monitor reacts to.
/// Leave it off the owl so it stays silent.
/// </summary>
[RequireComponent(typeof(EnemySearchlight))]
public class HeartbeatSource : MonoBehaviour
{
    // Shared list so the heart monitor never searches the scene for mutants.
    public static readonly List<HeartbeatSource> Active = new List<HeartbeatSource>();
    [SerializeField] private LayerMask wallMask;

    private EnemySearchlight searchlight;
    public float Distance => searchlight.distanceToPlayer;

    private void Awake()
    {
        searchlight = GetComponent<EnemySearchlight>();
    }

    private void OnEnable()
    {
        Active.Add(this);
    }

    private void OnDisable()
    {
        Active.Remove(this);
    }

    public bool IsBlockedByWall()
    {
        Vector3 playerHead = GameManager.PlayerCamera.transform.position;
        return Physics.Linecast(transform.position, playerHead, wallMask, QueryTriggerInteraction.Ignore);
    }
}
