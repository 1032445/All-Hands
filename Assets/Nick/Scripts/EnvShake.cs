using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class EnvShake : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] GameManager gameManager;

    [Header("Shake Settings")]
    [SerializeField] private float shakeFrequencyStage2;
    [SerializeField] private float shakeFrequencyStage3;
    [SerializeField] private float shakeFrequencyStage4;
    [SerializeField] private float shakeFrequencyStage5;

    [SerializeField] private float shakeFrequencyMax;
    [SerializeField] private float shakeFrequencyMin;

    [SerializeField] private float shakeIntensity;
    private bool shakeDirection;

    [Header("Camera Shake")]
    [SerializeField] private RectTransform cameraTransform;
    [SerializeField] private float cameraShakeDuration;
    [SerializeField] private float cameraShakeAmount;
    private float currentCameraShakeAmount;
    [SerializeField] private AnimationCurve curve;

    private bool isShaking = false;


    [Header("Shake Audio")]
    [SerializeField] private AudioClip[] shipImpact;
    [SerializeField] private AudioClip[] shipCreek;

    void Start()
    {
        GameManager.OnStageChange += UpdateEnvironmentShake;
    }

    private void UpdateEnvironmentShake(int stage)
    {
        switch (stage)
        {
            case 1:
                break;

            case 2:
                shakeFrequencyMax = shakeFrequencyStage2;
                shakeFrequencyMin = shakeFrequencyStage2 * (2f / 3f);

                StartCoroutine(ShakeTimer());

                Debug.Log("Started Shaking");

                break;

            case 3:
                shakeFrequencyMax = shakeFrequencyStage3;
                shakeFrequencyMin = shakeFrequencyStage3 * (2f / 3f);
                break;

            case 4:

                shakeFrequencyMax = shakeFrequencyStage4;
                shakeFrequencyMin = shakeFrequencyStage4 * (2f / 3f);

                break;

            case 5:

                shakeFrequencyMax = shakeFrequencyStage5;
                shakeFrequencyMin = shakeFrequencyStage5 * (2f / 3f);

                break;
        }
    }

    private void FixedUpdate()
    {
        CameraShakeFunctionality();
    }

    IEnumerator ShakeTimer()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(shakeFrequencyMin, shakeFrequencyMax));
            ShakeTrigger();
        }
    }

    private void ShakeTrigger()
    {
        ShakeDirection();

        float direction = shakeDirection ? 1f : -1f;

        Vector3 force = new Vector3(shakeIntensity * direction, 2f, 0f);

        Rigidbody[] rigidbodies = FindObjectsByType<Rigidbody>();

        foreach (Rigidbody rb in rigidbodies)
        {
            if (rb.isKinematic)
                continue;

            rb.AddForce(force, ForceMode.Impulse);
        }

        StartCoroutine(CameraShake());
    }

    private void ShakeDirection()
    {
       shakeDirection = Random.value > 0.5f;
    }

    private void OnDestroy()
    {
        GameManager.OnStageChange -= UpdateEnvironmentShake;
    }

    private void CameraShakeFunctionality()
    {
        if (isShaking)
        {
            currentCameraShakeAmount = Mathf.Lerp(currentCameraShakeAmount, 0f, Time.deltaTime * 2f);

            float randomX = Random.value - 0.5f;
            float randomY = Random.value - 0.5f;
            float randomZ = Random.value - 0.5f;

            cameraTransform.localPosition = new Vector3(randomX, randomY, randomZ) * currentCameraShakeAmount;
        }
    }

    IEnumerator CameraShake()
    {
        currentCameraShakeAmount = cameraShakeAmount;
        isShaking = true;

        AudioClip randomShipImpact = shipImpact[Random.Range(0, shipImpact.Length)];
        SFXManager.Instance.PlaySoundFXClip(randomShipImpact, transform, 0.5f);
        AudioClip randomShipCreek = shipCreek[Random.Range(0, shipCreek.Length)];
        SFXManager.Instance.PlaySoundFXClip(randomShipCreek, transform, 0.1f);

        yield return new WaitForSeconds(cameraShakeDuration);

        isShaking = false;
        currentCameraShakeAmount = 0f;
        cameraTransform.localPosition = Vector3.zero;
    }
}
