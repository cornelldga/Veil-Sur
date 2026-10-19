using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Level : MonoBehaviour
{
    // No code, but this is the general idea of the class
    public GameObject player;
    List<GameObject> mutants;
    InvestigationManager investigationManager;

    //current instance of the level
    public static Level current_level;

    private void Awake()
    {
        current_level = this;

        SaveOnFirstTime();
    }

    private IEnumerator SaveOnFirstTime()
    {
        yield return new WaitUntil(() => SaveManager.Instance != null);

        if (!SaveManager.Instance.HasCurrentLevelSave())
        {
            SaveManager.Instance.SaveGame();
        }
    }
}
