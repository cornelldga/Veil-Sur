using UnityEngine;

/// <summary>
/// Tennis ball launcher from Level One. Locked until the questions linked to it are solved.
/// </summary>
public class TennisLauncher : PuzzleInteractable
{
    [SerializeField] float launchDistance = 6f;
    [SerializeField] float launchHeight = 2f;
    [SerializeField] float ballVolume = 100f;
    [SerializeField] GameObject tennisBallPrefab;
    [Tooltip("Spawn height")]
    [SerializeField] Vector3 spawnOffset = Vector3.zero;
    [SerializeField] AudioClip launchSound;

    public override void SuccessBehavior()
    {
        Trigger(launchDistance);
        AudioManager.Instance.PlaySfxAtPosition(launchSound, transform.position);
    }

    public override void FailBehavior()
    {
        Trigger(launchDistance); //TODO remove
        ErrorNotifier.Show("Seems complicated. Maybe I can find instructions somewhere?");
    }

    /// <summary>
    /// Launch a tennis ball distance [dist] forward, and make a sound at that position.
    /// </summary>
    void Trigger(float dist)
    {
        Vector3 spawnPosition = transform.TransformPoint(spawnOffset);
        GameObject ball = Instantiate(tennisBallPrefab, spawnPosition, transform.rotation);
        TennisBall ballScript = ball.GetComponent<TennisBall>();
        ballScript.SetVolume(ballVolume);
        ballScript.IgnoreCollisions(GetComponentsInChildren<Collider>());
        ballScript.Launch(this.transform.forward, dist, launchHeight);
    }


}
