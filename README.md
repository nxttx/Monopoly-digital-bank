# MonopolyBank

A Monopoly banking domain, built as a pairing experiment: the human writes the tests, Claude (AI) writes the implementation — strict TDD driving DDD.

## Projects

| Project | Purpose |
|---|---|
| `MonopolyBank.Domain` | The domain model. Core types at the root, roles in `Roles/`, exceptions in `Exceptions/`. |
| `MonopolyBank.Tests` | xunit.v3 + Shouldly test suite. The tests are the spec — production code exists only because a test demanded it. |

## The experiment (round two)

This branch is a clean-room rebuild: the domain implementation was deleted, and a fresh AI session regenerates it using only the test suite as the spec. The goal is to compare the independent design against the original (see PR #1) — how much of the architecture do the tests actually force?

## Running the tests

```powershell
dotnet run --project MonopolyBank.Tests
```

Note: plain `dotnet test` does **not** discover xunit.v3 tests; use `dotnet run` or Rider's test runner (continuous testing works natively).

## Collaboration rules

See [CLAUDE.md](CLAUDE.md) for the human/AI working agreement and the domain decision log.

## License

[PolyForm Noncommercial 1.0.0](LICENSE.txt) — use, modify, and share freely for any noncommercial purpose.
