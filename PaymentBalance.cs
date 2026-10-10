namespace IfpBonus;

// A positional record has init-only properties and generated value equality.
public record PaymentBalance(string OwnerID, decimal Amount)
{
    public (PaymentBalance Updated, decimal Commission) Deposit(
        decimal amount,
        IPaymentChannel channel,
        Func<decimal, decimal> commissionPolicy)
    {
        PaymentValidation.ValidateDeposit(this, amount, channel, commissionPolicy);

        // Evaluate the supplied behavior once, then validate its result.
        decimal commission = commissionPolicy(amount);
        PaymentValidation.ValidateCommission(amount, commission);

        decimal netDeposit = amount - commission;
        decimal updatedAmount = checked(Amount + netDeposit);
        return (this with { Amount = updatedAmount }, commission);
    }
}
