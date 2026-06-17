using System;

/// <summary>扑克牌花色</summary>
public enum Suit { Spades = 0, Hearts = 1, Diamonds = 2, Clubs = 3 }

/// <summary>扑克牌点数（2-14，14=Ace）</summary>
public enum Rank { Two = 2, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Jack, Queen, King, Ace }

/// <summary>手牌等级（从高到低）</summary>
public enum HandRank
{
    HighCard, OnePair, TwoPair, ThreeOfAKind, Straight,
    Flush, FullHouse, FourOfAKind, StraightFlush, RoyalFlush
}

/// <summary>一张扑克牌</summary>
public struct Card : IComparable<Card>
{
    public Suit Suit { get; }
    public Rank Rank { get; }

    public Card(Suit suit, Rank rank) { Suit = suit; Rank = rank; }

    public int CompareTo(Card other) => Rank.CompareTo(other.Rank);

    public override string ToString()
    {
        string r = Rank switch
        {
            Rank.Ace => "A", Rank.King => "K", Rank.Queen => "Q", Rank.Jack => "J", Rank.Ten => "10",
            _ => ((int)Rank).ToString()
        };
        string s = Suit switch
        {
            Suit.Spades => "♠", Suit.Hearts => "♥", Suit.Diamonds => "♦", Suit.Clubs => "♣",
            _ => "?"
        };
        return r + s;
    }
}
