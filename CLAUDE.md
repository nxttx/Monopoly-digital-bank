# MonopolyBank

A Monopoly banking domain, built with **DDD** and strict **TDD**. The user has Rider's continuous testing enabled, so tests run automatically on every change.

## Collaboration rules (strict — follow these)

1. **The user writes the tests; Claude writes the production code.**
2. Claude may fix *minor* test issues (missing usings, wrong Shouldly calls), but must **always ask first** — even in accept-edits or auto mode.
3. **Implement the bare minimum** to make the current failing tests pass. Never anticipate features not yet specified by a test.
4. **Refactor minimally.** Only refactor when a clear pattern or other good reason emerges — not freely.
5. **Claude maintains this file.** When a new rule is agreed or important project information is shared, update CLAUDE.md in the same turn without being asked.
6. **When the user says they'll fix a test, wait.** Don't implement against a test that's known to be in flux — let the user finish their fix first, then implement against the final red state.
7. **Git:** Claude may commit on its own judgment (a green, coherent state is a good moment), must inform the user each time it does, and must NEVER commit on `master`/`main` — always work on a feature branch.
8. **If Claude changed any test file, ask before committing** so the user can review the test changes first. Autonomous commits are only allowed when the diff touches no test code.
9. **Test changes and implementation are separate steps.** When asked to change tests, change ONLY the tests and stop — the user wants to see the red state first. Implement only when explicitly told to.
10. **Memory: Claude persists session knowledge in `CLAUDE_MEMORY.md`** (repo root, gitignored — NEVER commit it). Read it at the start of every session. Whenever durable knowledge appears — a decision, a user preference, project state, an experiment outcome — update it in the same turn without being asked. Keep it terse; prune stale entries. Rules and domain decisions belong in CLAUDE.md, everything else session-worthy goes there. If the file is missing, recreate it from what you know.
11. **Never open a PR without discussing it first.** Committing on a feature branch remains autonomous (rule 7), but creating a pull request — or anything else outward-facing on the remote — waits for explicit agreement from the user.

## Project structure

- `MonopolyBank.Domain/` — class library, the domain model. Production code goes here. Core types at the root (`Game`, `User`, `UserType`, `Transaction`, `BankCard`), roles in `Roles/`, exceptions in `Exceptions/`. Everything stays in the single namespace `MonopolyBank.Domain` regardless of folder (deliberate — don't "fix" to folder-matching namespaces).
- `MonopolyBank.Tests/` — xunit.v3 test project (namespace `MonopolyBank.Tests`), references Domain. User territory.
- `MonopolyBank.Api/` — ASP.NET Core Minimal API, references Domain. Handlers are public static methods returning the API's response family (`Endpoints/ApiResult.cs`): abstract `ApiResponse(HttpCall Http, Dictionary<string,Link> Actions)` with siblings `ApiResult<T>(…, T? Value, …)` for success and `ApiError(…, IReadOnlyList<FieldError> Errors, …)` for failure — sibling types make "an error carries no Value" a TYPE-level guarantee (the user's reflection guard in the tests pins this; never add a Value to ApiError). `Http` describes the exchange (`Location` route with trailing slash, `HttpMethod`, `System.Net.HttpStatusCode`), `Actions` holds named follow-up actions as `Link(Location, Method)` records (HATEOAS-style); collection items carry their own `Actions` (e.g. `GameSummary`). Handlers WITHOUT an error path keep the concrete `ApiResult<T>` return type; a handler's first asserted error widens it to `ApiResponse`, and its tests unwrap with `ShouldBeOfType<>`. **The envelope must not lie**: routes translate it into the real response via `ApiResultExtensions.ToHttpResult()` (wire status = `Http.StatusCode`, body = envelope), and each registration's verb/route must match `Http.Method`/`Http.Location`. Handler tests can't observe the wire, so this alignment is a convention Claude keeps by hand — check it whenever an endpoint is added or its `HttpCall` changes. Endpoints are organized per resource: each static endpoints class (`GamesEndpoints`, `GamesUsersEndpoints`) owns its handlers, DTO records, and a `RegisterApiRoutes(WebApplication)` called from `Program.cs` (pattern set by the user 2026-08-15; the temporary `/health` scaffold route is gone). No endpoint exists until a user test demands it. `GameStore` (singleton) holds live games in memory keyed by `Guid` and appends every successful mutation to `Persistence/CommandLog` (SQLite, append-only `game_commands` table); a cache-missed game is rehydrated by replaying its commands through the domain. Identity is an API-layer concept — the domain has no IDs.
- `MonopolyBank.Api.Tests/` — xunit.v3 + Shouldly test project, references Api. User territory. Tests call handler methods DIRECTLY (no WebApplicationFactory, no HTTP) with real dependencies (`new GameStore(new CommandLog("Data Source=:memory:"))`). API tests are handler **contract tests** — specify input → `ApiResult` envelope output (`Http.Location`/`Http.Method`, `Value`, `Links`); the store appears only to prep data (`CreateGameStore()` helper), never as the assertion target (agreed 2026-08-13 over a store-level-TDD alternative).
- `MonopolyBank.slnx` — solution file listing all four projects.

