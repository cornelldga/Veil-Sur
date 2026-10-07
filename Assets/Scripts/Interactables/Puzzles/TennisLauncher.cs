using UnityEngine;

public class TennisLauncher : SoundEvent, Interactable
{
    [SerializeField] float launchDistance = 6f;
    [SerializeField] float launchSpeed = 8f;
    [SerializeField] float launchVolume = 3f;
    [SerializeField] float ballVolume = 12f;
    [SerializeField] GameObject tennisBallPrefab;

    public void Interact()
    {
        Trigger(launchDistance);
    }

    /// <summary>
    /// Launch a tennis ball distance [dist] forward, and make a sound at that position.
    /// </summary>
    void Trigger(float dist)
    {
        Vector3 destination = this.transform.position + this.transform.forward * dist;
        RegisterSoundEvent(destination, launchVolume);

        if (tennisBallPrefab == null) Debug.LogError("Tennis ball prefab not assigned.");

        GameObject ball = Instantiate(tennisBallPrefab, transform.position, transform.rotation);
        TennisBall ballScript = ball.GetComponent<TennisBall>();
        ballScript.SetDistance(dist);
        ballScript.SetDirection(this.transform.forward);
        ballScript.SetSpeed(launchSpeed);
        ballScript.SetVolume(ballVolume);
    }


}
