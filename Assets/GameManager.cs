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
    public Button uiModeButton;
    public TextMeshProUGUI turnIndicatorText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI logoText;
    public AudioSource clickSFX;
    public AudioSource popSFX;
    public AudioSource resetSFX;
    public AudioSource winSFX;
    public AudioSource drawSFX;
    public AudioSource soundtrack;
    public ParticleSystem confettiParticleSystem;
    public Image waelLogo;
    public Image bgImage;
    public Image gameLogo;
    public Camera mainCamera;
    private bool isGameActive;
    private string currentPlayer;
    private string[] boardState;
    private float gameTime;
    private Color xColor;
    private Color oColor;
    private const string xChar = "1";
    private const string oChar = "0";

    // Dark/Light mode
    private Color darkModeCameraColor;
    private Color darkModeButtonColor;
    private Color darkModeTurnTextColor;
    private Color darkModeTimerTextColor;
    private Color darkModeBGColor;
    private Color darkModeLogoColor;

    private Color lightModeCameraColor = new Color(1f, 1f, 1f); // FFFFFF
    private Color lightModeButtonColor = new Color(0.773f, 0.773f, 0.773f); // C5C5C5
    private Color lightModeTurnTextColor = new Color(0.773f, 0.773f, 0.773f); // C5C5C5
    private Color lightModeTimerTextColor = new Color(0.773f, 0.773f, 0.773f); // C5C5C5
    private Color lightModeBGColor = new Color(0.957f, 0.957f, 0.957f); // F4F4F4
    private Color lightModeLogoColor = new Color(0.918f, 0.918f, 0.918f); // EAEAEA
    private bool isDarkMode = true;


    void Start()
    {
        LeanTween.init(9900);
        currentPlayer = xChar;
        boardState = new string[9];
        SetTurnIndicatorColor("#616161");
        turnIndicatorText.text = "Player " + currentPlayer + "'s Turn";

        soundtrack.Play();

        SetRandomColors();

        darkModeCameraColor = mainCamera.backgroundColor;
        darkModeButtonColor = gridSpaces[0].GetComponent<Image>().color;
        darkModeTurnTextColor = turnIndicatorText.color;
        darkModeTimerTextColor = timerText.color;
        darkModeBGColor = bgImage.color;
        darkModeLogoColor = waelLogo.color;

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
        uiModeButton.onClick.AddListener(ToggleMode);

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

            if (currentPlayer == xChar)
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
                if (isDarkMode)
                    SetTurnIndicatorColor("#ffffff");
                else
                    SetTurnIndicatorColor("#555555");

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
                if (isDarkMode)
                    SetTurnIndicatorColor("#ffffff");
                else
                    SetTurnIndicatorColor("#555555");

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
        currentPlayer = (currentPlayer == xChar) ? oChar : xChar;

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
        float randomHueO;

        do
        {
            randomHueO = Random.Range(0f, 1f);
        } while (Mathf.Abs(randomHueX - randomHueO) < 0.09f);

        xColor = Color.HSVToRGB(randomHueX, 0.99f, 0.83f);
        oColor = Color.HSVToRGB(randomHueO, 0.99f, 0.83f);

        xColor.a = 1f;
        oColor.a = 1f;
    }


    public void ResetGame()
    {
        currentPlayer = xChar;
        boardState = new string[9];

        if (isDarkMode)
            SetTurnIndicatorColor("#616161");
        else
            SetTurnIndicatorColor("#C5C5C5");

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
                     .setOnStart(() => popSFX.Play())
                     .setOnComplete(() =>
                     {
                         WobbleButton(gridSpaces[index]);
                         //popSFX.Play();
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

    public void ToggleMode()
    {
        Color targetCameraColor = isDarkMode ? lightModeCameraColor : darkModeCameraColor;
        Color targetButtonColor = isDarkMode ? lightModeButtonColor : darkModeButtonColor;
        Color targetTurnTextColor = isDarkMode ? lightModeTurnTextColor : darkModeTurnTextColor;
        Color targetTimerTextColor = isDarkMode ? lightModeTimerTextColor : darkModeTimerTextColor;
        Color targetBGColor = isDarkMode ? lightModeBGColor : darkModeBGColor;
        Color targetLogoColor = isDarkMode ? lightModeLogoColor : darkModeLogoColor;
        Color targetGameLogoColor = isDarkMode ? new Color(1f, 1f, 1f, 1f) : new Color(0f, 0f, 0f, 0.6f);

        LeanTween.value(gameObject, mainCamera.backgroundColor, targetCameraColor, 0.15f)
                 .setOnUpdate((Color color) => { mainCamera.backgroundColor = color; });

        foreach (Button button in gridSpaces)
        {
            LeanTween.value(button.gameObject, button.GetComponent<Image>().color, targetButtonColor, 0.15f)
                     .setOnUpdate((Color color) => { button.GetComponent<Image>().color = color; });

            LeanTween.scale(button.gameObject, Vector3.one * 1.05f, 0.1f)
                     .setEaseOutQuad()
                     .setLoopPingPong(1);
        }

        LeanTween.value(gameObject, turnIndicatorText.color, targetTurnTextColor, 0.15f)
                 .setOnUpdate((Color color) => { turnIndicatorText.color = color; });

        /*LeanTween.scale(turnIndicatorText.gameObject, Vector3.one * 1.05f, 0.1f)
                 .setEaseOutQuad()
                 .setLoopPingPong(1);
*/
        LeanTween.value(gameObject, timerText.color, targetTimerTextColor, 0.15f)
                 .setOnUpdate((Color color) => { timerText.color = color; });

/*        LeanTween.scale(timerText.gameObject, Vector3.one * 1.05f, 0.1f)
                 .setEaseOutQuad()
                 .setLoopPingPong(1);*/

        LeanTween.value(bgImage.gameObject, bgImage.color, targetBGColor, 0.15f)
                 .setOnUpdate((Color color) => { bgImage.color = color; });
/*
        LeanTween.scale(bgImage.gameObject, Vector3.one * 1.05f, 0.1f)
                 .setEaseOutQuad()
                 .setLoopPingPong(1);*/

        LeanTween.value(waelLogo.gameObject, waelLogo.color, targetLogoColor, 0.15f)
                 .setOnUpdate((Color color) => { waelLogo.color = color; });

        LeanTween.scale(waelLogo.gameObject, Vector3.one * 1.05f, 0.1f)
                 .setEaseOutQuad()
                 .setLoopPingPong(1);

        LeanTween.value(gameLogo.gameObject, gameLogo.color, targetGameLogoColor, 0.15f)
                 .setOnUpdate((Color color) => { gameLogo.color = color; });

        /*        LeanTween.scale(gameLogo.gameObject, Vector3.one * 1.05f, 0.1f)
                         .setEaseOutQuad()
                         .setLoopPingPong(1);*/

        isDarkMode = !isDarkMode;

        if (isDarkMode)
        {
            logoText.text = "bit<color=#3F3F3F>-<color=#2EAAD2>tac<color=#3F3F3F>-<color=#D22E60>toe";
        }
        else
        {
            logoText.text = "bit<color=#9D9D9D>-<color=#2EAAD2>tac<color=#9D9D9D>-<color=#D22E60>toe";
        }
    }


}
