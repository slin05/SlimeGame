using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

public class PlayerController : MonoBehaviourPunCallbacks
{
    public float speed = 5f;
    public float slimeSpeed = 10f;

    private SlimeType currentSlimeType = SlimeType.Red;
    private TextMeshProUGUI slimeIndicatorText;

    private static readonly Color[] slimeColors = { Color.red, Color.green, Color.blue, Color.yellow };
    private int slimeCount => System.Enum.GetValues(typeof(SlimeType)).Length;

    void Start()
    {
        if (!photonView.IsMine) return;
        BuildSlimeIndicator();
        UpdateSlimeIndicator();
    }

    void Update()
    {
        if (!photonView.IsMine) return;

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        transform.position += new Vector3(h, 0, v) * speed * Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.Q))
        {
            currentSlimeType = (SlimeType)(((int)currentSlimeType - 1 + slimeCount) % slimeCount);
            UpdateSlimeIndicator();
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            currentSlimeType = (SlimeType)(((int)currentSlimeType + 1) % slimeCount);
            UpdateSlimeIndicator();
        }

        if (Input.GetMouseButtonDown(0)) ThrowSlime();
    }

    void ThrowSlime()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Vector3 targetPoint = ray.GetPoint(10f);
        Vector3 direction = (targetPoint - transform.position).normalized;

        object[] data = new object[] { (int)currentSlimeType };
        GameObject slimeObj = PhotonNetwork.Instantiate("PhotonSlime", transform.position, Quaternion.identity, 0, data);
        slimeObj.GetComponent<Rigidbody>().linearVelocity = direction * slimeSpeed;
    }

    void BuildSlimeIndicator()
    {
        GameObject canvasGO = new GameObject("SlimeIndicatorCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        GameObject bg = new GameObject("IndicatorBG");
        bg.transform.SetParent(canvas.transform, false);
        UnityEngine.UI.Image img = bg.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(0f, 0f, 0f, 0.6f);
        RectTransform bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0.5f, 0f);
        bgRT.anchorMax = new Vector2(0.5f, 0f);
        bgRT.pivot = new Vector2(0.5f, 0f);
        bgRT.anchoredPosition = new Vector2(0f, 20f);
        bgRT.sizeDelta = new Vector2(220f, 40f);

        GameObject textGO = new GameObject("SlimeText");
        textGO.transform.SetParent(bg.transform, false);
        slimeIndicatorText = textGO.AddComponent<TextMeshProUGUI>();
        slimeIndicatorText.fontSize = 18f;
        slimeIndicatorText.alignment = TextAlignmentOptions.Center;
        RectTransform textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;
    }

    void UpdateSlimeIndicator()
    {
        if (slimeIndicatorText == null) return;
        slimeIndicatorText.text = $"[Q] ◀  {currentSlimeType} Slime  ▶ [E]";
        slimeIndicatorText.color = slimeColors[(int)currentSlimeType];
    }
}