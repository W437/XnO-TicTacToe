using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public Button[] gridSpaces;
    public Button resetButton;
    public TextMeshProUGUI turnIndicatorText;
    public AudioSource clickSFX;
    public AudioSource popSFX;
    public AudioSource soundtrack;
    private string currentPlayer;
    private string[] boardState;

    void Start()
    {
        currentPlayer = "X";
        boardState = new string[9];
        turnIndicatorText.text = "Player " + currentPlayer + "'s Turn";

        soundtrack.Play();

        foreach (Button button in gridSpaces)
        {
            button.transform.localScale = Vector3.zero;
        }

        for (int i = 0; i < gridSpaces.Length; i++)
        {
            int index = i;
            gridSpaces[index].onClick.AddListener(() => OnGridSpaceClicked(index));
        }

        AnimateButtonsIn();
        resetButton.onClick.AddListener(ResetGame);
    }

    public void OnGridSpaceClicked(int index)
    {
        if (string.IsNullOrEmpty(boardState[index]))
        {
            clickSFX.Play();

            LeanTween.scale(gridSpaces[index].gameObject, new Vector3(0.9f, 0.9f, 1), 0.1f)
                     .setEaseInOutQuad()
                     .setOnComplete(() =>
                        LeanTween.scale(gridSpaces[index].gameObject, new Vector3(1f, 1f, 1), 0.1f)
                        .setEaseInOutQuad());

            boardState[index] = currentPlayer;
            gridSpaces[index].GetComponentInChildren<TextMeshProUGUI>().text = currentPlayer;
            gridSpaces[index].interactable = false;

            if (CheckForWin(out int[] winIndices))
            {
                turnIndicatorText.text = "Player " + currentPlayer + " Wins!";
                FlashButtons(winIndices);
                DisableButtons();
            }
            else if (CheckForDraw())
            {
                turnIndicatorText.text = "It's a Draw!";
                FlashButtons(null);
            }
            else
            {
                SwitchPlayer();
            }
        }
    }

    void SwitchPlayer()
    {
        currentPlayer = (currentPlayer == "X") ? "O" : "X";
        turnIndicatorText.text = "Player " + currentPlayer + "'s Turn";
    }

    bool CheckForWin(out int[] winIndices)
    {
        int[,] winConditions = new int[,]
        {
            { 0, 1, 2 },
            { 3, 4, 5 },
            { 6, 7, 8 },
            { 0, 3, 6 },
            { 1, 4, 7 },
            { 2, 5, 8 },
            { 0, 4, 8 },
            { 2, 4, 6 }
        };

        for (int i = 0; i < winConditions.GetLength(0); i++)
        {
            if (boardState[winConditions[i, 0]] == currentPlayer &&
                boardState[winConditions[i, 1]] == currentPlayer &&
                boardState[winConditions[i, 2]] == currentPlayer)
            {
                winIndices = new int[] { winConditions[i, 0], winConditions[i, 1], winConditions[i, 2] };
                return true;
            }
        }
        winIndices = null;
        return false;
    }

    bool CheckForDraw()
    {
        foreach (string state in boardState)
        {
            if (string.IsNullOrEmpty(state))
            {
                return false;
            }
        }
        return true;
    }

    void DisableButtons()
    {
        foreach (Button button in gridSpaces)
        {
            button.interactable = false;
        }
    }

    public void ResetGame()
    {
        currentPlayer = "X";
        boardState = new string[9];
        turnIndicatorText.text = "Player " + currentPlayer + "'s Turn";

        AnimateButtonsOut(() =>
        {
            foreach (Button button in gridSpaces)
            {
                button.GetComponentInChildren<TextMeshProUGUI>().text = "";
                button.interactable = true;
            }
            AnimateButtonsIn();
        });
    }

    void AnimateButtonsIn()
    {
        for (int i = 0; i < gridSpaces.Length; i++)
        {
            int index = i;
            LeanTween.scale(gridSpaces[index].gameObject, Vector3.one, 0.2f)
                     .setDelay(index * 0.1f)
                     .setEaseOutBounce()
                     .setOnStart(() => popSFX.Play());
        }
    }

    void AnimateButtonsOut(System.Action onComplete)
    {
        for (int i = gridSpaces.Length - 1; i >= 0; i--)
        {
            int index = i;
            LeanTween.scale(gridSpaces[index].gameObject, Vector3.zero, 0.2f)
                     .setDelay((gridSpaces.Length - 1 - index) * 0.1f)
                     .setEaseInBack()
                     .setOnStart(() => popSFX.Play())
                     .setOnComplete(index == 0 ? onComplete : null);
        }
    }

    void FlashButtons(int[] indicesToFlash)
    {
        if (indicesToFlash == null)
        {
            foreach (Button button in gridSpaces)
            {
                FlashButton(button.gameObject);
            }
        }
        else
        {
            foreach (int index in indicesToFlash)
            {
                FlashButton(gridSpaces[index].gameObject);
            }
        }
    }

    void FlashButton(GameObject button)
    {
        CanvasGroup canvasGroup = button.GetComponent<CanvasGroup>();
        LeanTween.alphaCanvas(canvasGroup, 0f, 0.25f).setEaseInOutQuad().setLoopPingPong(3);
    }
}
