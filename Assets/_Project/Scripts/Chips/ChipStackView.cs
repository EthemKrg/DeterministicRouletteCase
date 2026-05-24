using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ChipStackView : MonoBehaviour
{
    [Serializable]
    private class ChipMaterialEntry
    {
        public ChipDenomination denomination;
        public Material material;
    }

    [SerializeField] private TMP_Text stakeLabel;
    [SerializeField] private Transform chipRoot;
    [SerializeField] private GameObject chipVisualPrefab;
    [SerializeField] private Renderer fallbackChipRenderer;
    [SerializeField] private Vector3 chipStackOffset = new Vector3(0f, 0.025f, 0f);
    [SerializeField] private ChipMaterialEntry[] chipMaterials;

    private readonly List<GameObject> chipVisuals = new List<GameObject>();

    public void AddChip(ChipDenomination denomination)
    {
        if (chipVisualPrefab == null)
            return;

        Transform root = chipRoot != null ? chipRoot : transform;

        GameObject chip = Instantiate(chipVisualPrefab, root);
        chip.transform.localPosition = chipStackOffset * chipVisuals.Count;
        chip.transform.localRotation = Quaternion.identity;

        ApplyMaterial(chip, denomination);
        DisableColliders(chip);

        chipVisuals.Add(chip);
    }

    public void SetStake(int stake)
    {
        if (stakeLabel != null)
            stakeLabel.text = stake.ToString();
    }

    public void Clear()
    {
        foreach (GameObject chip in chipVisuals)
        {
            if (chip != null)
                Destroy(chip);
        }

        chipVisuals.Clear();

        if (stakeLabel != null)
            stakeLabel.text = string.Empty;
    }

    public void DisableColliders()
    {
        DisableColliders(gameObject);
    }

    private void ApplyMaterial(GameObject chip, ChipDenomination denomination)
    {
        Material material = GetMaterial(denomination);

        if (material == null)
            return;

        Renderer renderer = chip.GetComponentInChildren<Renderer>();

        if (renderer == null)
            renderer = fallbackChipRenderer;

        if (renderer != null)
            renderer.sharedMaterial = material;
    }

    private Material GetMaterial(ChipDenomination denomination)
    {
        foreach (ChipMaterialEntry entry in chipMaterials)
        {
            if (entry != null && entry.denomination == denomination)
                return entry.material;
        }

        return null;
    }

    private static void DisableColliders(GameObject target)
    {
        Collider[] colliders = target.GetComponentsInChildren<Collider>(true);

        foreach (Collider collider in colliders)
            collider.enabled = false;
    }
}