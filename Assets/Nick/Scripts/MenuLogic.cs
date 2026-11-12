using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class MenuLogic : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button startButton2;
    [SerializeField] private Button rewatchButton;

    [SerializeField] private string[] dialogueLines;
    [SerializeField] private TMP_Text dialogueText;

    [SerializeField] private float charDelay = 0.04f;
    [SerializeField] private float linePause = 2f;

    public void StartButton()
    {
        startButton.gameObject.SetActive(false);

        StartCoroutine(PlayDialogue());
    }

    public void LoadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }


    private IEnumerator PlayDialogue()
    {
        rewatchButton.gameObject.SetActive(false);
        startButton2.gameObject.SetActive(false);
        dialogueText.gameObject.SetActive(true);

        dialogueText.text = "";

        foreach (string line in dialogueLines)
        {
            dialogueText.text = "";

            foreach (char character in line)
            {
                dialogueText.text += character;
                yield return new WaitForSeconds(charDelay);
            }

            yield return new WaitForSeconds(linePause);
        }

        dialogueText.text = "";

        rewatchButton.gameObject.SetActive(true);
        startButton2.gameObject.SetActive(true);
    }
}
