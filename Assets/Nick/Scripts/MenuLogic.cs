using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class MenuLogic : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button startButton2;
    [SerializeField] private Button rewatchButton;

    [SerializeField] private string[] dialogueLines;
    [SerializeField] private TMP_Text dialogueText;

    [SerializeField] private float charDelay = 0.04f;
    private Coroutine typingCoroutine;
    private int lineIndex;
    private int lastAdvanceFrame;
    private bool dialogueActive;
    private bool isTyping;

    private void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            NextLine();
        }
    }

    public void StartButton()
    {
        if (dialogueActive) return;

        startButton.gameObject.SetActive(false);
        rewatchButton.gameObject.SetActive(false);
        startButton2.gameObject.SetActive(false);
        dialogueText.gameObject.SetActive(true);
        dialogueActive = true;
        lineIndex = 0;
        lastAdvanceFrame = Time.frameCount;
        ShowLine();
    }

    public void LoadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }


    public void NextLine()
    {
        if (!dialogueActive || lastAdvanceFrame == Time.frameCount) return;
        lastAdvanceFrame = Time.frameCount;

        if (isTyping)
        {
            StopCoroutine(typingCoroutine);
            isTyping = false;
            dialogueText.text = dialogueLines[lineIndex];
            return;
        }

        lineIndex++;
        ShowLine();
    }

    private void ShowLine()
    {
        if (dialogueLines == null || lineIndex >= dialogueLines.Length)
        {
            dialogueText.text = "";
            dialogueActive = false;
            rewatchButton.gameObject.SetActive(true);
            startButton2.gameObject.SetActive(true);
            return;
        }

        typingCoroutine = StartCoroutine(TypeLine(dialogueLines[lineIndex]));
    }

    private IEnumerator TypeLine(string line)
    {
        isTyping = true;
        dialogueText.text = "";
        foreach (char character in line)
        {
            dialogueText.text += character;
            yield return new WaitForSeconds(charDelay);
        }
        isTyping = false;
    }
}
