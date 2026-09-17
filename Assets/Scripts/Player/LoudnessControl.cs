using Unity.VisualScripting;
using UnityEngine;

public class LoudnessControl : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private PlayerStateController playerStateController;
    [SerializeField] SphereCollider normalLoudnessDetector;
    [SerializeField] SphereCollider enhancedLoudnessDetector;
 
    [SerializeField] int crouchRadius = 2;
    [SerializeField] int walkRadius = 5;
    [SerializeField] int runRadius = 10;
    [SerializeField] int enhancedMultiplier = 2;

    void Start()
    {
        playerStateController = GetComponent<PlayerStateController>();
    }

    // Update is called once per frame
    void Update()
    {
        playerStateController.GetCrouching();
    }
}
