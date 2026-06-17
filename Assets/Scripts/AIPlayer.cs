using System.Collections.Generic;
using System.Linq;

/// <summary>AI 玩家 — 根据难度采用不同的胜率估算策略</summary>
public class AIPlayer : Player
{
    private GameSettings.AIDifficulty difficulty;
    private System.Random rng;
    private const float NEVER_FOLD_THRESHOLD = 0.2f;

    public AIPlayer(string name, int chips, GameSettings.AIDifficulty diff)
        : base(name, chips)
    {
        difficulty = diff;
        rng = new System.Random();
        IsHuman = false;
    }

    /// <summary>AI 决策入口</summary>
    public PlayerAction DecideAction(List<Card> communityCards, int currentHighestBet, int potSize, int dealerIndex, int myIndex)
    {
        int callAmount = currentHighestBet - CurrentBet;
        float winProb = difficulty switch
        {
            GameSettings.AIDifficulty.Easy   => EstimateWinProb_Outs(communityCards),
            GameSettings.AIDifficulty.Medium => EstimateWinProb_Simulate(communityCards, 60),
            GameSettings.AIDifficulty.Hard   => EstimateWinProb_Hybrid(communityCards, dealerIndex, myIndex),
            _ => 0.1f
        };

        return MakeDecision(winProb, callAmount, potSize, dealerIndex, myIndex);
    }

    /// <summary>根据胜率做决策（胜率 > 20% 不盲目弃牌）</summary>
    private PlayerAction MakeDecision(float winProb, int callAmount, int pot, int dealer, int myIdx)
    {
        bool neverFold = winProb > NEVER_FOLD_THRESHOLD;
        float potOdds = pot > 0 ? (float)callAmount / (pot + callAmount) : 0f;
        bool isLatePos = (myIdx - dealer + 8) % 8 <= 2;

        // 无需跟注时（可过牌）
        if (callAmount == 0)
        {
            if (winProb > 0.65f) return PlayerAction.Raise;
            if (winProb > 0.4f && isLatePos && difficulty >= GameSettings.AIDifficulty.Medium) return PlayerAction.Raise;
            if (winProb > 0.5f && rng.NextDouble() < 0.3f) return PlayerAction.Raise;
            return PlayerAction.Check;
        }

        // 需要跟注时
        if (winProb > 0.75f) return PlayerAction.Raise;
        if (winProb > 0.6f && isLatePos && difficulty >= GameSettings.AIDifficulty.Medium) return PlayerAction.Raise;

        bool worthCalling = winProb > potOdds || neverFold;

        if (worthCalling)
        {
            if (callAmount > Chips * 0.4f && winProb < 0.45f)
                return neverFold ? PlayerAction.Call : PlayerAction.Fold;
            if (callAmount >= Chips) return PlayerAction.AllIn;
            return PlayerAction.Call;
        }

        // Hard 难度下小注码可适度松跟
        if (difficulty >= GameSettings.AIDifficulty.Hard && winProb > 0.15f && callAmount < Chips * 0.08f)
            return PlayerAction.Call;

        return PlayerAction.Fold;
    }

