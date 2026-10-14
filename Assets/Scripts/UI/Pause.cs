using UnityEngine;

public class Pause : UIScreen
{
    public void Resume()
    {
        UIManager.Instance.HideAll();
    }

    public void OpenSettings()
    {
        UIManager.Instance.Show(UIManager.UIGroupId.Settings);
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
