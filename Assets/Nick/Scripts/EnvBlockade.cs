using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnvBlockade : MonoBehaviour
{
    [Header("Blockade Groups")]
    [SerializeField] private List<GameObject> BlockadesStage1 = new List<GameObject>();
    [SerializeField] private List<GameObject> BlockadesStage2 = new List<GameObject>();
    [SerializeField] private List<GameObject> BlockadesStage3 = new List<GameObject>();
    [SerializeField] private List<GameObject> BlockadesStage4 = new List<GameObject>();
    [SerializeField] private List<GameObject> BlockadesStage5 = new List<GameObject>();

    void Start()
    {
        GameManager.OnStageChange += UpdateEnvironmentBlockades;
    }
    private void OnDestroy()
    {
        GameManager.OnStageChange -= UpdateEnvironmentBlockades;
    }

    private void UpdateEnvironmentBlockades(int stage)
    {
        switch (stage)
        {
            case 1:
                ActivateBlockadeGroup(BlockadesStage1);
                break;

            case 2:
                ActivateBlockadeGroup(BlockadesStage2);
                break;

            case 3:
                ActivateBlockadeGroup(BlockadesStage3);
                break;

            case 4:
                ActivateBlockadeGroup(BlockadesStage4);
                break;

            case 5:
                ActivateBlockadeGroup(BlockadesStage5);
                break;
        }
    }
    private void ActivateBlockadeGroup(List<GameObject> group)
    {
        StartCoroutine(ActivateBlockadeGroupTime(group));
    }

    private IEnumerator ActivateBlockadeGroupTime(List<GameObject> group)
    {
        foreach (GameObject obj in group)
        {
            yield return new WaitForSeconds(Random.Range(5f, 10f));
            obj.SetActive(true);
        }
    }
}
