using System.Collections.Generic;
using System.Linq;

/// <summary>游戏阶段</summary>
public enum GamePhase { PreFlop, Flop, Turn, River, Showdown, GameOver }

/// <summary>游戏管理器 — 核心德州扑克逻辑</summary>
public class GameManager
{
    public List<Player> Players;
    public List<Card> CommunityCards = new(5);
    public Deck Deck = new();
    public int Pot;
    public int DealerIndex;
    public int CurrentPlayerIndex;
    public int SmallBlind, BigBlind;
    public int CurrentHighestBet;
    public int MinRaise;
    public int MaxRaiseCap = int.MaxValue;   // max raise per action (venue limit)
    public int MaxAllInCap = int.MaxValue;   // max all-in amount (venue limit)
    public GamePhase Phase;
    public bool HandInProgress;
    public string LastActionLog = "";
    public List<string> ActionLogs = new List<string>();  // accumulated log entries
    public int LogConsumedIndex = 0;  // UI has consumed up to this index

    private System.Random rng = new();
    private int bbIndex; // 当前一手的大盲注玩家索引
    private bool bbOptionPending; // 大盲注是否还未行使选项权
    private int actionsInARow; // 自上次加注/新阶段起连续行动次数 // 大盲注是否还未行使选项权
    private int raiseCountThisRound; // per-round raise counter (max 5)

    public GameManager() { }

    /// <summary>初始化新游戏</summary>
    public void InitializeGame(List<Player> players, int startChips, int smallBlind = 10)
    {
        Players = players;
        SmallBlind = smallBlind;
        BigBlind = smallBlind * 2;
        MinRaise = BigBlind;
        DealerIndex = rng.Next(Players.Count);
    }

    /// <summary>开始一手牌</summary>
    public void StartHand()
    {
        ActionLogs.Clear();
        LogConsumedIndex = 0;
        CommunityCards.Clear();
        Pot = 0;
        CurrentHighestBet = 0;
        Phase = GamePhase.PreFlop;
        HandInProgress = true;
        actionsInARow = 0;
        raiseCountThisRound = 0;

        foreach (var p in Players)
        {
            if (p.State == PlayerState.Out) continue;
            p.ResetForRound();
            p.IsDealer = false;
        }

        // 庄家轮转
        do { DealerIndex = (DealerIndex + 1) % Players.Count; }
        while (Players[DealerIndex].State == PlayerState.Out);
        Players[DealerIndex].IsDealer = true;

        // 洗牌发牌
        Deck.Reset();
        Deck.Shuffle(rng);
        foreach (var p in Players)
        {
            if (p.State == PlayerState.Out) continue;
            p.HoleCards.Add(Deck.Deal());
            p.HoleCards.Add(Deck.Deal());
        }

        // 盲注
        int sbIdx = GetActiveNext(DealerIndex);
        int bbIdx = GetActiveNext(sbIdx);

        Players[sbIdx].PlaceBet(SmallBlind);
        Players[bbIdx].PlaceBet(BigBlind);
        Pot += SmallBlind + BigBlind;
        CurrentHighestBet = BigBlind;
        bbIndex = bbIdx;
        bbOptionPending = true; // 大盲注尚未行使选项权

        // 设置活跃玩家
        foreach (var p in Players)
        {
            if (p.State != PlayerState.Folded && p.State != PlayerState.Out)
                p.State = PlayerState.Active;
        }

        // 第一个行动的是大盲注之后的活跃玩家
        CurrentPlayerIndex = GetActiveNext(bbIdx);

        Log($"=== 新一手牌开始，庄家: {Players[DealerIndex].Name} ===");
    }

    public Player GetCurrentPlayer() => Players[CurrentPlayerIndex];

    public bool IsCurrentPlayerHuman() => GetCurrentPlayer().IsHuman;

    /// <summary>获取人类玩家的合法操作列表</summary>
    public List<PlayerAction> GetAvailableActions()
    {
        var player = GetCurrentPlayer();
        var actions = new List<PlayerAction>();

        int callAmount = CurrentHighestBet - player.CurrentBet;

            bool canRaise = raiseCountThisRound < 5;
        if (callAmount == 0)
        {
            actions.Add(PlayerAction.Check);
            if (player.Chips > 0 && canRaise) actions.Add(PlayerAction.Raise);
        }
        else
        {
            if (callAmount >= player.Chips)
                actions.Add(PlayerAction.AllIn);
            else
            {
                actions.Add(PlayerAction.Call);
                if (player.Chips > callAmount && canRaise) actions.Add(PlayerAction.Raise);
                actions.Add(PlayerAction.AllIn);
            }
            actions.Add(PlayerAction.Fold);
        }

        return actions;
    }

    public void ExecuteHumanAction(PlayerAction action, int raiseAmount = 0)
    {
        ExecuteAction(GetCurrentPlayer(), action, raiseAmount);
    }

