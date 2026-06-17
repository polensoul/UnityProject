using System.Collections.Generic;

/// <summary>玩家行动类型</summary>
public enum PlayerAction { Fold, Check, Call, Raise, AllIn }

/// <summary>玩家状态</summary>
public enum PlayerState { Waiting, Active, Folded, AllIn, Out }

/// <summary>玩家基类</summary>
public class Player
{
    public string Name;
    public int Chips;
    public int CurrentBet;
    public int TotalBetThisRound;
    public List<Card> HoleCards = new(2);
    public PlayerState State = PlayerState.Waiting;
    public bool IsDealer;
    public bool IsHuman;

    public Player(string name, int chips, bool isHuman = false)
    {
        Name = name;
        Chips = chips;
        IsHuman = isHuman;
    }

    public void ResetForRound()
    {
        HoleCards.Clear();
        CurrentBet = 0;
        TotalBetThisRound = 0;
        if (State != PlayerState.Out)
            State = PlayerState.Waiting;
    }

    /// <summary>下注/跟注</summary>
    public int PlaceBet(int amount)
    {
        amount = System.Math.Min(amount, Chips);
        Chips -= amount;
        CurrentBet += amount;
        TotalBetThisRound += amount;
        if (Chips == 0) State = PlayerState.AllIn;
        return amount;
    }

    /// <summary>全下</summary>
    public int GoAllIn()
    {
        int amount = Chips;
        Chips = 0;
        CurrentBet += amount;
        TotalBetThisRound += amount;
        State = PlayerState.AllIn;
        return amount;
    }

    /// <summary>弃牌</summary>
    public void Fold()
    {
        State = PlayerState.Folded;
    }

    /// <summary>新一轮开始，重置当前轮下注</summary>
    public void NewBettingRound()
    {
        CurrentBet = 0;
    }
}
