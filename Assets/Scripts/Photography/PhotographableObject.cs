using UnityEngine;

public class PhotographableObject : MonoBehaviour
{
    [SerializeField] private string subjectId;
    public string SubjectId => subjectId;
}