## Domain decisions

- `User` is an application-level concept (the person at the device); the *game domain* speaks in **Players** and the **Bank** — Monopoly's ubiquitous language never says "user".
- A `User` composes role objects: `user.Player` and `user.Banker` (role presence flags: `IsPlayer`/`IsBanker` from `UserType`).
- Every user/player has a mandatory non-empty name (`ArgumentException("Name cannot be empty.")`); there is no nameless construction.
- `User` rejects undefined `UserType` values (`Enum.IsDefined` guard → `ArgumentException("Invalid user type.")`) — C# enums accept any cast integer, and a role-less user (both flags false) must be unrepresentable.
- `Player` and `Banker` derive from abstract `Role`, which owns the has-role guard (`EnsureHasRole()` → `MissingRoleException`). Role objects are constructed only by `User` (internal ctors), except the public `Player(string name)`.
- Money lives on `Player`. Transfers target a `Player`, never a `User` (`Banker.TransferMoney(Player, int)`, `Player.TransferMoney(Player, int)`) — this keeps illegal states (paying a banker-only user) unrepresentable.
- The `Banker` has unlimited money: giving money doesn't decrease anything.
- Role guards are sender-side: transferring without the matching role throws `InvalidOperationException` (receiving is not guarded).
- No money moves before `game.Start()` (players AND banker): `GameNotStartedException`, covering both "no game" and "game not started". `Start()` requires ≥1 banker and ≥2 players (`AmountOfPlayersException`).
- Guard order in `Player.TransferMoney`: cross-game → game-started → role → negative amount → balance. Both roles carry an internal `Game?` link set by `Game.AddUser`.
- Transactions: `Game.Ledger` (internal) is the single source of truth — a `Transaction(Role From, Role To, int Amount)` record per money movement. `Player.History` is a filtered projection (entries where the player is From or To). Starting money is itself a ledger entry from the banker, written at `Start()` (and at `AddUser` for post-start joiners).
- Negative amounts: players cannot transfer them (no stealing); the Banker CAN — a negative bank transfer is how the bank collects taxes/fees.
- Invariant: a player's balance can never go negative — players can't overpay, and the bank can't collect more than the player has (both throw, balances untouched).
- Rule violations throw domain-specific exceptions deriving from `InvalidOperationException`: `MissingRoleException(role)`, `NegativeTransferException`, `InsufficientBalanceException`, `DuplicateBankerException`. Tests assert both the exception type and its message.
- `Game` was removed when its test was dropped — recreate it only when a test demands it.
- `Game.Started` and `Game.DefaultStartAmount` are public reads (widened from internal 2026-08-13 — the API's `GameDetails` needed them; setters stay domain-private).

## Tech notes

- .NET 10 (`net10.0`), nullable enabled, implicit usings.
- Tests: **xunit.v3** + **Shouldly** (`x.ShouldBe(...)`, not FluentAssertions' `x.Should().Be(...)`) + AutoFixture.
- Run tests from CLI with `dotnet run --project MonopolyBank.Tests` — plain `dotnet test` does NOT discover xunit.v3 tests (VSTest runner). Rider's continuous testing handles xunit.v3 natively.
- Gotcha: xunit.v3 generates an entry point under `<RootNamespace>.AutoGenerated`, so no class may share a name with the first segment after the root namespace (this forced the test namespace to `MonopolyBank.Tests` instead of a `Tests` class in namespace `MonopolyBank`).
- API tests run with `dotnet run --project MonopolyBank.Api.Tests` (same xunit.v3 quirk).
- API error mapping: errors RETURN an `ApiError`, they don't throw — `Errors` list of `FieldError(Field, Message)` (domain exception messages verbatim, e.g. "Name cannot be empty."), the error's `HttpStatusCode`, and recovery `Actions` (404: "Add game"; 400: retry "Add user"). Each mapping added only when a user test asserts it; the route gets a matching `.Produces<ApiError>(status)` entry. The wire serializes the RUNTIME type (`ToHttpResult` casts to object), so error JSON truly has no `value` key. Command-log caveats (accepted): replay re-runs domain guards (stricter future rules can make old logs unreplayable — `version` column exists for evolution) and BankCard data re-rolls on every replay.
- `CommandLog` keeps ONE open SQLite connection for its lifetime (required for `:memory:`; lock-serialized). Local `.db` files are gitignored.
