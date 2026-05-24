using System.Collections.Generic;
using UnityEngine;

public class ChipVisualPool : MonoBehaviour
{
    [SerializeField] private ChipVisual chipPrefab;
    [SerializeField] private Transform inactiveRoot;
    [SerializeField] private int prewarmCount = 20;

    private readonly Stack<ChipVisual> pool = new Stack<ChipVisual>();

    private void Awake()
    {
        if (chipPrefab == null)
            throw new System.InvalidOperationException($"{nameof(ChipVisualPool)} needs a ChipVisual prefab.");

        Prewarm();
    }

    public ChipVisual Get(Transform parent)
    {
        ChipVisual chip = pool.Count > 0
            ? pool.Pop()
            : Instantiate(chipPrefab, GetInactiveRoot());

        Transform chipTransform = chip.transform;
        chipTransform.SetParent(parent, false);

        chip.ResetVisual();
        chip.gameObject.SetActive(true);

        return chip;
    }

    public void Release(ChipVisual chip)
    {
        if (chip == null)
            return;

        chip.gameObject.SetActive(false);
        chip.transform.SetParent(GetInactiveRoot(), false);
        chip.ResetVisual();

        pool.Push(chip);
    }

    private void Prewarm()
    {
        for (int i = 0; i < prewarmCount; i++)
        {
            ChipVisual chip = Instantiate(chipPrefab, GetInactiveRoot());
            chip.gameObject.SetActive(false);
            pool.Push(chip);
        }
    }

    private Transform GetInactiveRoot()
    {
        return inactiveRoot != null ? inactiveRoot : transform;
    }
}