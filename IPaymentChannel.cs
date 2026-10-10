namespace IfpBonus;

public interface IPaymentChannel
{
    string Name { get; }
    bool IsAllowed(decimal amount);
}
