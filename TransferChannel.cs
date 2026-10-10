namespace IfpBonus;

public sealed class TransferChannel : IPaymentChannel
{
    public string Name => "Bank transfer";
    public decimal MinimumAmount { get; }

    public TransferChannel(decimal minimumAmount)
    {
        if (minimumAmount <= 0m)
            throw new ArgumentOutOfRangeException(nameof(minimumAmount), "Transfer minimum must be positive.");

        MinimumAmount = minimumAmount;
    }

    public bool IsAllowed(decimal amount) => amount >= MinimumAmount;
}
