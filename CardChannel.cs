namespace IfpBonus;

public sealed class CardChannel : IPaymentChannel
{
    public string Name => "Card";
    public decimal MaximumAmount { get; }

    public CardChannel(decimal maximumAmount)
    {
        if (maximumAmount <= 0m)
            throw new ArgumentOutOfRangeException(nameof(maximumAmount), "Card limit must be positive.");

        MaximumAmount = maximumAmount;
    }

    public bool IsAllowed(decimal amount) => amount > 0m && amount <= MaximumAmount;
}
