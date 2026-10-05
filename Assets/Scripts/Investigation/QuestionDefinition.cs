using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class QuestionDefinition : MonoBehaviour
{
    [SerializeField] List<PhotographableObject> required_objects;
    [SerializeField] List<PhotographableObject> current_objects;

    public bool isSolved()
    {
        foreach (PhotographableObject obj in required_objects)
        {
            if(!current_objects.Any((current_object) => obj.SubjectId.Equals(current_object.SubjectId)))
            {
                return false;
            }
        }
        return true;
    }

    //TODO: WHOEVER IS IMPLEMENTING QUESTION DEFINITION AND HOW IT INTERACTS WITH UI
    //This is just a draft. Feel free to do it whatever way works better 
    public void AttachObject(PhotographableObject photoObject)
    {
        current_objects.Add(photoObject);
    }

    
    public void RemoveObject(PhotographableObject photoObject) { 
        current_objects.Remove(photoObject);
    }

}
