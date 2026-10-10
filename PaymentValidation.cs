namespace IfpBonus;

public static class PaymentValidation
{
    public static void ValidateDeposit(
        PaymentBalance balance,
        decimal amount,
        IPaymentChannel channel,
        Func<decimal, decimal> commissionPolicy)
    {
        ArgumentNullException.ThrowIfNull(channel);
        ArgumentNullException.ThrowIfNull(commissionPolicy);

        if (string.IsNullOrWhiteSpace(balance.OwnerID))
            throw new InvalidOperationException("The balance must have an owner ID.");
        if (balance.Amount < 0m)
            throw new InvalidOperationException("The starting balance must not be negative.");
        if (amount <= 0m)
            throw new ArgumentOutOfRangeException(nameof(amount), "Deposit amount must be positive.");
        if (!channel.IsAllowed(amount))
            throw new ArgumentException($"{channel.Name} does not allow this deposit amount.", nameof(amount));
    }

    public static void ValidateCommission(decimal amount, decimal commission)
    {
        if (commission < 0m || commission > amount)
            throw new ArgumentOutOfRangeException(nameof(commission),
                "Commission must be between zero and the deposit amount.");
    }
}
