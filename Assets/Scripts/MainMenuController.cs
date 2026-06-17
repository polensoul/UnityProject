using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class MainMenuController : MonoBehaviour
{
    [Header("场景")]
    [SerializeField] private string gameSceneName = "GameSetup";
    [SerializeField] private string tableSceneName = "GameTable";

    // ? ??? ? ???????? ?
    private const string GAME_VERSION = "version.1.1(20260617)";

    // ── 运行时绑定的 UI 引用 ──────────────────────
    private Button btnNewGame, btnContinue, btnSettings, btnQuit;
    private GameObject settingsPanel, quitConfirmPanel;
    private RectTransform settingsPanelRT;

    // 动态创建设置控件
    private InputField inputPlayerName;
    private Slider sliderSoundVolume, sliderMusicVolume;
    private Button btnCloseSettings, btnQuitConfirm, btnQuitCancel;

    private void Awake()
    {
        Debug.Log("[MainMenu] Awake starting...");
        try { BuildBackground(); }
        catch (System.Exception e) { Debug.LogError("[MainMenu] BuildBackground failed: " + e.Message); }
        try { AutoBindUI(); }
        catch (System.Exception e) { Debug.LogError("[MainMenu] AutoBindUI failed: " + e.Message + "\n" + e.StackTrace); }

        try { BuildSettingsControls(); }
        catch (System.Exception e) { Debug.LogError("[MainMenu] BuildSettingsControls failed: " + e.Message + "\n" + e.StackTrace); }

        try { BuildPlayerInfo(); }
        catch (System.Exception e) { Debug.LogError("[MainMenu] BuildPlayerInfo failed: " + e.Message + "\n" + e.StackTrace); }
        try { BuildVersionLabel(); }
        catch (System.Exception e) { Debug.LogError("[MainMenu] BuildVersionLabel failed: " + e.Message); }

        Debug.Log("[MainMenu] Awake finished");
    }

    private void FixAllFonts()
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        foreach (var t in GetComponentsInChildren<Text>(true))
            t.font = font;
    }

    private void Start()
    {
        ApplyDisplaySettings();
        RefreshPlayerInfo();
        LoadSettingsToUI();
        settingsPanel.SetActive(false);
        quitConfirmPanel.SetActive(false);
    }

    /// <summary>通过名称自动查找主菜单和对话框 UI</summary>
    /// <summary>左上角玩家信息面板</summary>
    private void BuildPlayerInfo()
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var c = GetComponent<RectTransform>();

        // 背景面板
        var bg = new GameObject("PlayerInfoPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bg.layer = 5;
        var bgRt = bg.GetComponent<RectTransform>();
        bgRt.SetParent(c, false);
        bgRt.anchorMin = new Vector2(0, 1);
        bgRt.anchorMax = new Vector2(0, 1);
        bgRt.pivot = new Vector2(0, 1);
        bgRt.anchoredPosition = new Vector2(20, -20);
        bgRt.sizeDelta = new Vector2(280, 140);
        bg.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.14f, 0.85f);

        int chips = GameSettings.EffectiveChips;
        int rounds = GameSettings.RoundsPlayed;

        // 玩家名称
        var t1 = new GameObject("TxtName", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        t1.layer = 5;
        var t1rt = t1.GetComponent<RectTransform>();
        t1rt.SetParent(bgRt, false);
        t1rt.anchorMin = new Vector2(0, 1); t1rt.anchorMax = new Vector2(1, 1);
        t1rt.anchoredPosition = new Vector2(10, -12); t1rt.sizeDelta = new Vector2(-20, 26);
        var t1tx = t1.GetComponent<Text>();
        t1tx.font = font; t1tx.fontSize = 20; t1tx.alignment = TextAnchor.MiddleLeft;
        t1tx.color = new Color(1f, 0.85f, 0.3f);
        t1tx.text = "【玩家】" + GameSettings.PlayerName;
        txtInfoName = t1tx;

        // 资金
        var t2 = new GameObject("TxtChips", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        t2.layer = 5;
        var t2rt = t2.GetComponent<RectTransform>();
        t2rt.SetParent(bgRt, false);
        t2rt.anchorMin = new Vector2(0, 1); t2rt.anchorMax = new Vector2(1, 1);
        t2rt.anchoredPosition = new Vector2(10, -72); t2rt.sizeDelta = new Vector2(-20, 26);
        var t2tx = t2.GetComponent<Text>();
        t2tx.font = font; t2tx.fontSize = 18; t2tx.alignment = TextAnchor.MiddleLeft;
        t2tx.color = Color.white;
        t2tx.text = "资金: \u00a5" + chips.ToString("N0");
        txtInfoChips = t2tx;

        // Level
        int lv = GameSettings.Level;
        int xpCur = GameSettings.XPInCurrentLevel;
        int xpNext = GameSettings.XPForNextLevel - ((lv - 1) * lv / 2 * 1000);

        var tLv = new GameObject("TxtLevel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        tLv.layer = 5;
        var tLvRt = tLv.GetComponent<RectTransform>();
        tLvRt.SetParent(bgRt, false);
        tLvRt.anchorMin = new Vector2(0, 1); tLvRt.anchorMax = new Vector2(1, 1);
        tLvRt.anchoredPosition = new Vector2(10, -42); tLvRt.sizeDelta = new Vector2(-20, 26);
        var tLvTx = tLv.GetComponent<Text>();
        tLvTx.font = font; tLvTx.fontSize = 18; tLvTx.alignment = TextAnchor.MiddleLeft;
        tLvTx.color = new Color(0.3f, 0.9f, 0.5f);
        tLvTx.text = string.Format("Lv.{0} ({1}/{2})", lv, xpCur, xpNext);
        txtInfoLevel = tLvTx;
        // 局数
        var t3 = new GameObject("TxtRounds", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        t3.layer = 5;
        var t3rt = t3.GetComponent<RectTransform>();
        t3rt.SetParent(bgRt, false);
        t3rt.anchorMin = new Vector2(0, 1); t3rt.anchorMax = new Vector2(1, 1);
        t3rt.anchoredPosition = new Vector2(10, -102); t3rt.sizeDelta = new Vector2(-20, 26);
        var t3tx = t3.GetComponent<Text>();
        t3tx.font = font; t3tx.fontSize = 18; t3tx.alignment = TextAnchor.MiddleLeft;
        t3tx.color = new Color(0.7f, 0.75f, 0.85f);
        t3tx.text = "已玩局数: " + rounds;
        txtInfoRounds = t3tx;
    }

    /// <summary>左下角版本号</summary>
    private void BuildVersionLabel()
    {
        Debug.Log("[MainMenu] BuildVersionLabel start, version=" + GAME_VERSION);
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            font = Font.CreateDynamicFontFromOSFont("Arial", 14);
            Debug.LogWarning("[MainMenu] LegacyRuntime.ttf not found, fallback to Arial");
        }
        var go = new GameObject("VersionLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.layer = 5;
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(GetComponent<RectTransform>(), false);
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(0, 0);
        rt.pivot = new Vector2(0, 0);
        rt.anchoredPosition = new Vector2(16, 12);
        rt.sizeDelta = new Vector2(300, 30);
        var tx = go.GetComponent<Text>();
        tx.font = font;
        tx.text = GAME_VERSION;
        tx.fontSize = 16;
        tx.alignment = TextAnchor.MiddleLeft;
        tx.color = new Color(0.5f, 0.55f, 0.65f, 0.85f);
        tx.raycastTarget = false;
        Debug.Log("[MainMenu] BuildVersionLabel done, font=" + (font != null ? font.name : "NULL"));
    }

    
    /// <summary>全屏背景图（主菜单）</summary>
    private void BuildBackground()
    {
        // 禁用场景中已有的纯色 Background（避免覆盖背景图）
        Transform existingBg = transform.Find("Background");
        if (existingBg != null)
        {
            existingBg.gameObject.SetActive(false);
            Debug.Log("[MainMenu] Disabled existing solid-color Background");
        }

        Texture2D tex = Resources.Load<Texture2D>("MainMenuBg");
        if (tex == null)
        {
            Debug.LogWarning("[MainMenu] MainMenuBg texture not found in Resources");
            return;
        }
        var bg = new GameObject("MainMenuBg", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.RawImage));
        bg.layer = 5;
        var rt = bg.GetComponent<RectTransform>();
        rt.SetParent(GetComponent<RectTransform>(), false);
        rt.SetAsFirstSibling();  // 渲染在最底层
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var rawImg = bg.GetComponent<UnityEngine.UI.RawImage>();
        rawImg.texture = tex;
        rawImg.raycastTarget = false;
        Debug.Log("[MainMenu] Background set from MainMenuBg");
    }

    private void AutoBindUI()
    {
        Transform c = transform;

        btnNewGame   = c.Find("Btn_NewGame").GetComponent<Button>();
        btnContinue  = c.Find("Btn_Continue").GetComponent<Button>();
        btnSettings  = c.Find("Btn_Settings").GetComponent<Button>();
        btnQuit      = c.Find("Btn_Quit").GetComponent<Button>();

        btnNewGame.onClick.AddListener(OnNewGame);
        btnContinue.onClick.AddListener(OnContinue);
        btnSettings.onClick.AddListener(ShowSettings);
        Debug.Log("[MainMenu] btnSettings listener added, interactable=" + btnSettings.interactable);
        btnQuit.onClick.AddListener(ShowQuitConfirm);

        // 设置面板
        settingsPanel = c.Find("SettingsPanel").gameObject;
        settingsPanelRT = settingsPanel.GetComponent<RectTransform>();
        btnCloseSettings = settingsPanel.transform.Find("Panel/Btn_Close").GetComponent<Button>();
        btnCloseSettings.onClick.AddListener(HideSettings);

        // 退出确认对话框
        quitConfirmPanel = c.Find("QuitConfirmPanel").gameObject;
        Transform qp = quitConfirmPanel.transform;
        btnQuitConfirm = qp.Find("Panel/Btn_Confirm").GetComponent<Button>();
        btnQuitCancel  = qp.Find("Panel/Btn_Cancel").GetComponent<Button>();
        btnQuitConfirm.onClick.AddListener(ConfirmQuit);
        btnQuitCancel.onClick.AddListener(CancelQuit);
    }

    /// <summary>动态创建设置面板中的输入控件（避开 YAML GUID 问题）</summary>
    private Resolution[] resolutions;
    private Dropdown dropdownResolution, dropdownQuality;
    private Toggle toggleFullscreen;
    private Text txtInfoName, txtInfoChips, txtInfoRounds, txtInfoLevel;

    // 预设分辨率列表
    private readonly (int w, int h)[] resOptions = {
        (1920, 1080), (1600, 900), (1280, 720)
    };

    private void BuildSettingsControls()
    {
        RectTransform panel = (RectTransform)settingsPanel.transform.Find("Panel");
        float y = -100f;
        const float spacing = 40f;
        const float width = 420f;
        const float height = 32f;

        // ── 玩家名 InputField ─────────────────────
        inputPlayerName = CreateInputField("Input_PlayerName", panel, y, width, height);
        y -= spacing + 6f;

        // ── 音效 Slider ────────────────────────────
        sliderSoundVolume = CreateSlider("Slider_Sound", panel, y, width);
        y -= spacing + 6f;

        // ── 音乐 Slider ────────────────────────────
        sliderMusicVolume = CreateSlider("Slider_Music", panel, y, width);
        y -= spacing + 6f;

        // ── 分辨率 Dropdown ────────────────────────
        dropdownResolution = CreateResolutionDropdown("Dropdown_Resolution", panel, y, width);
        y -= spacing + 6f;

        // ── 全屏 Toggle ────────────────────────────
        toggleFullscreen = CreateToggle("Toggle_Fullscreen", panel, y, width);
        y -= spacing + 6f;

        // ── 画质 Dropdown ──────────────────────────
        dropdownQuality = CreateQualityDropdown("Dropdown_Quality", panel, y, width);
        y -= spacing + 6f;


    }

    /// <summary>创建 InputField（带标签和占位符）</summary>
    private InputField CreateInputField(string name, RectTransform parent, float y, float w, float h)
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // 标签
        var lbl = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        lbl.layer = 5;
        var lblRt = lbl.GetComponent<RectTransform>();
        lblRt.SetParent(parent, false);
        AnchorLeftTop(lblRt, y, 85, h, 5);
        var lblTx = lbl.GetComponent<Text>();
        lblTx.font = font; lblTx.text = "玩家名称:";
        lblTx.alignment = TextAnchor.MiddleRight; lblTx.fontSize = 20;
        lblTx.color = new Color(0.9f, 0.9f, 0.95f);

        // 输入框背景
        var bg = new GameObject("Bg", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bg.layer = 5;
        var bgRt = bg.GetComponent<RectTransform>();
        bgRt.SetParent(parent, false);
        AnchorLeftTop(bgRt, y, w - 130, h, 100);
        bg.GetComponent<Image>().color = new Color(0.82f, 0.82f, 0.84f);

        // 输入框
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(InputField));
        go.layer = 5;
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(bgRt, false);
        StretchFull(rt, 4);
        var ifd = go.GetComponent<InputField>();

        // Text
        var txtGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        txtGo.layer = 5;
        var txtRt = txtGo.GetComponent<RectTransform>();
        txtRt.SetParent(rt, false);
        AnchorStretch(txtRt, 8, 8, 4, 4);
        var txt = txtGo.GetComponent<Text>();
        txt.font = font; txt.fontSize = 18; txt.color = new Color(0.1f, 0.2f, 0.5f); txt.alignment = TextAnchor.MiddleLeft;
        txt.supportRichText = false;
        ifd.textComponent = txt;

        // Placeholder
        var phGo = new GameObject("Placeholder", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        phGo.layer = 5;
        var phRt = phGo.GetComponent<RectTransform>();
        phRt.SetParent(rt, false);
        AnchorStretch(phRt, 8, 8, 4, 4);
        var ph = phGo.GetComponent<Text>();
        ph.font = font; ph.fontSize = 18; ph.color = new Color(0.5f, 0.5f, 0.55f); ph.alignment = TextAnchor.MiddleLeft;
        ph.text = "输入玩家名称";
        ifd.placeholder = ph;

        ifd.onValueChanged.AddListener(OnPlayerNameChanged);
        return ifd;
    }

    /// <summary>创建带标签的 Slider</summary>
    private Slider CreateSlider(string name, RectTransform parent, float y, float w)
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        string displayName = name.Contains("Sound") ? "音效:" : "音乐:";

        // 标签
        var lbl = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        lbl.layer = 5;
        var lblRt = lbl.GetComponent<RectTransform>();
        lblRt.SetParent(parent, false);
        AnchorLeftTop(lblRt, y, 85, 32, 5);
        lbl.GetComponent<Text>().font = font;
        lbl.GetComponent<Text>().text = displayName;
        lbl.GetComponent<Text>().fontSize = 18;
        lbl.GetComponent<Text>().alignment = TextAnchor.MiddleRight;
        lbl.GetComponent<Text>().color = new Color(0.9f, 0.9f, 0.95f);

        // Slider 容器
        var bg = new GameObject(name, typeof(RectTransform));
        bg.layer = 5;
        var bgRt = bg.GetComponent<RectTransform>();
        bgRt.SetParent(parent, false);
        AnchorLeftTop(bgRt, y, w - 130, 32, 100);

        var sg = new GameObject("HandleSlideArea", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Slider));
        sg.layer = 5;
        sg.GetComponent<RectTransform>().SetParent(bgRt, false);
        StretchFull(sg.GetComponent<RectTransform>(), 0);
        sg.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.18f);
        var sl = sg.GetComponent<Slider>();
        sl.minValue = 0; sl.maxValue = 1;

        var fa = new GameObject("FillArea", typeof(RectTransform));
        fa.layer = 5; fa.GetComponent<RectTransform>().SetParent(sg.GetComponent<RectTransform>(), false);
        StretchFull(fa.GetComponent<RectTransform>(), 0);
        var fill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fill.layer = 5; fill.GetComponent<RectTransform>().SetParent(fa.GetComponent<RectTransform>(), false);
        StretchFull(fill.GetComponent<RectTransform>(), 0);
        fill.GetComponent<Image>().color = new Color(0.2f, 0.55f, 0.3f);
        sl.fillRect = fill.GetComponent<RectTransform>();

        var ha = new GameObject("HandleArea", typeof(RectTransform));
        ha.layer = 5; ha.GetComponent<RectTransform>().SetParent(sg.GetComponent<RectTransform>(), false);
        StretchFull(ha.GetComponent<RectTransform>(), 0);
        var handle = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        handle.layer = 5;
        var hRt = handle.GetComponent<RectTransform>();
        hRt.SetParent(ha.GetComponent<RectTransform>(), false);
        hRt.anchorMin = hRt.anchorMax = new Vector2(0.5f, 0.5f);
        hRt.sizeDelta = new Vector2(20, 28);
        handle.GetComponent<Image>().color = Color.white;
        sl.handleRect = hRt;

        // 值文本
        var valTxt = new GameObject("ValueText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        valTxt.layer = 5;
        valTxt.GetComponent<RectTransform>().SetParent(bgRt, false);
        var vRt = valTxt.GetComponent<RectTransform>();
        vRt.anchorMin = new Vector2(1, 0.5f); vRt.anchorMax = new Vector2(1, 0.5f);
        vRt.anchorMin = new Vector2(1, 0.5f); vRt.anchorMax = new Vector2(1, 0.5f);
        vRt.anchoredPosition = new Vector2(40, 0); vRt.sizeDelta = new Vector2(50, 30);
        var vTx = valTxt.GetComponent<Text>();
        vTx.font = font; vTx.fontSize = 16; vTx.alignment = TextAnchor.MiddleCenter;
        vTx.color = new Color(0.9f, 0.85f, 0.6f);

        sl.onValueChanged.AddListener(v => vTx.text = Mathf.RoundToInt(v * 100).ToString());

        if (name.Contains("Sound"))
            sl.onValueChanged.AddListener(v => GameSettings.SoundVolume = v);
        else
            sl.onValueChanged.AddListener(v => GameSettings.MusicVolume = v);

        return sl;
    }

    /// <summary>创建带标签的 Dropdown</summary>
    private Dropdown CreateDropdown(string name, RectTransform parent, float y, float w)
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        string displayName = name switch
        {
            "Dropdown_Resolution" => "分辨率:",
            "Dropdown_Quality" => "画质:",
            _ => ""
        };

        var lbl = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        lbl.layer = 5;
        var lblRt = lbl.GetComponent<RectTransform>();
        lblRt.SetParent(parent, false);
        AnchorLeftTop(lblRt, y, 85, 32, 5);
        lbl.GetComponent<Text>().font = font;
        lbl.GetComponent<Text>().text = displayName;
        lbl.GetComponent<Text>().fontSize = 18;
        lbl.GetComponent<Text>().alignment = TextAnchor.MiddleRight;
        lbl.GetComponent<Text>().color = new Color(0.9f, 0.9f, 0.95f);

        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Dropdown));
        go.layer = 5;
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        AnchorLeftTop(rt, y, w - 130, 32, 100);
        go.GetComponent<Image>().color = new Color(0.12f, 0.14f, 0.18f);
        var dd = go.GetComponent<Dropdown>();

        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        labelGo.layer = 5;
        labelGo.GetComponent<RectTransform>().SetParent(rt, false);
        AnchorStretch(labelGo.GetComponent<RectTransform>(), 10, 30, 4, 4);
        dd.captionText = labelGo.GetComponent<Text>();
        dd.captionText.font = font; dd.captionText.fontSize = 18; dd.captionText.color = Color.white;

        // itemTxt reserved as template; itemText set via itemLbl below

        var tmpl = new GameObject("Template", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
        tmpl.layer = 5;
        var tRt = tmpl.GetComponent<RectTransform>(); tRt.SetParent(rt, false);
        tRt.anchorMin = new Vector2(0, 0); tRt.anchorMax = new Vector2(1, 0);
        tRt.pivot = new Vector2(0.5f, 1); tRt.anchoredPosition = new Vector2(0, -4);
        tRt.sizeDelta = new Vector2(0, 160);
        tmpl.GetComponent<Image>().color = new Color(0.12f, 0.14f, 0.18f);
        tmpl.SetActive(false);
        dd.template = tmpl.GetComponent<RectTransform>();

        var vp = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
        vp.layer = 5; vp.GetComponent<RectTransform>().SetParent(tRt, false);
        AnchorStretch(vp.GetComponent<RectTransform>(), 0, 0, 0, 0);
        vp.GetComponent<Image>().color = Color.clear;

        var cont = new GameObject("Content", typeof(RectTransform));
        cont.layer = 5; cont.GetComponent<RectTransform>().SetParent(vp.GetComponent<RectTransform>(), false);
        var cRt = cont.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0, 1); cRt.anchorMax = new Vector2(1, 1);
        cRt.pivot = new Vector2(0.5f, 1); cRt.sizeDelta = new Vector2(0, 28);

        var item = new GameObject("Item", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Toggle));
        item.layer = 5; item.GetComponent<RectTransform>().SetParent(cRt, false);
        var iRt = item.GetComponent<RectTransform>();
        iRt.anchorMin = new Vector2(0, 0.5f); iRt.anchorMax = new Vector2(1, 0.5f);
        iRt.sizeDelta = new Vector2(0, 28);
        item.GetComponent<Image>().color = new Color(0.15f, 0.17f, 0.22f);
        var toggle = item.GetComponent<Toggle>();

        var itemLbl = new GameObject("ItemLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        itemLbl.layer = 5; itemLbl.GetComponent<RectTransform>().SetParent(iRt, false);
        AnchorStretch(itemLbl.GetComponent<RectTransform>(), 10, 10, 4, 4);
        dd.itemText = itemLbl.GetComponent<Text>();
        dd.itemText.font = font; dd.itemText.fontSize = 18; dd.itemText.color = Color.white;

        var check = new GameObject("Checkmark", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        check.layer = 5; toggle.graphic = check.GetComponent<Image>();
        toggle.graphic.GetComponent<RectTransform>().SetParent(iRt, false);
        AnchorStretch(toggle.graphic.GetComponent<RectTransform>(), 10, 10, 4, 4);
        toggle.graphic.color = new Color(0.2f, 0.55f, 0.3f);

        // 填充默认选项（由子类覆盖）
        dd.AddOptions(new List<string> { " " });

        return dd;
    }

    // ═══════════════════════════════════════════
    //  RectTransform 辅助
    // ═══════════════════════════════════════════

    

    /// <summary>创建分辨率下拉框</summary>
    private Dropdown CreateResolutionDropdown(string name, RectTransform parent, float y, float w)
    {
        var dd = CreateDropdown(name, parent, y, w);
        var labels = new System.Collections.Generic.List<string>();
        foreach (var r in resOptions) labels.Add(r.w + "x" + r.h);
        dd.ClearOptions();
        dd.AddOptions(labels);
        dd.onValueChanged.AddListener(OnResolutionChanged);
        return dd;
    }

    /// <summary>创建画质下拉框</summary>
    private Dropdown CreateQualityDropdown(string name, RectTransform parent, float y, float w)
    {
        var dd = CreateDropdown(name, parent, y, w);
        var labels = new System.Collections.Generic.List<string> { "低", "中", "高" };
        dd.ClearOptions();
        dd.AddOptions(labels);
        dd.onValueChanged.AddListener(OnQualityChanged);
        return dd;
    }

    /// <summary>创建全屏开关</summary>
    private Toggle CreateToggle(string name, RectTransform parent, float y, float w)
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var lbl = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        lbl.layer = 5;
        var lblRt = lbl.GetComponent<RectTransform>();
        lblRt.SetParent(parent, false);
        AnchorLeftTop(lblRt, y, 85, 32, 5);
        lbl.GetComponent<Text>().font = font;
        lbl.GetComponent<Text>().text = "全屏:";
        lbl.GetComponent<Text>().fontSize = 18;
        lbl.GetComponent<Text>().alignment = TextAnchor.MiddleRight;
        lbl.GetComponent<Text>().color = new Color(0.9f, 0.9f, 0.95f);

        var go = new GameObject(name, typeof(RectTransform), typeof(Toggle));
        go.layer = 5;
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        AnchorLeftTop(rt, y, 24, 24, 100);

        var bg = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bg.layer = 5;
        bg.GetComponent<RectTransform>().SetParent(rt, false);
        StretchFull(bg.GetComponent<RectTransform>(), 0);
        bg.GetComponent<Image>().color = new Color(0.82f, 0.82f, 0.84f);

        var check = new GameObject("Checkmark", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        check.layer = 5;
        check.GetComponent<RectTransform>().SetParent(bg.GetComponent<RectTransform>(), false);
        StretchFull(check.GetComponent<RectTransform>(), 3);
        check.GetComponent<Image>().color = new Color(0.2f, 0.55f, 0.3f);

        var toggle = go.GetComponent<Toggle>();
        toggle.graphic = check.GetComponent<Image>();
        toggle.targetGraphic = bg.GetComponent<Image>();
        toggle.isOn = GameSettings.IsFullscreen;
        toggle.onValueChanged.AddListener(OnFullscreenChanged);
        return toggle;
    }


    private void AnchorLeftTop(RectTransform rt, float y, float w, float h, float x)
    {
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 0.5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
    }

    private void AnchorCenterTop(RectTransform rt, float y, float w, float h)
    {
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0, y);
        rt.sizeDelta = new Vector2(w, h);
    }

    private void StretchFull(RectTransform rt, float margin)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(margin * 2, margin * 2);
    }

    private void AnchorStretch(RectTransform rt, float left, float right, float top, float bottom)
    {
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 1);
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }

    // ═══════════════════════════════════════════
    //  主菜单按钮
    // ═══════════════════════════════════════════

    public void OnNewGame()
    {
        // chips persist across new games; only reset on bankruptcy
        GameSettings.Save();
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnContinue()
    {
        GameSettings.Save();
        SceneManager.LoadScene(tableSceneName);
    }

    // ═══════════════════════════════════════════
    //  设置面板
    // ═══════════════════════════════════════════

    private void RefreshPlayerInfo()
    {
        if (txtInfoName != null) txtInfoName.text = "【玩家】" + GameSettings.PlayerName;
        if (txtInfoChips != null) txtInfoChips.text = "资金: \u00a5" + GameSettings.EffectiveChips.ToString("N0");
        if (txtInfoLevel != null)
        {
            int lv = GameSettings.Level;
            int xpCur = GameSettings.XPInCurrentLevel;
            int xpNext = GameSettings.XPForNextLevel - ((lv - 1) * lv / 2 * 1000);
            txtInfoLevel.text = string.Format("Lv.{0} ({1}/{2})", lv, xpCur, xpNext);
        }
        if (txtInfoRounds != null) txtInfoRounds.text = "已玩局数: " + GameSettings.RoundsPlayed;
    }

    public void ShowSettings()
    {
        Debug.Log("[MainMenu] ShowSettings called");
        LoadSettingsToUI();
        settingsPanel.SetActive(true);
    }

    public void HideSettings()
    {
        GameSettings.Save();
        ApplyDisplaySettings();
        RefreshPlayerInfo();
        settingsPanel.SetActive(false);
    }

    private void LoadSettingsToUI()
    {
        inputPlayerName.text = GameSettings.PlayerName;
        sliderSoundVolume.value = GameSettings.SoundVolume;
        sliderMusicVolume.value = GameSettings.MusicVolume;

        // 分辨率
        int resIdx = 0; // default 1920x1080
        for (int i = 0; i < resOptions.Length; i++)
            if (resOptions[i].w == GameSettings.ResolutionWidth && resOptions[i].h == GameSettings.ResolutionHeight)
                { resIdx = i; break; }
        if (dropdownResolution != null) dropdownResolution.value = resIdx;
        if (toggleFullscreen != null) toggleFullscreen.isOn = GameSettings.IsFullscreen;
        if (dropdownQuality != null) dropdownQuality.value = (int)GameSettings.GraphicsQuality;


    }

    public void OnPlayerNameChanged(string name)
    {
        GameSettings.PlayerName = string.IsNullOrWhiteSpace(name) ? "Player" : name;
    }

    // ═══════════════════════════════════════════
    //  退出确认
    // ═══════════════════════════════════════════

    public void OnResolutionChanged(int index)
    {
        if (index >= 0 && index < resOptions.Length)
        {
            GameSettings.ResolutionWidth = resOptions[index].w;
            GameSettings.ResolutionHeight = resOptions[index].h;
        }
    }

    public void OnFullscreenChanged(bool isFull)
    {
        GameSettings.IsFullscreen = isFull;
    }

    public void OnQualityChanged(int index)
    {
        GameSettings.GraphicsQuality = (GameSettings.QualityLevel)index;
    }

    /// <summary>应用设置（分辨率、全屏、画质）</summary>
    public void ApplyDisplaySettings()
    {
        Screen.SetResolution(GameSettings.ResolutionWidth, GameSettings.ResolutionHeight, GameSettings.IsFullscreen);
        QualitySettings.SetQualityLevel((int)GameSettings.GraphicsQuality, true);
    }

    public void ShowQuitConfirm() => quitConfirmPanel.SetActive(true);
    public void CancelQuit() => quitConfirmPanel.SetActive(false);

    public void ConfirmQuit()
    {
        GameSettings.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
