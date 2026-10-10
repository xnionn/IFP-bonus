# IFP Bonus Task

**Student:** Alimzhan Almukhambetov  
**Group:** IT-2510

A C# console application that demonstrates interfaces, an immutable payment balance record, and commission policies passed as delegates.

## Run

Install the .NET 8 SDK, open this directory, and run:

```bash
dotnet build --configuration Release
dotnet run --configuration Release --no-build
dotnet run --configuration Release --no-build -- --test
```

The final command runs 27 dependency-free regression checks and returns a nonzero exit code if any fail. No external NuGet packages are required.

## Assignment coverage

| Requirement | Implementation |
| --- | --- |
| Two implementations of `IPaymentChannel` | `CardChannel` accepts positive amounts up to a caller-defined maximum; `TransferChannel` accepts amounts at or above a caller-defined positive minimum. |
| Immutable balance | `PaymentBalance` retains the provided positional record and returns a new value using `with`. |
| Behaviors passed as delegates | `Func<decimal, decimal>` policies: `a => a * 0.02m` and `_ => 5m`. |
| Separate validation | `PaymentValidation.ValidateDeposit` checks inputs and channel rules; `ValidateCommission` checks the returned fee. |
| All four combinations | `Program.cs` deposits 200 using each channel and each policy. |
| Consecutive deposits | The first result is passed into the second deposit: 1000 → 1196 → 1491. |
| Immutability and equality | Earlier amounts remain 1000 and 1196; separately constructed equal records compare `true` with `==` and `false` with `ReferenceEquals`. |
| Output helper | `ShowDeposit` performs, prints, and returns each successful deposit. |
| Deposit 3 with flat fee 5 | Reproduces the original formula's result of 998, then calls the fixed method, which rejects the excessive fee. |

## Results

Each independent case starts at 1000 and deposits 200.

| Channel | Commission policy | Commission | Updated balance |
| --- | --- | ---: | ---: |
| Card | 2 percent | 4 | 1196 |
| Card | Flat 5 | 5 | 1195 |
| Bank transfer | 2 percent | 4 | 1196 |
| Bank transfer | Flat 5 | 5 | 1195 |

Actual output is in [docs/demo-output.txt](docs/demo-output.txt). The local regression run passed **27/27** checks; its output is in [docs/test-results.txt](docs/test-results.txt). The included GitHub Actions workflow is configured to build the project and run the demo and checks on code changes.

Local verification used the .NET 8.0.425 SDK Roslyn compiler directly, with C# 12, nullable analysis, and warnings as errors; the executable ran on .NET 8.0.31. The local process-information API prevented the dotnet CLI/MSBuild from starting. The standard project build is therefore configured for GitHub Actions but was not run locally.

## The excessive commission bug

The starter expression computes `oldAmount + amount - commission`. For a deposit of 3 and a fee of 5, the returned balance is 2 lower. With a starting amount of 1000, it becomes 998. The original record itself remains unchanged, even in the starter version.

The fix requires `0 <= commission <= amount` before creating the new record. This also rejects negative fees. A commission equal to the deposit is permitted and gives a zero net credit. Rejecting equality as well would be a different business rule.

The net credit is calculated before adding it to the starting balance. This avoids an unnecessary intermediate overflow when the gross addition would overflow but the net addition would fit. A genuine decimal overflow still throws.

## Questions

### Could double replace decimal

Technically yes, after changing the relevant signatures and literals. However, `double` stores binary floating-point values and cannot exactly represent many decimal fractions, such as 0.1. `decimal` is more suitable for these monetary amounts and the 0.02 commission rate. It still has finite precision and a finite range. A real currency application also needs an explicit rounding rule.

This assignment preserves the supplied commission policies without rounding. `F2` in console output only formats the display; it does not change stored values. For example, 2 percent of 10.01 is stored as 0.2002.

### Why accept IPaymentChannel instead of a concrete type

`Deposit` needs only the channel contract, so it can work with both implementations without type checks or separate overloads. Another channel can be added without changing the deposit calculation. Tests can provide their own implementation. Commission selection remains an independent choice made by the caller.

### What problems can an immutable record introduce

The caller must keep the returned balance; ignoring it loses the update. Each successful deposit allocates a new record. Immutability protects a snapshot but does not make concurrent read-modify-write operations atomic, so a shared account would still need synchronization or a database transaction.

The supplied positional record has init-only properties, but records in general can be mutable. Immutability is shallow; the fields here are safe because they are a string and a decimal. The public positional constructor and `with` also allow invalid snapshots to be created externally, so this implementation checks the current state on entry to `Deposit`. It does not claim that construction itself enforces every invariant.

### Would a mutable class be better

For this assignment, I would keep the record. It preserves earlier balances and provides generated value equality, which the task explicitly asks to demonstrate. A mutable class could model one shared account entity, but other references would observe its changes and synchronization would still be necessary. An ordinary class uses reference equality by default; value equality can be implemented explicitly. A class can be immutable too, so mutability and the class-versus-record choice are separate design decisions.

## Self-assessment

Complete these personal Yes/No answers after reviewing the solution:

- Difference between class and record: Yes / No
- Passing behavior through delegates: Yes / No
- Purpose and advantages of interfaces: Yes / No
- Decomposition and when to use it: Yes / No

## Files

- `Program.cs`: the requested demonstration and output helper.
- `IPaymentChannel.cs`, `CardChannel.cs`, `TransferChannel.cs`: channel contract and implementations.
- `PaymentBalance.cs`: immutable state transition and result tuple.
- `PaymentValidation.cs`: input, channel, and commission checks.
- `PaymentTests.cs`: executable regression checks.
- `docs/Report_IFP_Bonus_Almukhambetov_IT2510.docx`: submission report.

## References

- Assignment source: the supplied `bonus_task(1).docx`.
- [Microsoft Learn: Records](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record)
- [Microsoft Learn: Floating-point numeric types](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/floating-point-numeric-types)
- [Microsoft Learn: Interfaces](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/interfaces)
