using TMPro;
using UnityEngine;

public class ChipStackView : MonoBehaviour
{
    [SerializeField] private TMP_Text stakeLabel;

    public void SetStake(int stake)
    {
        if (stakeLabel != null)
            stakeLabel.text = stake.ToString();
    }

    public void DisableColliders()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);

        foreach (Collider collider in colliders)
            collider.enabled = false;
    }
}