    public bool ExecuteAIAction()
    {
        var player = GetCurrentPlayer();
        if (player.IsHuman) return false;

        var ai = player as AIPlayer;
        if (ai == null) return false;

        var action = ai.DecideAction(CommunityCards, CurrentHighestBet, Pot, DealerIndex, CurrentPlayerIndex);
        int raiseAmount = 0;

        if (action == PlayerAction.Raise)
        {
            if (raiseCountThisRound >= 5)
            {
                // Cap reached: downgrade to Call or Check
                int callAmt = CurrentHighestBet - player.CurrentBet;
                action = callAmt == 0 ? PlayerAction.Check : PlayerAction.Call;
                raiseAmount = 0;
            }
            else
            {
                raiseAmount = System.Math.Min(ai.GetRaiseAmount(CurrentHighestBet, MinRaise, Pot), MaxRaiseCap);
                if (raiseAmount >= player.Chips) action = PlayerAction.AllIn;
            }
        }

        ExecuteAction(player, action, raiseAmount);
        return true;
    }

    private void ExecuteAction(Player player, PlayerAction action, int raiseAmount = 0)
    {
        string actionStr = "";
        int oldHighestBet = CurrentHighestBet;

        switch (action)
        {
            case PlayerAction.Fold:
                player.Fold();
                actionStr = "弃牌";
                break;

            case PlayerAction.Check:
                actionStr = "过牌";
                break;

            case PlayerAction.Call:
                int callAmount = CurrentHighestBet - player.CurrentBet;
                int callPaid = player.PlaceBet(callAmount);
                Pot += callPaid;
                actionStr = $"跟注 {callPaid}";
                break;

            case PlayerAction.Raise:
                raiseAmount = System.Math.Min(raiseAmount, MaxRaiseCap);
                int raise = player.PlaceBet(raiseAmount);
                Pot += raise;
                CurrentHighestBet = player.CurrentBet;
                MinRaise = raiseAmount;
                actionStr = $"加注 {raise}";
                break;

            case PlayerAction.AllIn:
                int allInAmount = player.GoAllIn();
                Pot += allInAmount;
                if (player.CurrentBet > oldHighestBet)
                    CurrentHighestBet = player.CurrentBet;
                actionStr = $"全下 {allInAmount}";
                break;
        }

        bool isRaise = action == PlayerAction.Raise;
        bool isAllInRaise = action == PlayerAction.AllIn && player.CurrentBet > oldHighestBet;

        if (isRaise || isAllInRaise)
        {
            actionsInARow = 0; // reset counter so others must respond to raise
            raiseCountThisRound++; // only count actual raises
        }

        Log($"{player.Name}: {actionStr}");

        // 如果大盲注在翻牌前首次行动，标记选项权已行使
        if (Phase == GamePhase.PreFlop && bbOptionPending && player == Players[bbIndex])
            bbOptionPending = false;

        // 检查剩余玩家状态
        int activeCount = Players.Count(p => p.State == PlayerState.Active);
        int allInCount = Players.Count(p => p.State == PlayerState.AllIn);
        int totalRemaining = activeCount + allInCount;

        if (totalRemaining <= 1)
        {
            if (Phase != GamePhase.Showdown)
                EndHand();
            return;
        }

        if (activeCount == 0)
        {
            while (Phase < GamePhase.River)
                AdvancePhase();
            Showdown();
            return;
        }

        // 推进到下一个活跃玩家
        AdvanceToNextPlayer();

        // 只有当所有活跃玩家都已回应当前下注额，且下注持平时才进入下一阶段
        actionsInARow++;
        bool everyoneResponded = actionsInARow >= activeCount;
        if (everyoneResponded && AllBetsEqual())
        {
            AdvancePhase();
        }
    }

    /// <summary>推进到下一个未弃牌的活跃玩家</summary>
    private void AdvanceToNextPlayer()
    {
        int start = CurrentPlayerIndex;
        do
        {
            CurrentPlayerIndex = (CurrentPlayerIndex + 1) % Players.Count;
        }
        while (Players[CurrentPlayerIndex].State != PlayerState.Active
               && CurrentPlayerIndex != start);
    }

    /// <summary>检查所有活跃玩家下注是否持平</summary>
    private bool AllBetsEqual()
    {
        // PreFlop 阶段：大盲注尚未行使选项权时，不下注持平不算结束
        if (Phase == GamePhase.PreFlop && bbOptionPending)
            return false;

        foreach (var p in Players)
        {
            if (p.State == PlayerState.Active && p.CurrentBet != CurrentHighestBet && p.Chips > 0)
                return false;
        }
        return true;
    }

