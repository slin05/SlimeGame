using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

public class CookingManager : MonoBehaviourPunCallbacks
{
    public static CookingManager Instance { get; private set; }

    public RectTransform combatLogPanel;
    public RectTransform messageContainer;
    public int maxMessages = 5;
    public float messageDisplayTime = 4f;
    public float messageFadeTime = 1f;
    public float roundTime = 120f;

    private List<Recipe> recipes;
    private int currentRecipeIndex = 0;
    private int currentIngredientStep = 0;
    private float timeRemaining;
    private bool gameActive = false;

    private TextMeshProUGUI timerText;
    private TextMeshProUGUI recipeText;
    private readonly List<GameObject> activeMessages = new List<GameObject>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        BuildUIAtRuntime();
        BuildRecipes();
        timeRemaining = roundTime;
        gameActive = true;
        UpdateRecipeUI();
    }

    void Update()
    {
        if (!gameActive || !PhotonNetwork.IsMasterClient) return;

        timeRemaining -= Time.deltaTime;
        photonView.RPC(nameof(RPC_SyncTimer), RpcTarget.All, timeRemaining);

        if (timeRemaining <= 0)
        {
            gameActive = false;
            photonView.RPC(nameof(RPC_GameOver), RpcTarget.All, false);
        }
    }

    void BuildRecipes()
    {
        recipes = new List<Recipe>
        {
            new Recipe { recipeName = "Slime Stew",    ingredients = new List<SlimeType> { SlimeType.Red, SlimeType.Blue, SlimeType.Green } },
            new Recipe { recipeName = "Golden Broth",  ingredients = new List<SlimeType> { SlimeType.Yellow, SlimeType.Yellow, SlimeType.Red } },
            new Recipe { recipeName = "Midnight Soup", ingredients = new List<SlimeType> { SlimeType.Blue, SlimeType.Blue, SlimeType.Blue } },
        };
    }

    [PunRPC]
    void RPC_SyncTimer(float time)
    {
        timeRemaining = time;
        if (timerText != null)
            timerText.text = "Time: " + Mathf.CeilToInt(timeRemaining) + "s";
    }

    [PunRPC]
    public void RPC_SlimeAdded(int slimeTypeInt, string playerName)
    {
        if (currentRecipeIndex >= recipes.Count) return;

        SlimeType added = (SlimeType)slimeTypeInt;
        Recipe current = recipes[currentRecipeIndex];
        SlimeType needed = current.ingredients[currentIngredientStep];

        if (added == needed)
        {
            currentIngredientStep++;
            SpawnMessageRow($"✓ {playerName} added {added} slime!", new Color(0.35f, 1f, 0.35f));

            if (currentIngredientStep >= current.ingredients.Count)
            {
                SpawnMessageRow($"🍲 {current.recipeName} complete!", Color.yellow);
                currentRecipeIndex++;
                currentIngredientStep = 0;

                if (currentRecipeIndex >= recipes.Count)
                {
                    gameActive = false;
                    if (PhotonNetwork.IsMasterClient)
                        photonView.RPC(nameof(RPC_GameOver), RpcTarget.All, true);
                    return;
                }
            }

            UpdateRecipeUI();
        }
        else
        {
            SpawnMessageRow($"✗ {playerName} added wrong slime! Need {needed}", new Color(1f, 0.35f, 0.35f));
        }
    }

    [PunRPC]
    void RPC_GameOver(bool won)
    {
        gameActive = false;
        StartCoroutine(GameOverSequence(won));
    }

    IEnumerator GameOverSequence(bool won)
    {
        ShowGameOverPanel(won);
        yield return new WaitForSeconds(5f);
        if (PhotonNetwork.IsMasterClient)
            PhotonNetwork.LoadLevel("LobbyScene");
    }

    void ShowGameOverPanel(bool won)
    {
        GameObject overlayGO = new GameObject("GameOverOverlay");
        Canvas canvas = overlayGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        CanvasScaler scaler = overlayGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        overlayGO.AddComponent<GraphicRaycaster>();

        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(canvas.transform, false);
        Image bg = bgGO.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.88f);
        RectTransform bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;

        GameObject titleGO = new GameObject("TitleText");
        titleGO.transform.SetParent(canvas.transform, false);
        TextMeshProUGUI titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text = won ? "🍲 All Recipes Complete!" : "⏰ Time's Up!";
        titleTMP.fontSize = 64f;
        titleTMP.color = won ? new Color(1f, 0.85f, 0.1f) : new Color(1f, 0.35f, 0.35f);
        titleTMP.alignment = TextAlignmentOptions.Center;
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.5f, 0.62f);
        titleRT.anchorMax = new Vector2(0.5f, 0.62f);
        titleRT.pivot = Vector2.one * 0.5f;
        titleRT.anchoredPosition = Vector2.zero;
        titleRT.sizeDelta = new Vector2(900f, 90f);

        string scoreText = won
            ? $"You cooked all {recipes.Count} recipes together!"
            : $"Completed {currentRecipeIndex} / {recipes.Count} recipes.";

        GameObject scoreGO = new GameObject("ScoreText");
        scoreGO.transform.SetParent(canvas.transform, false);
        TextMeshProUGUI scoreTMP = scoreGO.AddComponent<TextMeshProUGUI>();
        scoreTMP.text = scoreText;
        scoreTMP.fontSize = 28f;
        scoreTMP.color = Color.white;
        scoreTMP.alignment = TextAlignmentOptions.Center;
        RectTransform scoreRT = scoreGO.GetComponent<RectTransform>();
        scoreRT.anchorMin = new Vector2(0.5f, 0.45f);
        scoreRT.anchorMax = new Vector2(0.5f, 0.45f);
        scoreRT.pivot = Vector2.one * 0.5f;
        scoreRT.anchoredPosition = Vector2.zero;
        scoreRT.sizeDelta = new Vector2(600f, 80f);

        GameObject countdownGO = new GameObject("CountdownText");
        countdownGO.transform.SetParent(canvas.transform, false);
        TextMeshProUGUI countdownTMP = countdownGO.AddComponent<TextMeshProUGUI>();
        countdownTMP.text = "Returning to lobby in 5...";
        countdownTMP.fontSize = 20f;
        countdownTMP.color = new Color(0.65f, 0.65f, 0.65f);
        countdownTMP.alignment = TextAlignmentOptions.Center;
        RectTransform countdownRT = countdownGO.GetComponent<RectTransform>();
        countdownRT.anchorMin = new Vector2(0.5f, 0.28f);
        countdownRT.anchorMax = new Vector2(0.5f, 0.28f);
        countdownRT.pivot = Vector2.one * 0.5f;
        countdownRT.anchoredPosition = Vector2.zero;
        countdownRT.sizeDelta = new Vector2(500f, 40f);

        StartCoroutine(TickCountdown(countdownTMP, 5));
    }

    IEnumerator TickCountdown(TextMeshProUGUI label, int seconds)
    {
        for (int i = seconds; i > 0; i--)
        {
            if (label != null) label.text = $"Returning to lobby in {i}...";
            yield return new WaitForSeconds(1f);
        }
    }

    void UpdateRecipeUI()
    {
        if (recipeText == null || currentRecipeIndex >= recipes.Count) return;

        Recipe current = recipes[currentRecipeIndex];
        string text = $"Recipe {currentRecipeIndex + 1}/{recipes.Count}: {current.recipeName}\n";

        for (int i = 0; i < current.ingredients.Count; i++)
        {
            string check = i < currentIngredientStep ? "✓" : (i == currentIngredientStep ? "▶" : "○");
            text += $"  {check} {current.ingredients[i]}\n";
        }

        recipeText.text = text;
    }

    void SpawnMessageRow(string text, Color color)
    {
        if (activeMessages.Count >= maxMessages)
        {
            GameObject oldest = activeMessages[0];
            activeMessages.RemoveAt(0);
            Destroy(oldest);
        }

        GameObject row = new GameObject("FeedMsg", typeof(RectTransform));
        row.transform.SetParent(messageContainer, false);
        TextMeshProUGUI tmp = row.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.color = color;
        tmp.fontSize = 14f;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        RectTransform rt = row.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(364f, 24f);
        activeMessages.Add(row);
        StartCoroutine(FadeAndRemove(row, tmp, messageDisplayTime, messageFadeTime));
    }

    IEnumerator FadeAndRemove(GameObject row, TextMeshProUGUI tmp, float displayTime, float fadeTime)
    {
        yield return new WaitForSeconds(displayTime);
        float elapsed = 0f;
        Color startColor = tmp.color;
        while (elapsed < fadeTime)
        {
            if (row == null) yield break;
            elapsed += Time.deltaTime;
            tmp.color = new Color(startColor.r, startColor.g, startColor.b, Mathf.Lerp(1f, 0f, elapsed / fadeTime));
            yield return null;
        }
        activeMessages.Remove(row);
        if (row != null) Destroy(row);
    }

    void BuildUIAtRuntime()
    {
        GameObject canvasGO = new GameObject("CookingUICanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        if (combatLogPanel == null)
        {
            GameObject panelGO = new GameObject("FeedPanel");
            panelGO.transform.SetParent(canvas.transform, false);
            Image bg = panelGO.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.45f);
            combatLogPanel = panelGO.GetComponent<RectTransform>();
            combatLogPanel.anchorMin = new Vector2(0f, 0f);
            combatLogPanel.anchorMax = new Vector2(0f, 0f);
            combatLogPanel.pivot = new Vector2(0f, 0f);
            combatLogPanel.anchoredPosition = new Vector2(16f, 16f);
            combatLogPanel.sizeDelta = new Vector2(380f, 160f);
        }

        if (messageContainer == null)
        {
            GameObject containerGO = new GameObject("MessageContainer");
            containerGO.transform.SetParent(combatLogPanel.transform, false);
            VerticalLayoutGroup vlg = containerGO.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.LowerLeft;
            vlg.spacing = 3f;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(8, 8, 6, 6);
            ContentSizeFitter csf = containerGO.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            messageContainer = containerGO.GetComponent<RectTransform>();
            messageContainer.anchorMin = Vector2.zero;
            messageContainer.anchorMax = Vector2.one;
            messageContainer.offsetMin = Vector2.zero;
            messageContainer.offsetMax = Vector2.zero;
        }

        GameObject recipeGO = new GameObject("RecipePanel");
        recipeGO.transform.SetParent(canvas.transform, false);
        Image recipeBg = recipeGO.AddComponent<Image>();
        recipeBg.color = new Color(0f, 0f, 0f, 0.6f);
        RectTransform recipeRT = recipeGO.GetComponent<RectTransform>();
        recipeRT.anchorMin = new Vector2(1f, 1f);
        recipeRT.anchorMax = new Vector2(1f, 1f);
        recipeRT.pivot = new Vector2(1f, 1f);
        recipeRT.anchoredPosition = new Vector2(-16f, -60f);
        recipeRT.sizeDelta = new Vector2(280f, 160f);

        GameObject recipeTextGO = new GameObject("RecipeText");
        recipeTextGO.transform.SetParent(recipeGO.transform, false);
        recipeText = recipeTextGO.AddComponent<TextMeshProUGUI>();
        recipeText.fontSize = 16f;
        recipeText.color = Color.white;
        recipeText.alignment = TextAlignmentOptions.TopLeft;
        RectTransform recipeTextRT = recipeTextGO.GetComponent<RectTransform>();
        recipeTextRT.anchorMin = Vector2.zero;
        recipeTextRT.anchorMax = Vector2.one;
        recipeTextRT.offsetMin = new Vector2(8f, 8f);
        recipeTextRT.offsetMax = new Vector2(-8f, -8f);

        GameObject timerGO = new GameObject("TimerText");
        timerGO.transform.SetParent(canvas.transform, false);
        timerText = timerGO.AddComponent<TextMeshProUGUI>();
        timerText.fontSize = 22f;
        timerText.color = Color.white;
        timerText.alignment = TextAlignmentOptions.TopRight;
        timerText.text = "Time: " + Mathf.CeilToInt(roundTime) + "s";
        RectTransform timerRT = timerGO.GetComponent<RectTransform>();
        timerRT.anchorMin = new Vector2(1f, 1f);
        timerRT.anchorMax = new Vector2(1f, 1f);
        timerRT.pivot = new Vector2(1f, 1f);
        timerRT.anchoredPosition = new Vector2(-16f, -16f);
        timerRT.sizeDelta = new Vector2(200f, 36f);
    }
}