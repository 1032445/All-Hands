using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnvLighting : MonoBehaviour
{
    [Header("Light Groups")]
    [SerializeField] private List<Light> lightGroup1 = new List<Light>();
    [SerializeField] private List<Light> lightGroup2 = new List<Light>();
    [SerializeField] private List<Light> lightGroup3 = new List<Light>();

    private List<List<Light>> lightGroups = new List<List<Light>>();

    [Header("Light Audio")]
    [SerializeField] private AudioClip lightOff;
    [SerializeField] private AudioClip lightOn;
    [SerializeField] private AudioClip alarm;

    [Header("Blackout Settings")]
    [SerializeField] private float blackoutFrequencyStage3;
    [SerializeField] private float blackoutFrequencyStage4;
    [SerializeField] private float blackoutFrequencyStage5;
    [SerializeField] private float disableDuration = 3f;

    private float minInterval = 5f;
    private float maxInterval = 15f;


    void Start()
    {
        lightGroups.Add(lightGroup1);
        lightGroups.Add(lightGroup2);
        lightGroups.Add(lightGroup3);

        GameManager.OnStageChange += UpdateEnvironmentLights;
    }

    private void OnDestroy()
    {
        GameManager.OnStageChange -= UpdateEnvironmentLights;
    }

    private void UpdateEnvironmentLights(int stage)
    {
        switch (stage)
        {
            case 1:
            case 2:
                break;

            case 3:
                minInterval = blackoutFrequencyStage3 * (2f / 3f);
                maxInterval = blackoutFrequencyStage3;

                StartCoroutine(LightingRoutine());

                Debug.Log("Starting Blackouts");
                break;

            case 4:
                minInterval = blackoutFrequencyStage4 * (2f / 3f);
                maxInterval = blackoutFrequencyStage4;
                break;

            case 5:
                emergencyLights();

                minInterval = blackoutFrequencyStage5 * (2f / 3f);
                maxInterval = blackoutFrequencyStage5;
                break;
        }
    }


    private IEnumerator LightingRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));

            int randomIndex = Random.Range(0, lightGroups.Count);
            List<Light> selectedGroup = lightGroups[randomIndex];

            foreach (Light light in selectedGroup)
            {
                light.enabled = false;
                SFXManager.Instance.PlaySoundFXClip(lightOff, transform, 0.2f);
            }

            yield return new WaitForSeconds(disableDuration);

            foreach (Light light in selectedGroup)
            {
                light.enabled = true;
                SFXManager.Instance.PlaySoundFXClip(lightOn, transform, 0.2f);
            }
        }
    }

    private void emergencyLights()
    {
        foreach (Light light in lightGroups[0])
            light.color = Color.red;

        foreach (Light light in lightGroups[1])
            light.color = Color.red;

        foreach (Light light in lightGroups[2])
            light.color = Color.red;

        SFXManager.Instance.PlayLoopingSoundFXClip(alarm, transform, 0.5f);
    }
}