    /// <summary>推进到下一阶段</summary>
    public void AdvancePhase()
    {
        foreach (var p in Players) p.NewBettingRound();
        CurrentHighestBet = 0;
        MinRaise = BigBlind;
        actionsInARow = 0; // 新阶段，重置行动计数
        raiseCountThisRound = 0; // reset raise counter

        switch (Phase)
        {
            case GamePhase.PreFlop:
                Phase = GamePhase.Flop;
                CommunityCards.Add(Deck.Deal());
                CommunityCards.Add(Deck.Deal());
                CommunityCards.Add(Deck.Deal());
                Log("--- 翻牌 (Flop) ---");
                break;
            case GamePhase.Flop:
                Phase = GamePhase.Turn;
                CommunityCards.Add(Deck.Deal());
                Log("--- 转牌 (Turn) ---");
                break;
            case GamePhase.Turn:
                Phase = GamePhase.River;
                CommunityCards.Add(Deck.Deal());
                Log("--- 河牌 (River) ---");
                break;
            case GamePhase.River:
                Showdown();
                return;
        }

        CurrentPlayerIndex = GetActiveNext(DealerIndex);

        if (Players.Count(p => p.State == PlayerState.Active) <= 1)
        {
            Showdown();
        }
    }
    public void Showdown()
    {
        Phase = GamePhase.Showdown;
        Log("=== 摊牌 ===");

        var activePlayers = Players.Where(p =>
            p.State == PlayerState.Active || p.State == PlayerState.AllIn).ToList();

        if (activePlayers.Count == 1)
        {
            var winner = activePlayers[0];
            winner.Chips += Pot;
            Log($"{winner.Name} 赢得底池 {Pot}!");
            Pot = 0;
        }
        else
        {
            // Evaluate all hands first
            var handResults = new Dictionary<Player, HandResult>();
            foreach (var p in activePlayers)
            {
                var hand = HandEvaluator.Evaluate(p.HoleCards, CommunityCards);
                handResults[p] = hand;
                if (p.State == PlayerState.AllIn)
                    Log(p.Name + "(AllIn): " + hand);
                else
                    Log(p.Name + ": " + hand + " [" + string.Join(" ", p.HoleCards) + "]");
            }

            // Distribute with side-pot logic
            var levels = activePlayers.Select(p => p.TotalBetThisRound).Distinct().OrderBy(l => l).ToList();
            int prevLevel = 0;
            foreach (int level in levels)
            {
                int levelAmount = level - prevLevel;
                if (levelAmount <= 0) continue;
                var eligible = activePlayers.Where(p => p.TotalBetThisRound >= level).ToList();
                int potAmount = levelAmount * eligible.Count;
                Player bestPlayer = null;
                HandResult bestHand = default;
                foreach (var p in eligible)
                {
                    if (handResults.TryGetValue(p, out var h))
                    {
                        if (bestPlayer == null || HandEvaluator.CompareResult(h, bestHand) > 0)
                        { bestPlayer = p; bestHand = h; }
                    }
                }
                if (bestPlayer != null && potAmount > 0)
                {
                    bestPlayer.Chips += potAmount;
                    string potLabel = prevLevel == 0 ? "主池" : "边池";
                    Log($">>> {bestPlayer.Name} 以 {bestHand.Rank} 赢得{potLabel} {potAmount}！");
                }
                prevLevel = level;
            }
            Pot = 0;
        }

        HandInProgress = false;
    }

    private void EndHand()
    {
        // 补齐剩余公共牌以确保结算展示完整
        while (CommunityCards.Count < 5 && Deck.Remaining > 0)
        {
            if (Phase < GamePhase.Flop)
            {
                CommunityCards.Add(Deck.Deal());
                CommunityCards.Add(Deck.Deal());
                CommunityCards.Add(Deck.Deal());
                Phase = GamePhase.Flop;
            }
            else if (Phase == GamePhase.Flop)
            {
                CommunityCards.Add(Deck.Deal());
                Phase = GamePhase.Turn;
            }
            else if (Phase == GamePhase.Turn)
            {
                CommunityCards.Add(Deck.Deal());
                Phase = GamePhase.River;
            }
            else break;
        }

        var winner = Players.FirstOrDefault(p =>
            p.State == PlayerState.Active || p.State == PlayerState.AllIn);
        if (winner != null)
        {
            winner.Chips += Pot;
            Log($"{winner.Name} 赢得底池 {Pot}（其余玩家弃牌）");
            Pot = 0;
        }
        HandInProgress = false;
        Phase = GamePhase.Showdown;
    }

    public bool IsGameOver()
    {
        return Players.Count(p => p.Chips > 0) <= 1;
    }

    public Player GetWinner()
    {
        return Players.OrderByDescending(p => p.Chips).FirstOrDefault();
    }

    public void EliminateBrokePlayers()
    {
        foreach (var p in Players)
            if (p.Chips <= 0) p.State = PlayerState.Out;
    }

    private int GetActiveNext(int fromIndex)
    {
        int idx = fromIndex;
        do { idx = (idx + 1) % Players.Count; }
        while (Players[idx].State == PlayerState.Out && idx != fromIndex);
        return idx;
    }

    private void Log(string msg) { LastActionLog = msg; ActionLogs.Add(msg); }
}
