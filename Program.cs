using System.Globalization;
using IfpBonus;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

if (args.Length == 1 && args[0] == "--test")
    return PaymentTests.Run();

if (args.Length != 0)
{
    Console.Error.WriteLine("Usage: dotnet run -- [--test]");
    return 1;
}

Console.WriteLine("IFP bonus task - Alimzhan Almukhambetov - IT-2510");

// Limits and behaviors are chosen by the caller.
IPaymentChannel card = new CardChannel(maximumAmount: 500m);
IPaymentChannel transfer = new TransferChannel(minimumAmount: 100m);
Func<decimal, decimal> percent = a => a * 0.02m;
Func<decimal, decimal> flat = _ => 5m;
var start = new PaymentBalance("IT-2510-01", 1000m);

Console.WriteLine("\nTask 1 - Caller-defined channel limits");
Console.WriteLine("Card: 0 < amount <= 500; bank transfer: amount >= 100.");

Console.WriteLine("\nTask 2 - Both channels with both commission policies");
ShowDeposit("Card / 2 percent", start, 200m, card, percent);
ShowDeposit("Card / flat 5", start, 200m, card, flat);
ShowDeposit("Transfer / 2 percent", start, 200m, transfer, percent);
ShowDeposit("Transfer / flat 5", start, 200m, transfer, flat);

Console.WriteLine("\nTask 2 - Two deposits in a row");
var afterFirst = ShowDeposit("First deposit", start, 200m, card, percent);
var afterSecond = ShowDeposit("Second deposit", afterFirst, 300m, transfer, flat);
Console.WriteLine($"Saved snapshots: start={start.Amount:F2}, first={afterFirst.Amount:F2}, second={afterSecond.Amount:F2}");
Console.WriteLine($"Start unchanged: {start.Amount == 1000m}");
Console.WriteLine($"First unchanged after second deposit: {afterFirst.Amount == 1196m}");
var sameValues = new PaymentBalance(afterSecond.OwnerID, afterSecond.Amount);
Console.WriteLine($"afterSecond == new PaymentBalance(same values): {afterSecond == sameValues}");
Console.WriteLine($"ReferenceEquals(afterSecond, sameValues): {ReferenceEquals(afterSecond, sameValues)}");

Console.WriteLine("\nTask 3 - Deposit 3 with a flat commission of 5");
// Reproduce the starter code's unchecked result without calling the fixed method.
var originalResult = start with { Amount = start.Amount + 3m - flat(3m) };
Console.WriteLine($"Original formula: 1000 + 3 - 5 = {originalResult.Amount:F2}; original start={start.Amount:F2}");
try
{
    start.Deposit(3m, card, flat);
}
catch (ArgumentOutOfRangeException ex)
{
    Console.WriteLine($"Fixed Deposit rejected: {ex.GetType().Name} ({ex.ParamName})");
}
Console.WriteLine($"Balance after rejected deposit: {start.Amount:F2}");
return 0;

static PaymentBalance ShowDeposit(
    string label,
    PaymentBalance before,
    decimal amount,
    IPaymentChannel channel,
    Func<decimal, decimal> policy)
{
    var (updated, commission) = before.Deposit(amount, channel, policy);
    Console.WriteLine($"{label}: before={before.Amount:F2}, deposit={amount:F2}, fee={commission:F2}, after={updated.Amount:F2}");
    return updated;
}
