using System;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public static event Action<int> OnStageChange;

    [Header("Debug Stuff")]
    [SerializeField] private int currentStage = 0;
    [SerializeField] private bool updateStage = false;

    void Start()
    {
        instance = this;
    }

    void Update()
    {
        if (updateStage)
        {
            updateStage = false;
            UpdateEnviromentStage(currentStage);
        }
    }

    public void UpdateEnviromentStage(int stage)
    {
        currentStage = stage;
        OnStageChange?.Invoke(currentStage);
    }
}
