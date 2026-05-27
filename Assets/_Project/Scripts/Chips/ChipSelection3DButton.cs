using UnityEngine;

public class ChipSelection3DButton : Selectable3DButton
{
    [SerializeField] private ChipDenomination denomination;

    public ChipDenomination Denomination => denomination;
}
