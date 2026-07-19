# MonopolyBank

A Monopoly banking domain, built as a pairing experiment: the human writes the tests, Claude (AI) writes the implementation — strict TDD driving DDD.

## Projects

| Project | Purpose |
|---|---|
| `MonopolyBank.Domain` | The domain model. Core types at the root, roles in `Roles/`, exceptions in `Exceptions/`. |
| `MonopolyBank.Tests` | xunit.v3 + Shouldly test suite. The tests are the spec — production code exists only because a test demanded it. |

## The domain in one minute

A `User` is the person at the table; the game itself speaks in **roles**: `user.Player` (owns money, a name, and a fake `BankCard`) and `user.Banker` (the bank). A `Game` holds the users and enforces the table rules:

- One banker per game; a game needs one banker and two players to `Start()`.
- No money moves before the game starts, and never between different games.
- A player's balance can never go negative — no overdrafts, no negative transfers, and the bank can't collect more than a player has. The bank's own money is unlimited.
- Every movement (including starting money) is a `Transaction` in the game's ledger. `player.History` shows a player's own entries; the banker also sees `GlobalHistory` and all `Players`.
- The banker configures the game before it starts: `SetStartAmount` (default 1500) and `SetCurrency` (default Monopolonian).

Rule violations throw domain-specific exceptions (`InsufficientBalanceException`, `DuplicateBankerException`, …).

## Running the tests

```powershell
dotnet run --project MonopolyBank.Tests
```

Note: plain `dotnet test` does **not** discover xunit.v3 tests; use `dotnet run` or Rider's test runner (continuous testing works natively).

## Collaboration rules

See [CLAUDE.md](CLAUDE.md) for the human/AI working agreement and the domain decision log.

## License

[PolyForm Noncommercial 1.0.0](LICENSE.txt) — use, modify, and share freely for any noncommercial purpose.