    // ──────────────────────────────────────────────
    //  Easy 难度：基于当前牌型 + Outs 估算胜率
    //  无随机模拟，速度快
    // ──────────────────────────────────────────────
    private float EstimateWinProb_Outs(List<Card> community)
    {
        if (HoleCards == null || HoleCards.Count < 2) return 0f;
        var safeComm = community ?? new List<Card>();
        var result = HandEvaluator.Evaluate(HoleCards, safeComm);

        float baseProb = result.Rank switch
        {
            HandRank.RoyalFlush    => 0.98f,
            HandRank.StraightFlush => 0.95f,
            HandRank.FourOfAKind   => 0.92f,
            HandRank.FullHouse     => 0.85f,
            HandRank.Flush         => 0.75f,
            HandRank.Straight      => 0.70f,
            HandRank.ThreeOfAKind  => 0.60f,
            HandRank.TwoPair       => 0.50f,
            HandRank.OnePair       => 0.35f,
            HandRank.HighCard      => 0.15f,
            _ => 0.05f
        };

        // PreFlop 阶段增加底牌质量加成
        int communityCount = safeComm.Count;
        if (communityCount == 0)
        {
            if (HoleCards[0].Rank == HoleCards[1].Rank) baseProb += 0.1f;
            bool suited = HoleCards[0].Suit == HoleCards[1].Suit;
            if (suited) baseProb += 0.05f;
            int highRank = System.Math.Max((int)HoleCards[0].Rank, (int)HoleCards[1].Rank);
            if (highRank >= (int)Rank.Ten) baseProb += 0.05f;
        }

        float drawBonus = CountDrawOuts(HoleCards, safeComm);
        baseProb += drawBonus * (5 - communityCount) * 0.02f;

        return System.Math.Min(baseProb, 0.95f);
    }

    /// <summary>估算听牌 Outs 数量</summary>
    private float CountDrawOuts(List<Card> hole, List<Card> community)
    {
        float outs = 0f;
        if (community.Count == 0) return 0f;

        var allKnown = new HashSet<int>();
        foreach (var c in hole) allKnown.Add((int)c.Suit * 100 + (int)c.Rank);
        foreach (var c in community) allKnown.Add((int)c.Suit * 100 + (int)c.Rank);

        // 检查同花听牌
        foreach (var s in new[] { Suit.Spades, Suit.Hearts, Suit.Diamonds, Suit.Clubs })
        {
            int suitedCount = hole.Where(c => c.Suit == s).Count()
                            + community.Where(c => c.Suit == s).Count();
            if (suitedCount >= 3)
                outs += (5 - suitedCount);
        }

        // 检查顺子听牌（简化：按最大最小差距）
        var allRanks = hole.Concat(community).Select(c => (int)c.Rank).Distinct().OrderBy(r => r).ToList();
        if (allRanks.Count >= 3)
        {
            int minR = allRanks.First(), maxR = allRanks.Last();
            if (maxR - minR <= 4)
                outs += (5 - allRanks.Count);
        }

        return outs;
    }

    /// <summary>构建剩余牌堆（供模拟使用）</summary>
    private List<Card> BuildRemainingDeck()
    {
        var usedCards = new HashSet<int>();
        foreach (var c in HoleCards) usedCards.Add(CardToId(c));

        var deck = new List<Card>();
        for (int s = 0; s < 4; s++)
        for (int r = 2; r <= 14; r++)
        {
            int id = s * 100 + r;
            if (!usedCards.Contains(id))
                deck.Add(new Card((Suit)s, (Rank)r));
        }
        return deck;
    }

    /// <summary>从剩余牌堆中随机抽取指定数量的牌</summary>
    private List<Card> DrawRandomCards(List<Card> deck, int count, HashSet<int> exclude)
    {
        var drawn = new List<Card>();
        var available = deck.Where(c => !exclude.Contains(CardToId(c))).ToList();
        ShuffleList(available);
        for (int i = 0; i < count && i < available.Count; i++)
            drawn.Add(available[i]);
        return drawn;
    }

