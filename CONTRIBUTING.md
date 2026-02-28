# Contributing to Freddy

Bedankt voor je interesse in het bijdragen aan Freddy!

## Development Workflow

### Branching Strategie

We gebruiken **GitHub Flow** met de volgende conventies:

| Branch | Doel |
|--------|------|
| `main` | Altijd deployable, beschermd |
| `feature/<beschrijving>` | Nieuwe functionaliteit |
| `bugfix/<beschrijving>` | Bug fixes |
| `docs/<beschrijving>` | Documentatie wijzigingen |
| `chore/<beschrijving>` | Tooling, CI, refactoring |

### Commit Conventions

We volgen [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>(<scope>): <beschrijving>

[optionele body]

[optionele footer]
```

**Types:**

| Type | Wanneer |
|------|---------|
| `feat` | Nieuwe functionaliteit |
| `fix` | Bug fix |
| `docs` | Documentatie |
| `refactor` | Code refactoring zonder gedragswijziging |
| `test` | Tests toevoegen of wijzigen |
| `chore` | Build, CI, tooling |
| `perf` | Performance verbetering |

**Voorbeelden:**

```
feat(chat): add conversation history endpoint
fix(rag): improve chunk relevance threshold
docs(adr): add ADR-0004 for caching strategy
test(auth): add JWT validation edge cases
chore(ci): add markdown lint step
```

### Pull Request Process

1. Maak een branch vanaf `main`
2. Maak je wijzigingen (kleine, focused commits)
3. Zorg dat CI slaagt (`dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`)
4. Open een PR met het [PR template](.github/pull_request_template.md)
5. Minstens 1 review approval vereist
6. Squash merge naar `main`

### Code Standards

Zie [docs/engineering/standards.md](docs/engineering/standards.md) voor volledige standaarden.

Kort:

- C# 13, nullable reference types aan
- File-scoped namespaces
- CQRS via MediatR — geen directe DB access in controllers
- FluentValidation voor input validatie
- Alle async methoden met `CancellationToken`
- XML documentation op publieke API's

### Testing

- Unit tests met xUnit + Moq
- Naming: `MethodName_Scenario_ExpectedBehavior`
- Golden test set voor AI/prompt kwaliteit in `tests/Freddy.AI.Tests/`
- Run: `dotnet test` of `./tools/test.ps1`
