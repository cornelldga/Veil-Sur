using UnityEngine;

/// <summary>
/// Tennis ball launcher from Level One. Locked until the questions linked to it are solved.
/// </summary>
public class TennisLauncher : PuzzleInteractable
{
    [SerializeField] float launchDistance = 6f;
    [SerializeField] float launchSpeed = 8f;
    [SerializeField] float ballVolume = 100f;
    [SerializeField] GameObject tennisBallPrefab;

    public override void SuccessBehavior()
    {
        Trigger(launchDistance);
    }

    public override void FailBehavior()
    {
        ErrorNotifier.Show("Seems complicated. Maybe I can find instructions somewhere?");
    }

    /// <summary>
    /// Launch a tennis ball distance [dist] forward, and make a sound at that position.
    /// </summary>
    void Trigger(float dist)
    {
        Vector3 destination = this.transform.position + this.transform.forward * dist;

        GameObject ball = Instantiate(tennisBallPrefab, transform.position, transform.rotation);
        TennisBall ballScript = ball.GetComponent<TennisBall>();
        ballScript.SetDistance(dist);
        ballScript.SetDirection(this.transform.forward);
        ballScript.SetSpeed(launchSpeed);
        ballScript.SetVolume(ballVolume);
    }


}