    // ──────────────────────────────────────────────
    //  Medium 难度：蒙特卡洛模拟（60次）
    //  随机模拟剩余牌 + 对手随机底牌
    // ──────────────────────────────────────────────
    private float EstimateWinProb_Simulate(List<Card> community, int simCount)
    {
        if (HoleCards == null || HoleCards.Count < 2) return 0f;
        var safeComm = community ?? new List<Card>();
        var remainingDeck = BuildRemainingDeck();

        if (remainingDeck.Count < 4) return 0.2f;

        int wins = 0, totalSims = 0;
        int cardsNeeded = 5 - safeComm.Count;

        for (int sim = 0; sim < simCount; sim++)
        {
            ShuffleList(remainingDeck);
            var simComm = new List<Card>(safeComm);
            for (int i = 0; i < cardsNeeded && i < remainingDeck.Count; i++)
                simComm.Add(remainingDeck[i]);

            int oppStart = cardsNeeded;
            if (oppStart + 2 > remainingDeck.Count) break;
            var oppHole = new List<Card> { remainingDeck[oppStart], remainingDeck[oppStart + 1] };

            var myResult = HandEvaluator.Evaluate(HoleCards, simComm);
            var oppResult = HandEvaluator.Evaluate(oppHole, simComm);
            if (HandEvaluator.CompareResult(myResult, oppResult) >= 0)
                wins++;

            totalSims++;
        }

        if (totalSims == 0) return 0.2f;
        return (float)wins / totalSims;
    }

    // ──────────────────────────────────────────────
    //  Hard 难度：蒙特卡洛（150次）+ 位置优势 + 读牌调整
    // ──────────────────────────────────────────────
    private float EstimateWinProb_Hybrid(List<Card> community, int dealerIdx, int myIdx)
    {
        if (HoleCards == null || HoleCards.Count < 2) return 0f;
        var safeComm = community ?? new List<Card>();
        var remainingDeck = BuildRemainingDeck();

        if (remainingDeck.Count < 4) return 0.2f;

        int simCount = 150;
        float totalEquity = 0f;
        int cardsNeeded = 5 - safeComm.Count;
        int totalSims = 0;
        int opponentCount = 2;

        for (int sim = 0; sim < simCount; sim++)
        {
            ShuffleList(remainingDeck);
            var simComm = new List<Card>(safeComm);
            int offset = 0;
            for (int i = 0; i < cardsNeeded && offset < remainingDeck.Count; i++, offset++)
                simComm.Add(remainingDeck[offset]);

            var myResult = HandEvaluator.Evaluate(HoleCards, simComm);
            int beaten = 0;

            for (int opp = 0; opp < opponentCount; opp++)
            {
                int o1 = offset, o2 = offset + 1;
                offset += 2;
                if (o2 >= remainingDeck.Count) break;
                var oppHole = new List<Card> { remainingDeck[o1], remainingDeck[o2] };
                var oppResult = HandEvaluator.Evaluate(oppHole, simComm);
                if (HandEvaluator.CompareResult(myResult, oppResult) >= 0)
                    beaten++;
            }

            totalEquity += beaten > 0 ? (float)beaten / opponentCount : 0f;
            totalSims++;
        }

        if (totalSims == 0) return 0.2f;
        float rawProb = totalEquity / totalSims;

        // 位置加成
        bool isLatePos = (myIdx - dealerIdx + 8) % 8 <= 2;
        if (isLatePos) rawProb += 0.04f;

        // 后期阶段经验加成（翻牌后信息更丰富）
        if (safeComm.Count >= 4) rawProb += 0.03f;

        return System.Math.Min(rawProb, 0.95f);
    }

    private int CardToId(Card c) => (int)c.Suit * 100 + (int)c.Rank;

    private void ShuffleList(List<Card> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            var tmp = list[k];
            list[k] = list[n];
            list[n] = tmp;
        }
    }

    /// <summary>AI 加注金额计算</summary>
    public int GetRaiseAmount(int currentHighestBet, int minRaise, int potSize)
    {
        float aggression = difficulty switch
        {
            GameSettings.AIDifficulty.Easy => 0.3f,
            GameSettings.AIDifficulty.Medium => 0.5f,
            GameSettings.AIDifficulty.Hard => 0.8f,
            _ => 0.3f
        };

        int maxRaise = System.Math.Min(Chips, potSize);
        int raise = minRaise + (int)((maxRaise - minRaise) * aggression * (float)rng.NextDouble());
        return System.Math.Min(raise, Chips);
    }
}
