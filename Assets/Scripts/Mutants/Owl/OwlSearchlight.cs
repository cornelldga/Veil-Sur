using System.Drawing;
using Unity.VisualScripting;

public class OwlSearchlight : EnemySearchlight
{
    protected override void UpdateAlertState(bool canSeePlayer)
    {
        if (CurrentState != AlertState.Alert)
        {
            SetState(AlertState.Alert);
        }
    }

    protected override UnityEngine.Color GetDetectionColor()
    {
        return alertColor;
    }
}