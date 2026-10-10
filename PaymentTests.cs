namespace IfpBonus;

// A small dependency-free test runner: dotnet run -- --test
internal static class PaymentTests
{
    public static int Run()
    {
        var start = new PaymentBalance("IT-2510-01", 1000m);
        IPaymentChannel card = new CardChannel(500m);
        IPaymentChannel transfer = new TransferChannel(100m);
        Func<decimal, decimal> percent = a => a * 0.02m;
        Func<decimal, decimal> flat = _ => 5m;

        var tests = new (string Name, Action Test)[]
        {
            ("Card and percent commission", () => CheckDeposit(card, percent, 1196m, 4m)),
            ("Card and flat commission", () => CheckDeposit(card, flat, 1195m, 5m)),
            ("Transfer and percent commission", () => CheckDeposit(transfer, percent, 1196m, 4m)),
            ("Transfer and flat commission", () => CheckDeposit(transfer, flat, 1195m, 5m)),
            ("Sequential deposits preserve snapshots", () =>
            {
                var first = start.Deposit(200m, card, percent).Updated;
                var second = first.Deposit(300m, transfer, flat).Updated;
                Equal(1491m, second.Amount);
                Equal(1196m, first.Amount);
                Equal(1000m, start.Amount);
                Equal(start.OwnerID, second.OwnerID);
                Equal(false, ReferenceEquals(start, first));
            }),
            ("Records compare by values with ==", () =>
            {
                var copy = new PaymentBalance(start.OwnerID, start.Amount);
                Equal(true, start == copy);
                Equal(false, ReferenceEquals(start, copy));
                Equal(false, start == new PaymentBalance("another-owner", start.Amount));
                Equal(false, start == new PaymentBalance(start.OwnerID, 999m));
            }),
            ("Exact card limit is accepted", () => Equal(1490m, start.Deposit(500m, card, percent).Updated.Amount)),
            ("Above card limit is rejected", () => Throws<ArgumentException>(() => start.Deposit(500.01m, card, percent))),
            ("Exact transfer minimum is accepted", () => Equal(1098m, start.Deposit(100m, transfer, percent).Updated.Amount)),
            ("Below transfer minimum is rejected", () => Throws<ArgumentException>(() => start.Deposit(99.99m, transfer, percent))),
            ("Zero deposit is rejected", () => Throws<ArgumentOutOfRangeException>(() => start.Deposit(0m, card, flat))),
            ("Negative deposit is rejected", () => Throws<ArgumentOutOfRangeException>(() => start.Deposit(-10m, card, flat))),
            ("Null channel is rejected", () => Throws<ArgumentNullException>(() => start.Deposit(200m, null!, flat))),
            ("Null policy is rejected", () => Throws<ArgumentNullException>(() => start.Deposit(200m, card, null!))),
            ("Task 3 rejects fee exceeding deposit", () =>
            {
                Throws<ArgumentOutOfRangeException>(() => start.Deposit(3m, card, flat));
                Equal(1000m, start.Amount);
            }),
            ("Negative commission is rejected", () => Throws<ArgumentOutOfRangeException>(() => start.Deposit(200m, card, _ => -1m))),
            ("Commission equal to deposit gives zero net credit", () => Equal(1000m, start.Deposit(5m, card, flat).Updated.Amount)),
            ("Zero commission credits the whole deposit", () => Equal(1200m, start.Deposit(200m, card, _ => 0m).Updated.Amount)),
            ("Channel limits are caller-defined", () =>
            {
                Equal(false, new CardChannel(50m).IsAllowed(51m));
                Equal(true, new CardChannel(75m).IsAllowed(51m));
                Equal(false, new TransferChannel(150m).IsAllowed(100m));
                Equal(true, new TransferChannel(25m).IsAllowed(100m));
            }),
            ("Invalid channel configuration is rejected", () =>
            {
                Throws<ArgumentOutOfRangeException>(() => new CardChannel(0m));
                Throws<ArgumentOutOfRangeException>(() => new CardChannel(-1m));
                Throws<ArgumentOutOfRangeException>(() => new TransferChannel(0m));
                Throws<ArgumentOutOfRangeException>(() => new TransferChannel(-1m));
            }),
            ("Invalid starting state is rejected", () =>
            {
                Throws<InvalidOperationException>(() => new PaymentBalance(" ", 100m).Deposit(200m, card, flat));
                Throws<InvalidOperationException>(() => new PaymentBalance("owner", -1m).Deposit(200m, card, flat));
            }),
            ("Rejected channel does not execute policy", () =>
            {
                int calls = 0;
                Throws<ArgumentException>(() => start.Deposit(501m, card, a => { calls++; return a * 0.02m; }));
                Equal(0, calls);
            }),
            ("Successful deposit executes policy once", () =>
            {
                int calls = 0;
                var result = start.Deposit(200m, card, a => { calls++; return a * 0.02m; });
                Equal(1, calls);
                Equal(4m, result.Commission);
                Equal(1196m, result.Updated.Amount);
            }),
            ("Policy failure leaves starting balance unchanged", () =>
            {
                Throws<InvalidOperationException>(() => start.Deposit(200m, card, _ => throw new InvalidOperationException("Policy failed.")));
                Equal(1000m, start.Amount);
            }),
            ("Decimal retains fractional commission", () =>
            {
                var result = start.Deposit(10.01m, card, percent);
                Equal(0.2002m, result.Commission);
                Equal(1009.8098m, result.Updated.Amount);
            }),
            ("Balance overflow is rejected without mutation", () =>
            {
                var large = new PaymentBalance("owner", decimal.MaxValue);
                Throws<OverflowException>(() => large.Deposit(10m, card, _ => 0m));
                Equal(decimal.MaxValue, large.Amount);
            }),
            ("Net calculation avoids unnecessary intermediate overflow", () =>
            {
                var large = new PaymentBalance("owner", decimal.MaxValue);
                Equal(decimal.MaxValue, large.Deposit(5m, card, flat).Updated.Amount);
            })
        };

        int failed = 0;
        foreach (var (name, test) in tests)
        {
            try
            {
                test();
                Console.WriteLine($"PASS {name}");
            }
            catch (Exception ex)
            {
                failed++;
                Console.WriteLine($"FAIL {name}: {ex.Message}");
            }
        }
        Console.WriteLine($"\n{tests.Length - failed}/{tests.Length} tests passed.");
        return failed == 0 ? 0 : 1;

        void CheckDeposit(IPaymentChannel channel, Func<decimal, decimal> policy, decimal balance, decimal fee)
        {
            var result = start.Deposit(200m, channel, policy);
            Equal(balance, result.Updated.Amount);
            Equal(fee, result.Commission);
            Equal(1000m, start.Amount);
        }
    }

    private static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new Exception($"Expected {expected}, received {actual}.");
    }

    private static void Throws<TException>(Action action) where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }
        throw new Exception($"Expected {typeof(TException).Name}.");
    }
}
