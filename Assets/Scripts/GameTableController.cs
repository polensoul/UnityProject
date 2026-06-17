using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class GameTableController : MonoBehaviour
{
    public GameManager gm;
    private Button btnBack, btnNextHand, btnRestart;

    // 动态 UI
    private RectTransform communityRow, playerRow, actionRow;
    private Text txtPot, txtPhase, txtLog, txtPotCommunity, scrollLogText;
    private Button btnFold, btnCheckCall, btnRaise, btnAllIn;
    private Slider raiseSlider;
    private Text txtRaiseAmount;
    private List<CardSlot> communitySlots = new(5);
    private List<PlayerPanel> playerPanels = new();

    private bool waitingForHuman = false;
    private bool handEnded;
    private int handStartChips;
    private GameObject backConfirmDialog;
    private List<string> actionHistory = new List<string>();
    private string lastLoggedAction = ""; // confirm dialog root // ?????????????

    private void Awake()
    {
        Debug.Log("[Table] Awake start");
        EnsureCanvasComponents();
        EnsureEventSystem();
        BindBaseUI();
        Debug.Log("[Table] BaseUI bound");
        BuildGameUI();
        Debug.Log("[Table] GameUI built");
    }

    private void Start()
    {
        Debug.Log("[Table] Start - init game");
        gm = new GameManager();
        // Read venue config
        var cfg = GameSettings.GetVenueConfig(GameSettings.SelectedVenue);
        int humanChips = GameSettings.EffectiveChips;

        // Determine total player count
        var rng = new System.Random();
        int totalPlayers = cfg.MinPlayers == cfg.MaxPlayers
            ? cfg.MinPlayers
            : rng.Next(cfg.MinPlayers, cfg.MaxPlayers + 1);

        // Build player list
        var players = new List<Player>
        {
            new Player(GameSettings.PlayerName, humanChips, true)
        };

        // Determine which AI slots get Hard difficulty
        int hardCount = cfg.MinHardAI;
        int aiCount = totalPlayers - 1;
        var aiDifficulties = new GameSettings.AIDifficulty[aiCount];
        for (int i = 0; i < aiCount; i++)
            aiDifficulties[i] = i < hardCount
                ? GameSettings.AIDifficulty.Hard
                : (rng.Next(2) == 0 ? GameSettings.AIDifficulty.Medium : GameSettings.AIDifficulty.Easy);
        // Shuffle difficulty assignments
        for (int i = aiCount - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            var tmp = aiDifficulties[i];
            aiDifficulties[i] = aiDifficulties[j];
            aiDifficulties[j] = tmp;
        }

        for (int i = 0; i < aiCount; i++)
        {
            int aiChips = rng.Next(cfg.AiChipsMin, cfg.AiChipsMax + 1);
            players.Add(new AIPlayer(string.Format("AI-{0}", i + 1), aiChips, aiDifficulties[i]));
        }

        gm.InitializeGame(players, GameSettings.StartChips, cfg.SmallBlind);
        gm.MaxRaiseCap = cfg.MaxRaise;
        gm.MaxAllInCap = cfg.MaxAllIn;
        gm.EliminateBrokePlayers();
        StartNewHand();
    }

    // ═══════════════════════════════════════════
    //  基础设置
    // ═══════════════════════════════════════════

    private void EnsureCanvasComponents()
    {
        if (!GetComponent<CanvasScaler>())
        {
            var s = gameObject.AddComponent<CanvasScaler>();
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = new Vector2(1920, 1080);
            s.matchWidthOrHeight = 0.5f;
        }
        if (!GetComponent<GraphicRaycaster>()) gameObject.AddComponent<GraphicRaycaster>();
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }
    }

    private void BindBaseUI()
    {
        btnBack = transform.Find("Btn_Back").GetComponent<Button>();
        btnBack.onClick.AddListener(ShowBackConfirm);
    }

    // ═══════════════════════════════════════════
    //  构建游戏 UI
    // ═══════════════════════════════════════════

    private void BuildGameUI()
    {
        var c = GetComponent<RectTransform>();

        // 信息栏（顶部）
        var infoBar = MakeRect("InfoBar", c, 0, 1, -40, 0, 900, 40);
        txtPot = MakeText("Pot", infoBar, "底池: ￥0", 24, TextAnchor.MiddleLeft, Color.white);
        txtPot.rectTransform.anchorMin = new Vector2(0, 0.5f);
        txtPot.rectTransform.anchorMax = new Vector2(0, 0.5f);
        txtPot.rectTransform.anchoredPosition = new Vector2(20, 0);
        txtPot.rectTransform.sizeDelta = new Vector2(300, 36);

        txtPhase = MakeText("Phase", infoBar, "等待开局...", 22, TextAnchor.MiddleCenter, new Color(0.9f, 0.85f, 0.6f));
        txtPhase.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        txtPhase.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        txtPhase.rectTransform.sizeDelta = new Vector2(300, 36);

        // 公共牌行
        communityRow = MakeRect("CommunityRow", c, 0.5f, 0.55f, 0, 0, 700, 120);

        for (int i = 0; i < 5; i++)
        {
            var slot = MakeCardSlot($"Community_{i}", communityRow, (float)(i - 2));
            communitySlots.Add(slot);
        }

        // Pot display above community cards
        txtPotCommunity = MakeText("PotCommunity", c, "底池: ￥0", 28, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.2f));
        txtPotCommunity.fontStyle = FontStyle.Bold;
        txtPotCommunity.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        txtPotCommunity.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        txtPotCommunity.rectTransform.anchoredPosition = new Vector2(260, 15);
        txtPotCommunity.rectTransform.sizeDelta = new Vector2(400, 40);

        // 玩家面板动态布局
        var playerRowGo = new GameObject("PlayerRow", typeof(RectTransform));
        playerRowGo.layer = 5;
        playerRow = playerRowGo.GetComponent<RectTransform>();
        playerRow.SetParent(c, false);
        playerRow.anchorMin = Vector2.zero;
        playerRow.anchorMax = Vector2.one;
        playerRow.sizeDelta = Vector2.zero;

        // 操作行（底部）
        
        // Action log panel (bottom-left, scrollable)
        var logPanel = new GameObject("ActionLogPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        logPanel.layer = 5;
        var logRt = logPanel.GetComponent<RectTransform>();
        logRt.SetParent(c, false);
        logRt.anchorMin = new Vector2(0, 0);
        logRt.anchorMax = new Vector2(0, 0);
        logRt.pivot = new Vector2(0, 0);
        logRt.anchoredPosition = new Vector2(16, 16);
        logRt.sizeDelta = new Vector2(300, 300);
        logPanel.GetComponent<Image>().color = new Color(0.05f, 0.06f, 0.08f, 0.75f);

        // Simple Text for action log (no ScrollRect complexity)
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        scrollLogText = MakeText("LogText", logRt, "", 14, TextAnchor.UpperLeft, new Color(0.85f, 0.88f, 0.92f));
        scrollLogText.rectTransform.anchorMin = new Vector2(0, 0);
        scrollLogText.rectTransform.anchorMax = new Vector2(1, 1);
        scrollLogText.rectTransform.offsetMin = new Vector2(8, 4);
        scrollLogText.rectTransform.offsetMax = new Vector2(-8, -4);
        scrollLogText.horizontalOverflow = HorizontalWrapMode.Wrap;
        scrollLogText.verticalOverflow = VerticalWrapMode.Truncate;
actionRow = MakeRect("ActionRow", c, 0.5f, 0.11f, 0, 0, 800, 100);

        // Fold
        btnFold = MakeButton("BtnFold", actionRow, "弃牌", new Color(0.5f, 0.15f, 0.15f));
        btnFold.GetComponent<RectTransform>().anchoredPosition = new Vector2(-320, 0);

        // Check / Call
        btnCheckCall = MakeButton("BtnCheckCall", actionRow, "过牌", new Color(0.15f, 0.4f, 0.5f));
        btnCheckCall.GetComponent<RectTransform>().anchoredPosition = new Vector2(-120, 0);

        // Raise 按钮
        btnRaise = MakeButton("BtnRaise", actionRow, "加注", new Color(0.4f, 0.25f, 0.1f));
        btnRaise.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);

        // 加注金额滑块（水平置于 Raise 和 AllIn 之间）
        var sliderArea = new GameObject("SliderArea", typeof(RectTransform));
        sliderArea.layer = 5;
        var sliderAreaRt = sliderArea.GetComponent<RectTransform>();
        sliderAreaRt.SetParent(actionRow, false);
        sliderAreaRt.anchorMin = new Vector2(0.5f, 0.5f);
        sliderAreaRt.anchorMax = new Vector2(0.5f, 0.5f);
        sliderAreaRt.anchoredPosition = new Vector2(120, 0);
        sliderAreaRt.sizeDelta = new Vector2(120, 36);

        var sliderBg = new GameObject("SliderBg", typeof(RectTransform), typeof(Image));
        sliderBg.layer = 5;
        sliderBg.GetComponent<RectTransform>().SetParent(sliderAreaRt, false);
        sliderBg.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0.1f);
        sliderBg.GetComponent<RectTransform>().anchorMax = new Vector2(1, 0.9f);
        sliderBg.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        sliderBg.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.18f);

        var sg = new GameObject("RaiseSlider", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Slider));
        sg.layer = 5;
        var sgRt = sg.GetComponent<RectTransform>();
        sgRt.SetParent(sliderBg.GetComponent<RectTransform>(), false);
        sgRt.anchorMin = Vector2.zero;
        sgRt.anchorMax = Vector2.one;
        sgRt.sizeDelta = Vector2.zero;
        sg.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
        raiseSlider = sg.GetComponent<Slider>();
        raiseSlider.minValue = 0;
        raiseSlider.maxValue = 1;
        raiseSlider.wholeNumbers = false;
        raiseSlider.direction = Slider.Direction.LeftToRight;

        var fillArea = new GameObject("FillArea", typeof(RectTransform));
        fillArea.layer = 5;
        fillArea.GetComponent<RectTransform>().SetParent(sgRt, false);
        fillArea.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0.15f);
        fillArea.GetComponent<RectTransform>().anchorMax = new Vector2(1, 0.85f);
        fillArea.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        var fillImg = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillImg.layer = 5;
        fillImg.GetComponent<RectTransform>().SetParent(fillArea.GetComponent<RectTransform>(), false);
        fillImg.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        fillImg.GetComponent<RectTransform>().anchorMax = new Vector2(1, 1);
        fillImg.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        fillImg.GetComponent<Image>().color = new Color(0.2f, 0.55f, 0.3f);
        raiseSlider.fillRect = fillImg.GetComponent<RectTransform>();

        var handleArea = new GameObject("HandleArea", typeof(RectTransform));
        handleArea.layer = 5;
        handleArea.GetComponent<RectTransform>().SetParent(sgRt, false);
        handleArea.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        handleArea.GetComponent<RectTransform>().anchorMax = Vector2.one;
        handleArea.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        var hImg = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        hImg.layer = 5;
        hImg.GetComponent<RectTransform>().SetParent(handleArea.GetComponent<RectTransform>(), false);
        hImg.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.5f);
        hImg.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
        hImg.GetComponent<RectTransform>().sizeDelta = new Vector2(18, 32);
        hImg.GetComponent<Image>().color = Color.white;
        raiseSlider.handleRect = hImg.GetComponent<RectTransform>();

        // 加注金额标签
        txtRaiseAmount = MakeText("RaiseAmount", sliderAreaRt, "￥0", 16, TextAnchor.MiddleLeft, new Color(0.9f, 0.85f, 0.6f));
        txtRaiseAmount.rectTransform.anchorMin = new Vector2(1, 0.5f);
        txtRaiseAmount.rectTransform.anchorMax = new Vector2(1, 0.5f);
        txtRaiseAmount.rectTransform.anchoredPosition = new Vector2(50, 0);
        txtRaiseAmount.rectTransform.sizeDelta = new Vector2(90, 30);

        // All In
        btnAllIn = MakeButton("BtnAllIn", actionRow, "All In", new Color(0.5f, 0.1f, 0.1f));
        btnAllIn.GetComponent<RectTransform>().anchoredPosition = new Vector2(320, 0);

        // 动作按钮绑定
        btnFold.onClick.AddListener(() => DoAction(PlayerAction.Fold));
        btnCheckCall.onClick.AddListener(() => { var acts = gm.GetAvailableActions(); if (acts.Contains(PlayerAction.Call)) DoAction(PlayerAction.Call); else DoAction(PlayerAction.Check); });
        btnRaise.onClick.AddListener(() => { var p = gm.GetCurrentPlayer(); int minR = System.Math.Min(gm.MinRaise, p.Chips); int maxR = System.Math.Min(p.Chips, gm.MaxRaiseCap); int val = minR + Mathf.RoundToInt(raiseSlider.value * (maxR - minR)); DoAction(PlayerAction.Raise, val); });
        btnAllIn.onClick.AddListener(() => DoAction(PlayerAction.AllIn));

        // 日志（底部提示）
        txtLog = MakeText("Log", c, "", 18, TextAnchor.MiddleCenter, new Color(0.8f, 0.8f, 0.9f));
        txtLog.rectTransform.anchorMin = new Vector2(0.5f, 0);
        txtLog.rectTransform.anchorMax = new Vector2(0.5f, 0);
        txtLog.rectTransform.anchoredPosition = new Vector2(0, 8);
        txtLog.rectTransform.sizeDelta = new Vector2(900, 26);
    }

    // ==========================================
    //  Game loop
    // ==========================================

    private void StartNewHand()
    {
        if (gm.IsGameOver())
        {
            ShowGameOver();
            return;
        }

        handEnded = false;
        actionHistory.Clear();
        lastLoggedAction = "";
        if (scrollLogText != null) scrollLogText.text = "";
        var human = gm.Players.Find(p => p.IsHuman);
        handStartChips = human != null ? human.Chips : 0;
        gm.StartHand();
        RebuildPlayerPanels();
        StartCoroutine(GameLoop());
    }

    private IEnumerator GameLoop()
    {
        while (gm.HandInProgress && !handEnded)
        {
            RefreshUI();

            if (gm.IsCurrentPlayerHuman())
            {
                waitingForHuman = true;
                UpdateActionButtons();
                yield return new WaitUntil(() => !waitingForHuman || handEnded);
            }
            else
            {
                yield return new WaitForSeconds(0.5f);
                gm.ExecuteAIAction();
                RefreshUI();
            }

            if (handEnded) break;
        }

        if (!gm.HandInProgress || handEnded)
        {
            RefreshUI(); // refresh one more time for showdown reveal
            if (gm.Phase == GamePhase.Showdown)
                ShowNextHandPopup();
        }
    }

    private void DoAction(PlayerAction action, int raiseAmount = 0)
    {
        gm.ExecuteHumanAction(action, raiseAmount);
        waitingForHuman = false;
    }

    private void UpdateActionButtons()
    {
        var actions = gm.GetAvailableActions();
        btnFold.gameObject.SetActive(actions.Contains(PlayerAction.Fold));
        btnAllIn.gameObject.SetActive(actions.Contains(PlayerAction.AllIn));
        btnRaise.gameObject.SetActive(actions.Contains(PlayerAction.Raise));

        bool canCheck = actions.Contains(PlayerAction.Check);
        bool canCall = actions.Contains(PlayerAction.Call);
        btnCheckCall.gameObject.SetActive(canCheck || canCall);
        btnCheckCall.GetComponentInChildren<Text>().text = canCall ? "跟注" : "过牌";

        if (actions.Contains(PlayerAction.Raise))
        {
            var player = gm.GetCurrentPlayer();
            int minRaise = System.Math.Min(gm.MinRaise, player.Chips);
            int maxRaise = System.Math.Min(player.Chips, gm.MaxRaiseCap);
            raiseSlider.gameObject.SetActive(true);
            raiseSlider.minValue = minRaise;
            raiseSlider.maxValue = maxRaise;
            raiseSlider.value = minRaise;
            txtRaiseAmount.text = minRaise.ToString();
            txtRaiseAmount.gameObject.SetActive(true);
        }
        else
        {
            raiseSlider.gameObject.SetActive(false);
            txtRaiseAmount.gameObject.SetActive(false);
        }
    }

    // ==========================================
    //  Hand end / popups
    // ==========================================

    private void ShowNextHandPopup()
    {
        btnNextHand = MakeButton("BtnNextHand", actionRow, "下一局", new Color(0.2f, 0.55f, 0.3f));
        btnNextHand.GetComponent<RectTransform>().anchoredPosition = new Vector2(-80, 0);
        btnNextHand.GetComponent<RectTransform>().sizeDelta = new Vector2(140, 50);
        btnNextHand.onClick.AddListener(OnNextHand);

        var btnBackMenu = MakeButton("BtnBackToMenu", actionRow, "返回主菜单", new Color(0.3f, 0.32f, 0.38f));
        btnBackMenu.GetComponent<RectTransform>().anchoredPosition = new Vector2(80, 0);
        btnBackMenu.GetComponent<RectTransform>().sizeDelta = new Vector2(140, 50);
        btnBackMenu.onClick.AddListener(OnBackToMenu);

        txtLog.text = gm.LastActionLog;
        txtPhase.text = "手牌结束";
    }

    public void OnNextHand()
    {
        ClearPopupButtons();
        var human = gm.Players.Find(p => p.IsHuman);
        if (human != null)
        {
            int profit = human.Chips - handStartChips;
            if (profit > 0)
                GameSettings.TotalExperience += profit;
        }
        GameSettings.RoundsPlayed++;
        GameSettings.Save();
        gm.EliminateBrokePlayers();
        StartNewHand();
    }

    public void OnBackToMenu()
    {
        var human = gm.Players.Find(p => p.IsHuman);
        if (human != null)
        {
            int profit = human.Chips - handStartChips;
            if (profit > 0)
                GameSettings.TotalExperience += profit;
            GameSettings.RoundsPlayed++;
            GameSettings.SavedChips = human.Chips;
            GameSettings.Save();
        }
        SceneManager.LoadScene("MainMenu");
    }

    private void ClearPopupButtons()
    {
        if (btnNextHand != null) { Destroy(btnNextHand.gameObject); btnNextHand = null; }
        var backBtn = actionRow.Find("BtnBackToMenu");
        if (backBtn != null) Destroy(backBtn.gameObject);
    }

    private void ShowGameOver()
    {
        btnFold.gameObject.SetActive(false);
        btnCheckCall.gameObject.SetActive(false);
        btnRaise.gameObject.SetActive(false);
        btnAllIn.gameObject.SetActive(false);
        raiseSlider.gameObject.SetActive(false);

        var human = gm.Players.Find(p => p.IsHuman);
        bool isBroke = human != null && human.Chips <= 0;

        if (isBroke)
        {
            txtPhase.text = "破产";
            txtLog.text = "您已破产！点击返回主菜单，资金将重置为1000";
            var btnBrokeBack = MakeButton("BtnBrokeBack", actionRow, "返回主菜单", new Color(0.6f, 0.2f, 0.2f));
            btnBrokeBack.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            btnBrokeBack.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 50);
            btnBrokeBack.onClick.AddListener(() =>
            {
                GameSettings.SavedChips = 1000;
                GameSettings.Save();
                SceneManager.LoadScene("MainMenu");
            });
        }
        else
        {
            var winner = gm.GetWinner();
            txtLog.text = winner != null ? "游戏结束！" + winner.Name + " 获胜！" : "Game Over!";
            txtPhase.text = "游戏结束";
            btnRestart = MakeButton("BtnRestart", actionRow, "重新开始", new Color(0.2f, 0.5f, 0.3f));
            btnRestart.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            btnRestart.onClick.AddListener(() => SceneManager.LoadScene("GameSetup"));
        }
    }

    /// <summary>Back confirm dialog</summary>
    private void ShowBackConfirm()
    {
        btnFold.gameObject.SetActive(false);
        btnCheckCall.gameObject.SetActive(false);
        btnRaise.gameObject.SetActive(false);
        btnAllIn.gameObject.SetActive(false);
        raiseSlider.gameObject.SetActive(false);

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var canvas = GetComponent<RectTransform>();

        var overlay = new GameObject("BackConfirmOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        overlay.layer = 5;
        var ovRt = overlay.GetComponent<RectTransform>();
        ovRt.SetParent(canvas, false);
        ovRt.anchorMin = Vector2.zero;
        ovRt.anchorMax = Vector2.one;
        ovRt.sizeDelta = Vector2.zero;
        overlay.GetComponent<Image>().color = new Color(0, 0, 0, 0.6f);
        backConfirmDialog = overlay;

        var panel = new GameObject("Dialog", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.layer = 5;
        var pRt = panel.GetComponent<RectTransform>();
        pRt.SetParent(ovRt, false);
        pRt.anchorMin = pRt.anchorMax = new Vector2(0.5f, 0.5f);
        pRt.sizeDelta = new Vector2(400, 220);
        pRt.anchoredPosition = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0.12f, 0.14f, 0.18f);

        var title = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        title.layer = 5;
        var tRt = title.GetComponent<RectTransform>();
        tRt.SetParent(pRt, false);
        tRt.anchorMin = new Vector2(0, 0.55f);
        tRt.anchorMax = new Vector2(1, 0.95f);
        tRt.sizeDelta = Vector2.zero;
        var tTx = title.GetComponent<Text>();
        tTx.font = font;
        tTx.text = "确认返回主菜单？";
        tTx.fontSize = 24;
        tTx.alignment = TextAnchor.MiddleCenter;
        tTx.color = new Color(0.9f, 0.9f, 0.95f);

        var sub = new GameObject("Subtitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        sub.layer = 5;
        var sRt = sub.GetComponent<RectTransform>();
        sRt.SetParent(pRt, false);
        sRt.anchorMin = new Vector2(0, 0.3f);
        sRt.anchorMax = new Vector2(1, 0.55f);
        sRt.sizeDelta = Vector2.zero;
        var sTx = sub.GetComponent<Text>();
        sTx.font = font;
        sTx.text = "资金将被保存，可通过继续游戏进入";
        sTx.fontSize = 16;
        sTx.alignment = TextAnchor.MiddleCenter;
        sTx.color = new Color(0.6f, 0.65f, 0.7f);

        var btnConfirm = new GameObject("BtnConfirm", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnConfirm.layer = 5;
        var cfRt = btnConfirm.GetComponent<RectTransform>();
        cfRt.SetParent(pRt, false);
        cfRt.anchorMin = new Vector2(0.25f, 0.05f);
        cfRt.anchorMax = new Vector2(0.48f, 0.25f);
        cfRt.sizeDelta = Vector2.zero;
        btnConfirm.GetComponent<Image>().color = new Color(0.6f, 0.2f, 0.2f);
        var cfBtn = btnConfirm.GetComponent<Button>();
        cfBtn.targetGraphic = btnConfirm.GetComponent<Image>();
        var cfLbl = MakeTextLabel("Label", cfRt, "确认", font, 20, Color.white);
        cfBtn.onClick.AddListener(() =>
        {
            var human = gm.Players.Find(p => p.IsHuman);
            if (human != null)
            {
                GameSettings.SavedChips = human.Chips;
                GameSettings.Save();
            }
            SceneManager.LoadScene("MainMenu");
        });

        var btnCancel = new GameObject("BtnCancel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnCancel.layer = 5;
        var clRt = btnCancel.GetComponent<RectTransform>();
        clRt.SetParent(pRt, false);
        clRt.anchorMin = new Vector2(0.52f, 0.05f);
        clRt.anchorMax = new Vector2(0.75f, 0.25f);
        clRt.sizeDelta = Vector2.zero;
        btnCancel.GetComponent<Image>().color = new Color(0.25f, 0.28f, 0.33f);
        var clBtn = btnCancel.GetComponent<Button>();
        clBtn.targetGraphic = btnCancel.GetComponent<Image>();
        var clLbl = MakeTextLabel("Label", clRt, "取消", font, 20, Color.white);
        clBtn.onClick.AddListener(() =>
        {
            Destroy(backConfirmDialog);
            backConfirmDialog = null;
            UpdateActionButtons();
            if (gm.HandInProgress)
                txtPhase.text = gm.Phase.ToString();
        });
    }

    private Text MakeTextLabel(string name, RectTransform parent, string text, Font font, int fontSize, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.layer = 5;
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        var tx = go.GetComponent<Text>();
        tx.font = font;
        tx.text = text;
        tx.fontSize = fontSize;
        tx.alignment = TextAnchor.MiddleCenter;
        tx.color = color;
        tx.raycastTarget = false;
        return tx;
    }

    // ==========================================
    //  Refresh UI
    // ==========================================

    private void RefreshUI()
    {
        txtPot.text = "底池: " + gm.Pot;
        txtPotCommunity.text = "底池: " + gm.Pot;
        txtPhase.text = gm.Phase switch
        {
            GamePhase.PreFlop => "翻牌前",
            GamePhase.Flop => "翻牌",
            GamePhase.Turn => "转牌",
            GamePhase.River => "河牌",
            GamePhase.Showdown => "摊牌",
            _ => "等待..."
        };

        // Consume all new log entries from GameManager
        while (gm.LogConsumedIndex < gm.ActionLogs.Count)
        {
            string entry = gm.ActionLogs[gm.LogConsumedIndex];
            gm.LogConsumedIndex++;
            actionHistory.Add(entry);
        }
        if (actionHistory.Count > 20) actionHistory.RemoveRange(0, actionHistory.Count - 20);
        if (scrollLogText != null) scrollLogText.text = string.Join("\n", actionHistory);

        if (!string.IsNullOrEmpty(gm.LastActionLog))
            txtLog.text = gm.LastActionLog;

        for (int i = 0; i < 5; i++)
        {
            if (i < gm.CommunityCards.Count)
                SetCardSlot(communitySlots[i], gm.CommunityCards[i]);
            else
                ClearCardSlot(communitySlots[i]);
        }

        foreach (var pp in playerPanels)
            pp.Refresh();
    }

    // ==========================================
    //  Player panels layout
    // ==========================================

    private static readonly Vector2[] AIRowPositions = new Vector2[]
    {
        new Vector2(0.18f, 0.82f), // 座位1：上方左1
        new Vector2(0.36f, 0.82f), // 座位2：上方左2
        new Vector2(0.56f, 0.82f), // 座位3：上方正中
        new Vector2(0.76f, 0.82f), // 座位4：上方右2
        new Vector2(0.94f, 0.82f), // 座位5：上方右1
        new Vector2(0.18f, 0.62f), // 座位6：左侧上
        new Vector2(0.18f, 0.40f), // 座位7：左侧下
        new Vector2(0.94f, 0.62f), // 座位8：右侧上
        new Vector2(0.94f, 0.40f), // 座位9：右侧下
    };

    private static readonly Vector2 HumanPosition = new Vector2(0.5f, 0.18f);

    private void RebuildPlayerPanels()
    {
        foreach (var p in playerPanels)
        {
            if (p.Root != null) Destroy(p.Root.gameObject);
        }
        playerPanels.Clear();

        if (gm.Players == null) return;

        int seatIndex = 0; // AI 座位分配索引

        for (int i = 0; i < gm.Players.Count; i++)
        {
            var player = gm.Players[i];
            bool isHuman = player.IsHuman;
            Vector2 pos;

            if (isHuman)
            {
                // 人类玩家固定在牌堆正下方居中
                pos = HumanPosition;
            }
            else
            {
                // AI 按座位序号 1-9 依次分配
                pos = AIRowPositions[seatIndex % AIRowPositions.Length];
                seatIndex++;
            }

            var panel = isHuman
                ? new PlayerPanel(playerRow, pos, player, new Vector2(427, 243), new Vector2(0, 120))
                : new PlayerPanel(playerRow, pos, player, new Vector2(290, 165), Vector2.zero);
            playerPanels.Add(panel);
        }
    }

    // ==========================================
    //  UI helpers
    // ==========================================

    private Button MakeButton(string name, RectTransform parent, string label, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.layer = 5;
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(100, 44);
        var img = go.GetComponent<Image>();
        img.color = color;
        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;

        var lbl = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        lbl.layer = 5;
        var lblRt = lbl.GetComponent<RectTransform>();
        lblRt.SetParent(rt, false);
        lblRt.anchorMin = Vector2.zero;
        lblRt.anchorMax = Vector2.one;
        lblRt.sizeDelta = Vector2.zero;
        var txt = lbl.GetComponent<Text>();
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.font = font;
        txt.text = label;
        txt.fontSize = 20;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.raycastTarget = false;
        return btn;
    }

    private RectTransform MakeRect(string name, RectTransform parent, float ax, float ay, float ox, float oy, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(ax, ay);
        rt.anchorMax = new Vector2(ax, ay);
        rt.anchoredPosition = new Vector2(ox, oy);
        rt.sizeDelta = new Vector2(w, h);
        return rt;
    }

    private Text MakeText(string name, RectTransform parent, string text, int size, TextAnchor align, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.layer = 5;
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        var tx = go.GetComponent<Text>();
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tx.font = font;
        tx.text = text;
        tx.fontSize = size;
        tx.alignment = align;
        tx.color = color;
        tx.raycastTarget = false;
        return tx;
    }

    private CardSlot MakeCardSlot(string name, RectTransform parent, float xOffset)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = 5;
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(xOffset * 90, 0);
        rt.sizeDelta = new Vector2(72, 104);
        var bg = go.GetComponent<Image>();
        bg.color = new Color(0.85f, 0.85f, 0.85f);

        var txtGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        txtGo.layer = 5;
        var txtRt = txtGo.GetComponent<RectTransform>();
        txtRt.SetParent(rt, false);
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;
        var label = txtGo.GetComponent<Text>();
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.font = font;
        label.text = "";
        label.fontSize = 22;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.black;
        label.raycastTarget = false;

        return new CardSlot { Root = go, Bg = bg, Label = label };
    }

    private void SetCardSlot(CardSlot slot, Card card)
    {
        if ((int)card.Rank >= 2)
        {
            slot.Label.text = card.ToString();
            bool red = card.Suit == Suit.Hearts || card.Suit == Suit.Diamonds;
            slot.Label.color = red ? new Color(1f, 0.3f, 0.3f) : Color.black;
            slot.Bg.color = Color.white;
        }
        else
        {
            slot.Label.text = "";
            slot.Bg.color = new Color(0.85f, 0.85f, 0.85f);
        }
    }

    

    // ==========================================
    //  PlayerPanel inner class
    // ==========================================

    

    
    private void ClearCardSlot(CardSlot slot)
    {
        slot.Label.text = "";
        slot.Bg.color = new Color(0.85f, 0.85f, 0.85f);
    }


    private class CardSlot
    {
        public GameObject Root;
        public Image Bg;
        public Text Label;
    }

    private class PlayerPanel
    {
        public Player Player;
        public GameObject Root;
        public Text NameText, ChipsText, BetText, StatusText;
        public Image PanelBg;
        public Image Card1Bg, Card2Bg;
        public Text Card1Label, Card2Label;

        public PlayerPanel(RectTransform parent, Vector2 anchor, Player player, Vector2 panelSize, Vector2 panelOffset)
        {
            Player = player;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Root = new GameObject("PP", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Root.layer = 5;
            var rt = Root.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = panelSize;
            rt.anchoredPosition = panelOffset;
            PanelBg = Root.GetComponent<Image>();
            PanelBg.color = new Color(0.08f, 0.1f, 0.14f, 0.9f);

            // --- Left info column (name, chips, bet, status) ---
            NameText = MkTx("Name", Root, player.Name, 16, TextAnchor.UpperLeft,
                player.IsHuman ? new Color(1f, 0.85f, 0.3f) : new Color(0.7f, 0.75f, 0.85f), font);
            NameText.rectTransform.anchorMin = new Vector2(0.03f, 0.55f);
            NameText.rectTransform.anchorMax = new Vector2(0.38f, 0.95f);
            NameText.rectTransform.sizeDelta = Vector2.zero;

            ChipsText = MkTx("Chips", Root, "￥" + player.Chips, 14, TextAnchor.UpperLeft, Color.white, font);
            ChipsText.rectTransform.anchorMin = new Vector2(0.03f, 0.25f);
            ChipsText.rectTransform.anchorMax = new Vector2(0.38f, 0.52f);
            ChipsText.rectTransform.sizeDelta = Vector2.zero;

            BetText = MkTx("Bet", Root, "", 13, TextAnchor.UpperLeft, new Color(0.9f, 0.85f, 0.3f), font);
            BetText.rectTransform.anchorMin = new Vector2(0.03f, 0.08f);
            BetText.rectTransform.anchorMax = new Vector2(0.38f, 0.22f);
            BetText.rectTransform.sizeDelta = Vector2.zero;

            StatusText = MkTx("Status", Root, "", 13, TextAnchor.MiddleLeft, new Color(0.6f, 0.6f, 0.6f), font);
            StatusText.rectTransform.anchorMin = new Vector2(0.03f, -0.05f);
            StatusText.rectTransform.anchorMax = new Vector2(0.38f, 0.06f);
            StatusText.rectTransform.sizeDelta = Vector2.zero;

            // --- Right side: two hole cards (proper proportion ~2.5:3.5) ---
            var c1go = new GameObject("Card1", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            c1go.layer = 5;
            var c1rt = c1go.GetComponent<RectTransform>();
            c1rt.SetParent(rt, false);
            c1rt.anchorMin = c1rt.anchorMax = new Vector2(0.56f, 0.50f);
            c1rt.sizeDelta = new Vector2(72, 104);
            c1rt.anchoredPosition = Vector2.zero;
            Card1Bg = c1go.GetComponent<Image>();
            Card1Bg.color = new Color(0.85f, 0.85f, 0.85f);
            var c1lbl = new GameObject("Lbl", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            c1lbl.layer = 5;
            var c1lrt = c1lbl.GetComponent<RectTransform>();
            c1lrt.SetParent(c1rt, false);
            c1lrt.anchorMin = Vector2.zero; c1lrt.anchorMax = Vector2.one; c1lrt.sizeDelta = Vector2.zero;
            Card1Label = c1lbl.GetComponent<Text>();
            Card1Label.font = font; Card1Label.fontSize = 22; Card1Label.alignment = TextAnchor.MiddleCenter;
            Card1Label.color = Color.black; Card1Label.raycastTarget = false;

            var c2go = new GameObject("Card2", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            c2go.layer = 5;
            var c2rt = c2go.GetComponent<RectTransform>();
            c2rt.SetParent(rt, false);
            c2rt.anchorMin = c2rt.anchorMax = new Vector2(0.86f, 0.50f);
            c2rt.sizeDelta = new Vector2(72, 104);
            c2rt.anchoredPosition = Vector2.zero;
            Card2Bg = c2go.GetComponent<Image>();
            Card2Bg.color = new Color(0.85f, 0.85f, 0.85f);
            var c2lbl = new GameObject("Lbl", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            c2lbl.layer = 5;
            var c2lrt = c2lbl.GetComponent<RectTransform>();
            c2lrt.SetParent(c2rt, false);
            c2lrt.anchorMin = Vector2.zero; c2lrt.anchorMax = Vector2.one; c2lrt.sizeDelta = Vector2.zero;
            Card2Label = c2lbl.GetComponent<Text>();
            Card2Label.font = font; Card2Label.fontSize = 22; Card2Label.alignment = TextAnchor.MiddleCenter;
            Card2Label.color = Color.black; Card2Label.raycastTarget = false;
        }

        private Text MkTx(string n, GameObject p, string t, int s, TextAnchor a, Color c, Font f)
        {
            var go = new GameObject(n, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.layer = 5;
            go.GetComponent<RectTransform>().SetParent(p.GetComponent<RectTransform>(), false);
            var tx = go.GetComponent<Text>();
            tx.font = f; tx.text = t; tx.fontSize = s; tx.alignment = a; tx.color = c; tx.raycastTarget = false;
            return tx;
        }

        public void Refresh()
        {
            if (Player == null) return;
            ChipsText.text = "￥" + Player.Chips;
            BetText.text = Player.CurrentBet > 0 ? "Bet ￥" + Player.CurrentBet : "";
            // Show status next to name
            string statusTag = Player.State switch
            {
                PlayerState.Out => " <color=#ff4444>[OUT]</color>",
                PlayerState.AllIn => " <color=#ffcc00>[ALL IN]</color>",
                PlayerState.Folded => " <color=#888888>[Fold]</color>",
                _ => Player.IsDealer ? " <color=#aaaaff>[D]</color>" : ""
            };
            NameText.text = Player.Name + statusTag;
            NameText.supportRichText = true;
            StatusText.text = "";

            bool showCards = Player.IsHuman;
            if (!showCards)
            {
                var gm = Object.FindFirstObjectByType<GameTableController>();
                if (gm != null && gm.gm != null && gm.gm.Phase == GamePhase.Showdown)
                    showCards = true;
            }

            if (Player.HoleCards.Count >= 2)
            {
                if (showCards)
                {
                    var c1 = Player.HoleCards[0];
                    var c2 = Player.HoleCards[1];
                    Card1Label.text = c1.ToString();
                    Card2Label.text = c2.ToString();
                    bool red1 = c1.Suit == Suit.Hearts || c1.Suit == Suit.Diamonds;
                    bool red2 = c2.Suit == Suit.Hearts || c2.Suit == Suit.Diamonds;
                    Card1Label.color = red1 ? new Color(1f, 0.3f, 0.3f) : Color.black;
                    Card2Label.color = red2 ? new Color(1f, 0.3f, 0.3f) : Color.black;
                    Card1Bg.color = Color.white;
                    Card2Bg.color = Color.white;
                }
                else
                {
                    Card1Label.text = "?";
                    Card2Label.text = "?";
                    Card1Label.color = new Color(0.4f, 0.4f, 0.5f);
                    Card2Label.color = new Color(0.4f, 0.4f, 0.5f);
                    Card1Bg.color = new Color(0.3f, 0.35f, 0.45f);
                    Card2Bg.color = new Color(0.3f, 0.35f, 0.45f);
                }
            }
            else
            {
                Card1Label.text = "";
                Card2Label.text = "";
                Card1Bg.color = new Color(0.85f, 0.85f, 0.85f);
                Card2Bg.color = new Color(0.85f, 0.85f, 0.85f);
            }

            if (Player.State == PlayerState.Folded || Player.State == PlayerState.Out)
                PanelBg.color = new Color(0.04f, 0.04f, 0.06f, 0.5f);
            else if (Player.State == PlayerState.AllIn)
                PanelBg.color = new Color(0.12f, 0.08f, 0.04f, 0.9f);
            else
                PanelBg.color = Player.IsHuman
                    ? new Color(0.1f, 0.15f, 0.25f, 0.9f)
                    : new Color(0.08f, 0.1f, 0.14f, 0.9f);
        }
    }



}
