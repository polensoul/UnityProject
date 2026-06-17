using System.Collections.Generic;

/// <summary>52张标准扑克牌堆</summary>
public class Deck
{
    private List<Card> cards;
    private int nextIndex;

    public Deck()
    {
        cards = new List<Card>(52);
        foreach (Suit s in System.Enum.GetValues(typeof(Suit)))
        foreach (Rank r in System.Enum.GetValues(typeof(Rank)))
            cards.Add(new Card(s, r));
    }

    /// <summary>Fisher-Yates 洗牌</summary>
    public void Shuffle(System.Random rng = null)
    {
        rng ??= new System.Random();
        int n = cards.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            var tmp = cards[k];
            cards[k] = cards[n];
            cards[n] = tmp;
        }
        nextIndex = 0;
    }

    /// <summary>发一张牌</summary>
    public Card Deal()
    {
        if (nextIndex >= cards.Count)
            throw new System.InvalidOperationException("牌堆已空");
        return cards[nextIndex++];
    }

    /// <summary>剩余牌数</summary>
    public int Remaining => cards.Count - nextIndex;

    /// <summary>重置牌堆（重新收集所有牌）</summary>
    public void Reset()
    {
        nextIndex = 0;
    }
}
