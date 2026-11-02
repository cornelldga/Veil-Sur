using UnityEngine;
using System;

/// <summary>
/// Subclass of EnemySearchlight to handle the dog that reacts only to tennis balls.
/// </summary>
public class IdleDogStates : EnemySearchlight
{
    [SerializeField] protected string[] acceptedSenseSources;

    protected override void Start()
    {
        base.Start();
        CurrentState = AlertState.Idle;
    }

    /// <summary>
    /// Same as the original UpdateAlertState, but returns to Idle instead of Patrol
    /// </summary>
    protected override void UpdateAlertState(bool canSeePlayer)
    {
        // Debug.Log("Last seen timer: " +lastSeenTimer);
        if (canSeePlayer || (CurrentState == AlertState.Alert && IsProximityDetected()))
        {
            lastSeenTimer = 0f;
            LastKnownPlayerPosition = player.position;
            float proximity = Mathf.Clamp01(1f - distanceToPlayer / viewDistance);
            float multiplier = 1f + proximity * (maxDetectionMultiplier - 1f);

            detectionMeter += Time.deltaTime * multiplier;
        }
        else
        {
            lastSeenTimer += Time.deltaTime;
            detectionMeter -= suspicionDecayRate * Time.deltaTime;
        }

        detectionMeter = Mathf.Clamp(detectionMeter, 0f, timeToAlert);

        // Update stun timer
        timeSinceLastStun += Time.deltaTime;

        switch (CurrentState)
        {
            case AlertState.Idle :
            case AlertState.Patrol :
                if (detectionMeter >= timeToSuspicious) SetState(AlertState.Suspicious);
                break;

            case AlertState.Suspicious:
                if (detectionMeter >= timeToAlert) SetState(AlertState.Alert);
                else if (GetComponent<EnemyMover>().ReachedDestination() && detectionMeter <= 0f) {
                    SetState(AlertState.Idle); // Return to idle immediately
                    // GetComponent<EnemyMover>().StartLookAround();
                    ResetSuspicion();
                }
                break;

            case AlertState.Alert:
                // Arriving at the last known spot ends the chase too, so the mutant never idles out the timer there.
                bool lostPlayer = lastSeenTimer > 0f;
                if (lostPlayer && (lastSeenTimer >= loseAlertAfter || GetComponent<EnemyMover>().ReachedDestination()))
                {
                    // GetComponent<EnemyMover>()
                    //     .RegisterDetectionEvent(LastKnownPlayerPosition);
                    SetState(AlertState.Suspicious);
                    ReportSense(LastKnownPlayerPosition, Priority.Sight, "Player");
                }
                break;
            case AlertState.LookAround:
                if (canSeePlayer && detectionMeter >= timeToAlert)
                {
                    SetState(AlertState.Alert);
                }
                else if (timesLookedAround >= investigateCount)
                {
                    SetState(AlertState.Idle);
                }
                break;
            case AlertState.Stun:
                if (timeSinceLastStun >= stunDuration)
                {
                    if (LastKnownPlayerPosition != null)
                    {
                        SetState(AlertState.Suspicious);
                        ReportSense(LastKnownPlayerPosition, Priority.Sight, "Player");
                    }
                    else
                    {
                        SetState(AlertState.Idle);
                    }
                    
                }
                break;
        }
    }

    /// <summary>
    /// Works largely the same as the superclass method, but ONLY accepted  
    /// </summary>
    public override void ReportSense(Vector3 position, Priority priority, string source)
    {
        Debug.Log($"State: {CurrentState}");
        Debug.Log($"Source: {source}");
        Debug.Log($"Accepted: {Array.IndexOf(acceptedSenseSources, source) != -1}");
        Debug.Log($"Priority: {priority}, Current Priority: {currentPriority}");
        if (CurrentState == AlertState.Alert || 
        CurrentState == AlertState.Stun || priority > currentPriority) return;

        // Only react to accepted stimuli while idle
        if (CurrentState == AlertState.Idle && 
        GetComponent<EnemyMover>().ReachedDestination() && 
        Array.IndexOf(acceptedSenseSources, source) == -1) return;

        LastKnownPlayerPosition = position;
        detectionMeter = Mathf.Max(detectionMeter, timeToSuspicious);
        SetState(AlertState.Suspicious);
        currentPriority = priority;
        // Debug.Log("Now investigating " + position + " at priority " + priority);
    }
}
