using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GameSetupController : MonoBehaviour
{
    [SerializeField] private string tableSceneName = "GameTable";

    private Text txtVenueName, txtVenueDetail;
    private Button btnStart, btnBack;
    private List<Button> venueButtons = new List<Button>();
    private GameSettings.Venue selectedVenue;

    private void Awake() { EnsureCanvasComponents(); EnsureEventSystem(); BuildUI(); }
    private void Start() { selectedVenue = GameSettings.SelectedVenue; RefreshUI(); }

    private void EnsureCanvasComponents()
    {
        if (!GetComponent<CanvasScaler>()) { var s = gameObject.AddComponent<CanvasScaler>(); s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; s.referenceResolution = new Vector2(1920, 1080); s.matchWidthOrHeight = 0.5f; }
        if (!GetComponent<GraphicRaycaster>()) gameObject.AddComponent<GraphicRaycaster>();
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() == null) { var go = new GameObject("EventSystem"); go.AddComponent<EventSystem>(); go.AddComponent<StandaloneInputModule>(); }
    }

    private void BuildUI()
    {
        for (int i = transform.childCount - 1; i >= 0; i--) { var c = transform.GetChild(i); if (c.name != "Main Camera") Destroy(c.gameObject); }
        var canvas = GetComponent<RectTransform>();
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Title
        MakeLabel("Title", canvas, "选择游玩场所", 40, 0.88f, 500, 60, font);

        // Venue cards - 5 cards in a row
        string[] venueNames = { "朋友家", "酒吧", "市级比赛", "省级比赛", "德州王锦标赛" };
        string[] venueLevels = { "LV1-20", "LV20-40", "LV40-60", "LV60-80", "LV80-100" };
        float[] xPos = { 0.15f, 0.32f, 0.50f, 0.68f, 0.85f };

        for (int i = 0; i < 5; i++)
        {
            var btn = MakeVenueCard("Btn_Venue_" + i, venueNames[i], venueLevels[i],
                new Vector2(xPos[i], 0.52f), canvas, font);
            int idx = i;
            btn.onClick.AddListener(() => SelectVenue((GameSettings.Venue)idx));
            venueButtons.Add(btn);
        }

        // Venue name display
        txtVenueName = MakeLabel("Txt_VenueName", canvas, "", 36, 0.35f, 600, 50, font);
        txtVenueName.color = new Color(1f, 0.85f, 0.3f);

        // Venue detail display
        txtVenueDetail = MakeLabel("Txt_VenueDetail", canvas, "", 20, 0.22f, 600, 80, font);
        txtVenueDetail.color = new Color(0.7f, 0.75f, 0.85f);

        // Buttons
        btnStart = MakeBigBtn("Btn_Start", "开始游戏", new Vector2(0.5f, 0.08f), canvas, font, new Color(0.2f, 0.55f, 0.3f));
        btnBack  = MakeBigBtn("Btn_Back",  "返回",  new Vector2(0.5f, 0.03f), canvas, font, new Color(0.3f, 0.32f, 0.38f));
        btnStart.onClick.AddListener(StartGame);
        btnBack.onClick.AddListener(GoBack);
    }

    private Text MakeLabel(string n, RectTransform p, string t, int s, float y, float w, float h, Font f)
    {
        var go = new GameObject(n, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)); go.layer = 5;
        var rt = go.GetComponent<RectTransform>(); rt.SetParent(p, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, y); rt.anchoredPosition = Vector2.zero; rt.sizeDelta = new Vector2(w, h);
        var tx = go.GetComponent<Text>(); tx.font = f; tx.text = t; tx.fontSize = s; tx.alignment = TextAnchor.MiddleCenter;
        tx.color = new Color(0.9f, 0.9f, 0.95f); tx.raycastTarget = false; return tx;
    }

    private Button MakeVenueCard(string n, string name, string lv, Vector2 a, RectTransform p, Font f)
    {
        var go = new GameObject(n, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button)); go.layer = 5;
        var rt = go.GetComponent<RectTransform>(); rt.SetParent(p, false);
        rt.anchorMin = rt.anchorMax = a; rt.anchoredPosition = Vector2.zero; rt.sizeDelta = new Vector2(190, 160);
        go.GetComponent<Image>().color = new Color(0.15f, 0.18f, 0.22f);
        var btn = go.GetComponent<Button>(); btn.targetGraphic = go.GetComponent<Image>();

        // Venue name on card
        var nl = new GameObject("Name", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)); nl.layer = 5;
        var nlr = nl.GetComponent<RectTransform>(); nlr.SetParent(rt, false);
        nlr.anchorMin = new Vector2(0, 0.6f); nlr.anchorMax = new Vector2(1, 0.95f); nlr.sizeDelta = Vector2.zero;
        var nt = nl.GetComponent<Text>(); nt.font = f; nt.text = name; nt.fontSize = 22;
        nt.alignment = TextAnchor.MiddleCenter; nt.color = Color.white; nt.raycastTarget = false;

        // Level label
        var ll = new GameObject("Level", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)); ll.layer = 5;
        var llr = ll.GetComponent<RectTransform>(); llr.SetParent(rt, false);
        llr.anchorMin = new Vector2(0, 0.05f); llr.anchorMax = new Vector2(1, 0.5f); llr.sizeDelta = Vector2.zero;
        var lt = ll.GetComponent<Text>(); lt.font = f; lt.text = lv; lt.fontSize = 16;
        lt.alignment = TextAnchor.MiddleCenter; lt.color = new Color(0.5f, 0.55f, 0.6f); lt.raycastTarget = false;

        return btn;
    }

    private Button MakeBigBtn(string n, string l, Vector2 a, RectTransform p, Font f, Color c)
    {
        var go = new GameObject(n, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button)); go.layer = 5;
        var rt = go.GetComponent<RectTransform>(); rt.SetParent(p, false);
        rt.anchorMin = rt.anchorMax = a; rt.anchoredPosition = Vector2.zero; rt.sizeDelta = new Vector2(220, 48);
        go.GetComponent<Image>().color = c; var btn = go.GetComponent<Button>(); btn.targetGraphic = go.GetComponent<Image>();
        var lb = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)); lb.layer = 5;
        var lr = lb.GetComponent<RectTransform>(); lr.SetParent(rt, false);
        lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one; lr.sizeDelta = Vector2.zero;
        var lt = lb.GetComponent<Text>(); lt.font = f; lt.text = l; lt.fontSize = 22;
        lt.alignment = TextAnchor.MiddleCenter; lt.color = Color.white; lt.raycastTarget = false;
        return btn;
    }

    private void SelectVenue(GameSettings.Venue v)
    {
        selectedVenue = v;
        RefreshUI();
    }

    private void RefreshUI()
    {
        var cfg = GameSettings.GetVenueConfig(selectedVenue);
        txtVenueName.text = cfg.Name;
        txtVenueDetail.text = string.Format(
            "人数: {0}-{1} | 困难AI: ≥{2} | 起始下注: {3}\n起始资金: {4}-{5} | 最大加注: {6} | ALL IN: ≤{7}",
            cfg.MinPlayers, cfg.MaxPlayers, cfg.MinHardAI, cfg.SmallBlind,
            cfg.AiChipsMin, cfg.AiChipsMax, cfg.MaxRaise, cfg.MaxAllIn);

        Color active = new Color(0.2f, 0.6f, 0.3f);
        Color inactive = new Color(0.15f, 0.18f, 0.22f);
        for (int i = 0; i < venueButtons.Count; i++)
        {
            venueButtons[i].GetComponent<Image>().color = (int)selectedVenue == i ? active : inactive;
        }
    }

    private void StartGame()
    {
        GameSettings.SelectedVenue = selectedVenue;
        GameSettings.Save();
        SceneManager.LoadScene(tableSceneName);
    }

    private void GoBack() { SceneManager.LoadScene("MainMenu"); }
}
