using System.Collections.Generic;
using System.Linq;

/// <summary>手牌评估结果</summary>
public struct HandResult
{
    public HandRank Rank;
    public List<Card> BestCards; // 最佳5张牌（按重要性排序）
    public override string ToString() => $"{Rank} [{string.Join(" ", BestCards)}]";
}

/// <summary>德州扑克手牌评估器（7选5最优）</summary>
public static class HandEvaluator
{
    /// <summary>从 7 张牌中评估最佳 5 张手牌</summary>
    public static HandResult Evaluate(List<Card> holeCards, List<Card> communityCards)
    {
        var all = new List<Card>(holeCards);
        all.AddRange(communityCards);
        return EvaluateSeven(all);
    }

    private static HandResult EvaluateSeven(List<Card> cards)
    {
        HandResult best = default;
        best.Rank = HandRank.HighCard;
        best.BestCards = cards.OrderByDescending(c => c.Rank).ToList();
        int n = cards.Count;
        if (n < 5) return best;

        // 枚举所有 5 张牌组合
        for (int a = 0; a < n - 4; a++)
        for (int b = a + 1; b < n - 3; b++)
        for (int c = b + 1; c < n - 2; c++)
        for (int d = c + 1; d < n - 1; d++)
        for (int e = d + 1; e < n; e++)
        {
            var five = new List<Card> { cards[a], cards[b], cards[c], cards[d], cards[e] };
            var result = EvaluateFive(five);
            if (CompareResult(result, best) > 0)
                best = result;
        }
        return best;
    }

    private static HandResult EvaluateFive(List<Card> cards)
    {
        var sorted = cards.OrderByDescending(c => c.Rank).ToList();
        bool flush = sorted.All(c => c.Suit == sorted[0].Suit);
        bool straight = IsStraight(sorted);

        // 统计点数的频次
        var groups = sorted.GroupBy(c => c.Rank)
            .OrderByDescending(g => g.Key)
            .ThenByDescending(g => g.Count())
            .ToList();

        // 按频次分组排序
        var quads = groups.Where(g => g.Count() == 4).ToList();
        var trips = groups.Where(g => g.Count() == 3).ToList();
        var pairs = groups.Where(g => g.Count() == 2).ToList();
        var singles = groups.Where(g => g.Count() == 1).ToList();

        HandResult result = new();

        if (flush && straight && sorted[0].Rank == Rank.Ace)
        {
            result.Rank = HandRank.RoyalFlush;
            result.BestCards = sorted;
        }
        else if (flush && straight)
        {
            result.Rank = HandRank.StraightFlush;
            result.BestCards = sorted;
        }
        else if (quads.Count > 0)
        {
            result.Rank = HandRank.FourOfAKind;
            result.BestCards = BuildBest(quads, singles);
        }
        else if (trips.Count > 0 && pairs.Count > 0)
        {
            result.Rank = HandRank.FullHouse;
            result.BestCards = BuildBest(trips, pairs);
        }
        else if (flush)
        {
            result.Rank = HandRank.Flush;
            result.BestCards = sorted;
        }
        else if (straight)
        {
            result.Rank = HandRank.Straight;
            result.BestCards = sorted;
        }
        else if (trips.Count > 0)
        {
            result.Rank = HandRank.ThreeOfAKind;
            result.BestCards = BuildBest(trips, singles);
        }
        else if (pairs.Count >= 2)
        {
            result.Rank = HandRank.TwoPair;
            result.BestCards = BuildBest(pairs.Take(2).ToList(), singles);
        }
        else if (pairs.Count == 1)
        {
            result.Rank = HandRank.OnePair;
            result.BestCards = BuildBest(pairs, singles);
        }
        else
        {
            result.Rank = HandRank.HighCard;
            result.BestCards = sorted;
        }

        return result;
    }

    private static bool IsStraight(List<Card> sorted)
    {
        // 常规顺子
        bool straight = true;
        for (int i = 0; i < 4; i++)
            if ((int)sorted[i].Rank - (int)sorted[i + 1].Rank != 1)
                { straight = false; break; }
        if (straight) return true;

        // A-2-3-4-5 (Ace low)
        if (sorted[0].Rank == Rank.Ace &&
            sorted[1].Rank == Rank.Five &&
            sorted[2].Rank == Rank.Four &&
            sorted[3].Rank == Rank.Three &&
            sorted[4].Rank == Rank.Two)
            return true;

        return false;
    }

    /// <summary>合并频次组（先排高频次组，再排单张）</summary>
    private static List<Card> BuildBest(List<IGrouping<Rank, Card>> primary, List<IGrouping<Rank, Card>> kickers)
    {
        var result = new List<Card>();
        foreach (var g in primary) result.AddRange(g);
        foreach (var g in kickers) result.AddRange(g);
        return result.Take(5).ToList();
    }

    /// <summary>比较两个手牌结果，>0 表示 a > b</summary>
    public static int CompareResult(HandResult a, HandResult b)
    {
        if (a.Rank != b.Rank) return a.Rank.CompareTo(b.Rank);
        // 同等级比较踢脚牌
        for (int i = 0; i < System.Math.Min(a.BestCards.Count, b.BestCards.Count); i++)
        {
            int cmp = a.BestCards[i].Rank.CompareTo(b.BestCards[i].Rank);
            if (cmp != 0) return cmp;
        }
        return 0;
    }
}
