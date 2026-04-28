using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

public class GameSceneManager : MonoBehaviourPunCallbacks
{
    public static GameSceneManager Instance { get; private set; }

    [Header("Combat Log UI")]
    [Tooltip("Root panel that holds the combat feed (auto-created if null).")]
    public RectTransform combatLogPanel;

    [Tooltip("Vertical-layout parent for individual message rows.")]
    public RectTransform messageContainer;

    [Header("Feed Settings")]
    [Tooltip("Maximum messages visible at once.")]
    public int maxMessages = 5;

    [Tooltip("Seconds a message stays at full opacity before fading.")]
    public float messageDisplayTime = 4f;

    [Tooltip("Seconds it takes to fade out.")]
    public float messageFadeTime = 1f;

    private readonly Dictionary<int, bool> alivePlayerMap = new Dictionary<int, bool>();
    private TextMeshProUGUI playersRemainingText;

    private readonly Dictionary<int, int> killStreaks = new Dictionary<int, int>();
    private readonly List<GameObject> activeMessages = new List<GameObject>();
    private readonly Dictionary<int, int> playerKills = new Dictionary<int, int>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (combatLogPanel == null || messageContainer == null)
            BuildUIAtRuntime();

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            alivePlayerMap[player.ActorNumber] = true;
        }

        UpdatePlayersRemainingUI();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        alivePlayerMap[newPlayer.ActorNumber] = true;
        UpdatePlayersRemainingUI();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        alivePlayerMap.Remove(otherPlayer.ActorNumber);
        UpdatePlayersRemainingUI();

        if (PhotonNetwork.IsMasterClient)
            CheckGameOver();
    }

    [PunRPC]
    public void RPC_PlayerDied(int actorNumber)
    {
        alivePlayerMap[actorNumber] = false;
        UpdatePlayersRemainingUI();
        CheckGameOver();
    }

    private void CheckGameOver()
    {
        int alivePlayers = 0;
        string winnerName = "Nobody";

        foreach (var kvp in alivePlayerMap)
        {
            if (!kvp.Value) continue;

            alivePlayers++;

            foreach (Player p in PhotonNetwork.PlayerList)
            {
                if (p.ActorNumber == kvp.Key)
                {
                    winnerName = p.NickName;
                    break;
                }
            }
        }

        if (alivePlayers <= 1)
        {
            photonView.RPC(nameof(RPC_GameOver), RpcTarget.All, winnerName);
        }
    }

    [PunRPC]
    public void RPC_GameOver(string winnerName)
    {
        StartCoroutine(GameOverSequence(winnerName));
    }

    private IEnumerator GameOverSequence(string winnerName)
    {
        ShowGameOverPanel(winnerName);
        yield return new WaitForSeconds(5f);

        if (PhotonNetwork.IsMasterClient)
            PhotonNetwork.LoadLevel("LobbyScene");
    }

    private void ShowGameOverPanel(string winnerName)
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

        GameObject winnerGO = new GameObject("WinnerText");
        winnerGO.transform.SetParent(canvas.transform, false);
        TextMeshProUGUI winnerTMP = winnerGO.AddComponent<TextMeshProUGUI>();
        winnerTMP.text = $"<b>{winnerName}</b> WINS!";
        winnerTMP.fontSize = 64f;
        winnerTMP.color = new Color(1f, 0.85f, 0.1f);
        winnerTMP.alignment = TextAlignmentOptions.Center;
        RectTransform winnerRT = winnerGO.GetComponent<RectTransform>();
        winnerRT.anchorMin = new Vector2(0.5f, 0.62f);
        winnerRT.anchorMax = new Vector2(0.5f, 0.62f);
        winnerRT.pivot = Vector2.one * 0.5f;
        winnerRT.anchoredPosition = Vector2.zero;
        winnerRT.sizeDelta = new Vector2(800f, 90f);

        string scoreText = "── Kill Count ──\n\n";
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            int kills = playerKills.ContainsKey(p.ActorNumber) ? playerKills[p.ActorNumber] : 0;
            string killLabel = kills == 1 ? "kill" : "kills";
            scoreText += $"<b>{p.NickName}</b>  {kills} {killLabel}\n";
        }

        GameObject scoreGO = new GameObject("ScoreText");
        scoreGO.transform.SetParent(canvas.transform, false);
        TextMeshProUGUI scoreTMP = scoreGO.AddComponent<TextMeshProUGUI>();
        scoreTMP.text = scoreText;
        scoreTMP.fontSize = 28f;
        scoreTMP.color = Color.white;
        scoreTMP.alignment = TextAlignmentOptions.Center;
        RectTransform scoreRT = scoreGO.GetComponent<RectTransform>();
        scoreRT.anchorMin = new Vector2(0.5f, 0.38f);
        scoreRT.anchorMax = new Vector2(0.5f, 0.38f);
        scoreRT.pivot = Vector2.one * 0.5f;
        scoreRT.anchoredPosition = Vector2.zero;
        scoreRT.sizeDelta = new Vector2(500f, 240f);

        GameObject countdownGO = new GameObject("CountdownText");
        countdownGO.transform.SetParent(canvas.transform, false);
        TextMeshProUGUI countdownTMP = countdownGO.AddComponent<TextMeshProUGUI>();
        countdownTMP.text = "Returning to lobby in 5...";
        countdownTMP.fontSize = 20f;
        countdownTMP.color = new Color(0.65f, 0.65f, 0.65f, 1f);
        countdownTMP.alignment = TextAlignmentOptions.Center;
        RectTransform countdownRT = countdownGO.GetComponent<RectTransform>();
        countdownRT.anchorMin = new Vector2(0.5f, 0.22f);
        countdownRT.anchorMax = new Vector2(0.5f, 0.22f);
        countdownRT.pivot = Vector2.one * 0.5f;
        countdownRT.anchoredPosition = Vector2.zero;
        countdownRT.sizeDelta = new Vector2(500f, 40f);

        StartCoroutine(TickCountdown(countdownTMP, 5));
    }

    private IEnumerator TickCountdown(TextMeshProUGUI label, int seconds)
    {
        for (int i = seconds; i > 0; i--)
        {
            if (label != null)
                label.text = $"Returning to lobby in {i}...";
            yield return new WaitForSeconds(1f);
        }
    }

    private void UpdatePlayersRemainingUI()
    {
        if (playersRemainingText == null) return;

        int alive = 0;
        foreach (bool status in alivePlayerMap.Values)
        {
            if (status) alive++;
        }

        playersRemainingText.text = "Players Remaining: " + alive;
    }

    public void BroadcastHitMessage(string attackerName, string victimName,
                                    int attackerActorNumber, int victimActorNumber,
                                    int damage)
    {
        photonView.RPC(nameof(RPC_ShowCombatMessage), RpcTarget.All,
                       attackerName, victimName,
                       attackerActorNumber, victimActorNumber,
                       damage, false);
    }

    public void BroadcastKillMessage(string attackerName, string victimName,
                                     int attackerActorNumber, int victimActorNumber)
    {
        photonView.RPC(nameof(RPC_ShowCombatMessage), RpcTarget.All,
                       attackerName, victimName,
                       attackerActorNumber, victimActorNumber,
                       0, true);
    }

    [PunRPC]
    public void RPC_ShowCombatMessage(string attackerName, string victimName,
                                      int attackerActorNumber, int victimActorNumber,
                                      int damage, bool isKill)
    {
        string msgText;
        if (isKill)
            msgText = $"<b>{attackerName}</b> eliminated <b>{victimName}</b>";
        else
            msgText = $"<b>{attackerName}</b> hit <b>{victimName}</b> for {damage} dmg";

        string streakAnnouncement = null;
        if (isKill)
        {
            if (!killStreaks.ContainsKey(attackerActorNumber))
                killStreaks[attackerActorNumber] = 0;

            killStreaks[attackerActorNumber]++;
            int streak = killStreaks[attackerActorNumber];

            killStreaks[victimActorNumber] = 0;

            if (streak == 3)
                streakAnnouncement = $"<color=#FFA500><b>{attackerName}</b> is on a KILLING SPREE! ({streak} kills)</color>";
            else if (streak == 5)
                streakAnnouncement = $"<color=#FF4500><b>{attackerName}</b> is UNSTOPPABLE! ({streak} kills)</color>";
            else if (streak >= 7 && streak % 2 == 1)
                streakAnnouncement = $"<color=#FF0000><b>{attackerName}</b> is GODLIKE! ({streak} kills)</color>";

            if (!playerKills.ContainsKey(attackerActorNumber))
                playerKills[attackerActorNumber] = 0;
            playerKills[attackerActorNumber]++;
        }

        Color msgColor;
        int localActor = PhotonNetwork.LocalPlayer.ActorNumber;

        if (attackerActorNumber == localActor)
            msgColor = new Color(0.35f, 1f, 0.35f);
        else if (victimActorNumber == localActor)
            msgColor = new Color(1f, 0.35f, 0.35f);
        else
            msgColor = Color.white;

        SpawnMessageRow(msgText, msgColor);

        if (streakAnnouncement != null)
            SpawnMessageRow(streakAnnouncement, new Color(1f, 0.85f, 0.1f));
    }

    private void SpawnMessageRow(string text, Color color)
    {
        if (activeMessages.Count >= maxMessages)
        {
            GameObject oldest = activeMessages[0];
            activeMessages.RemoveAt(0);
            Destroy(oldest);
        }

        GameObject row = new GameObject("CombatMsg", typeof(RectTransform));
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

    private IEnumerator FadeAndRemove(GameObject row, TextMeshProUGUI tmp,
                                      float displayTime, float fadeTime)
    {
        yield return new WaitForSeconds(displayTime);

        float elapsed = 0f;
        Color startColor = tmp.color;

        while (elapsed < fadeTime)
        {
            if (row == null) yield break;
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
            tmp.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        activeMessages.Remove(row);
        if (row != null) Destroy(row);
    }

    private void BuildUIAtRuntime()
    {
        GameObject canvasGO = new GameObject("CombatLogCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        if (combatLogPanel == null)
        {
            GameObject panelGO = new GameObject("CombatLogPanel");
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

        GameObject labelGO = new GameObject("PlayersRemainingLabel");
        labelGO.transform.SetParent(canvas.transform, false);

        playersRemainingText = labelGO.AddComponent<TextMeshProUGUI>();
        playersRemainingText.fontSize = 18f;
        playersRemainingText.color = Color.white;
        playersRemainingText.alignment = TextAlignmentOptions.TopRight;
        playersRemainingText.text = "Players Remaining: --";

        RectTransform labelRT = labelGO.GetComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(1f, 1f);
        labelRT.anchorMax = new Vector2(1f, 1f);
        labelRT.pivot = new Vector2(1f, 1f);
        labelRT.anchoredPosition = new Vector2(-16f, -16f);
        labelRT.sizeDelta = new Vector2(280f, 36f);
    }
}