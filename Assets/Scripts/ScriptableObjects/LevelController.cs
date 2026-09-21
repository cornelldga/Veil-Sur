using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelController", menuName = "Scriptable Objects/LevelController")]
public class LevelController : ScriptableObject
{
    /* Contains:
        Level ID

        Scene name

        Mutant configuration

        Questions + solutions

        Other level-specific settings
    */
    [Header("Base Data")]
    [Tooltip("Room Name")]
    [SerializeField] private string levelName;
}
