using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Coffee.UIEffects;
using System;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour
{
    public Button[] gridSpaces;
    public Button resetButton;
    public TextMeshProUGUI turnIndicatorText;
    public TextMeshProUGUI timerText;
    public AudioSource clickSFX;
    public AudioSource popSFX;
    public AudioSource resetSFX;
    public AudioSource winSFX;
    public AudioSource drawSFX;
    public AudioSource soundtrack;
    public ParticleSystem confettiParticleSystem;
    private bool isGameActive;
    private string currentPlayer;
    private string[] boardState;
    private float gameTime;
    private Color xColor;
    private Color oColor;


    void Start()
    {
        LeanTween.init(9900);
        currentPlayer = "X";
        boardState = new string[9];
        SetTurnIndicatorColor("#616161");
        turnIndicatorText.text = "Player " + currentPlayer + "'s Turn";

        soundtrack.Play();

        SetRandomColors();

        foreach (Button button in gridSpaces)
        {
            button.transform.localScale = Vector3.zero;
            AddHoverEffects(button);
        }

        for (int i = 0; i < gridSpaces.Length; i++)
        {
            int index = i;
            gridSpaces[index].onClick.AddListener(() => OnGridSpaceClicked(index));
        }

        AnimateButtonsIn();
        resetButton.onClick.AddListener(ResetGame);

        gameTime = 0f;
        isGameActive = true;
        timerText.text = "00:000";
    }

    void Update()
    {
        if (isGameActive)
        {
            gameTime += Time.deltaTime;
            int seconds = Mathf.FloorToInt(gameTime);
            int milliseconds = Mathf.FloorToInt((gameTime * 1000) % 1000);
            timerText.text = $"{seconds:00}:{milliseconds:000}";

            if(seconds >= 20)
                ResetGame();
        }
    }

    void AddHoverEffects(Button button)
    {
        EventTrigger trigger = button.gameObject.AddComponent<EventTrigger>();
        EventTrigger.Entry entryEnter = new EventTrigger.Entry();
        EventTrigger.Entry entryExit = new EventTrigger.Entry();

        entryEnter.eventID = EventTriggerType.PointerEnter;
        entryEnter.callback.AddListener((eventData) => OnButtonHoverEnter(button));

        entryExit.eventID = EventTriggerType.PointerExit;
        entryExit.callback.AddListener((eventData) => OnButtonHoverExit(button));

        trigger.triggers.Add(entryEnter);
        trigger.triggers.Add(entryExit);
    }

    void OnButtonHoverEnter(Button button)
    {
        LeanTween.scale(button.gameObject, new Vector3(1.1f, 1.1f, 1f), 0.2f).setEaseOutQuad();
        button.GetComponent<UIShiny>().Play();
    }

    void OnButtonHoverExit(Button button)
    {
        LeanTween.scale(button.gameObject, new Vector3(1f, 1f, 1f), 0.2f).setEaseOutQuad();
    }

    void OnGridSpaceClicked(int index)
    {
        if (string.IsNullOrEmpty(boardState[index]))
        {
            clickSFX.Play();

            float randomOffsetX = Random.Range(-10f, 10f);
            float randomOffsetY = Random.Range(-10f, 10f);
            float randomRotation = Random.Range(-15f, 15f);
            float randomDownTime = Random.Range(0.1f, 0.2f);
            float randomUpTime = Random.Range(0.2f, 0.3f);

            LeanTween.scale(gridSpaces[index].gameObject, new Vector3(0.8f, 0.8f, 1), 0.1f)
                .setEaseInOutQuad()
                .setOnComplete(() =>
                LeanTween.scale(gridSpaces[index].gameObject, new Vector3(1f, 1f, 1), 0.1f)
                .setEaseInOutQuad());

            LeanTween.moveLocal(gridSpaces[index].gameObject,
                                gridSpaces[index].transform.localPosition + new Vector3(randomOffsetX, randomOffsetY, 0),
                                randomDownTime)
                     .setEaseInOutQuad();

            LeanTween.rotateZ(gridSpaces[index].gameObject, randomRotation, randomDownTime)
                     .setEaseInOutQuad();

            LeanTween.moveLocal(gridSpaces[index].gameObject,
                                gridSpaces[index].transform.localPosition,
                                randomUpTime)
                     .setDelay(randomDownTime)
                     .setEaseOutBounce();

            LeanTween.rotateZ(gridSpaces[index].gameObject, 0f, randomUpTime)
                     .setDelay(randomDownTime)
                     .setEaseOutBounce();

            boardState[index] = currentPlayer;
            var textComponent = gridSpaces[index].GetComponentInChildren<TextMeshProUGUI>();
            textComponent.text = currentPlayer;

            if (currentPlayer == "X")
            {
                textComponent.color = xColor;
            }
            else
            {
                textComponent.color = oColor;
            }

            gridSpaces[index].interactable = false;

            if (CheckForWin(out int[] winIndices))
            {
                SetTurnIndicatorColor("#ffffff");
                turnIndicatorText.text = "Player " + currentPlayer + " Wins!";
                FlashButtons(winIndices);
                DisableButtons();
                winSFX.Play();
                confettiParticleSystem.Play();
                isGameActive = false;
                LeanTween.scale(timerText.gameObject, new Vector3(1.2f, 1.2f, 1f), 0.5f).setEaseOutBounce();
                Invoke("ResetGame", 2f);
            }
            else if (CheckForDraw())
            {
                SetTurnIndicatorColor("#ffffff");
                turnIndicatorText.text = "It's a Draw!";
                FlashButtons(null);
                drawSFX.Play();
                isGameActive = false;
                LeanTween.scale(timerText.gameObject, new Vector3(1.2f, 1.2f, 1f), 0.5f).setEaseOutBounce();
                Invoke("ResetGame", 2f);
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
        SetTurnIndicatorColor("#616161");
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

    void SetRandomColors()
    {
        float randomHueX = Random.Range(0f, 1f);
        float randomHueO = Random.Range(0f, 1f);

        xColor = Color.HSVToRGB(randomHueX, 0.99f, 0.83f);
        oColor = Color.HSVToRGB(randomHueO, 0.99f, 0.83f);

        xColor.a = 1f;
        oColor.a = 1f;
    }

    public void ResetGame()
    {
        currentPlayer = "X";
        boardState = new string[9];
        SetTurnIndicatorColor("#616161");
        turnIndicatorText.text = "Player " + currentPlayer + "'s Turn";

        SetRandomColors();

        gameTime = 0f;
        timerText.text = "00:000";
        LeanTween.scale(timerText.gameObject, Vector3.one, 0f);

        AnimateButtonsOut(() =>
        {
            foreach (Button button in gridSpaces)
            {
                button.GetComponentInChildren<TextMeshProUGUI>().text = "";
                button.interactable = true;
            }
            AnimateButtonsIn();
            isGameActive = true;
        });
    }

    void AnimateButtonsIn()
    {
        for (int i = 0; i < gridSpaces.Length; i++)
        {
            int index = i;
            LeanTween.scale(gridSpaces[index].gameObject, Vector3.one, 0.28901f)
                     .setDelay(index * 0.11f)
                     .setEaseOutBounce()
                     .setOnComplete(() =>
                     {
                         WobbleButton(gridSpaces[index]);
                         popSFX.Play();
                     });
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
        if (canvasGroup == null)
        {
            canvasGroup = button.AddComponent<CanvasGroup>();
        }

        LeanTween.alphaCanvas(canvasGroup, 0f, 0.25f).setEaseInOutQuad().setLoopPingPong(3);
    }

    void WobbleButton(Button button)
    {
        float randomDelay = Random.Range(0f, 0.34f);
        float randomOffsetX = Random.Range(-6f, -3.5f);
        float randomOffsetY = Random.Range(5.9f, 3.5f);
        float randTime = Random.Range(0.4798f, 0.998f);
        float randTimeR = Random.Range(0.80f, 1.23456f);
        Vector3 originalPosition = button.transform.localPosition;

        LeanTween.moveLocal(button.gameObject, originalPosition + new Vector3(Random.Range(randomOffsetX, randomOffsetY), Random.Range(randomOffsetX, randomOffsetY), 0), randTime)
                 .setDelay(randomDelay)
                 .setEaseInOutSine()
                 .setLoopPingPong();

        LeanTween.rotateZ(button.gameObject, Random.Range(randomOffsetX, randomOffsetY), randTimeR)
                 .setDelay(randomDelay)
                 .setEaseInOutSine()
                 .setLoopPingPong();
    }

    void SetTurnIndicatorColor(string hexColor)
    {
        Color color;
        if (UnityEngine.ColorUtility.TryParseHtmlString(hexColor, out color))
        {
            turnIndicatorText.color = color;
        }
    }
}
