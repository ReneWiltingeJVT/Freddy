# Git Workflow

## Branch Strategy

We use a **trunk-based development** model with short-lived feature branches.

### Rules

1. **Never push directly to `main`** — all changes go through a pull request.
2. **One branch per feature/fix** — keep branches focused and small.
3. **Self-review before merging** — even solo, review your own diff.
4. **Delete branches after merge** — keep the repo clean.

### Branch Naming

| Type | Pattern | Example |
|------|---------|---------|
| Feature | `feature/<short-description>` | `feature/package-routing` |
| Bug fix | `fix/<short-description>` | `fix/ollama-timeout` |
| Chore | `chore/<short-description>` | `chore/update-dependencies` |
| Docs | `docs/<short-description>` | `docs/api-documentation` |

## Commit Messages

We follow **Conventional Commits** (`type(scope): description`).

### Types

| Type | When |
|------|------|
| `feat` | New feature or capability |
| `fix` | Bug fix |
| `refactor` | Code change that neither fixes a bug nor adds a feature |
| `docs` | Documentation only |
| `test` | Adding or updating tests |
| `chore` | Build, CI, tooling changes |

### Examples

```text
feat(packages): add package-driven routing model
fix(ollama): increase HTTP timeout for cold starts
docs(readme): update quick start instructions
test(chat): add handler tests for package routing
```

### Rules

- Use **imperative mood** ("add", not "added" or "adds").
- Keep the subject line under **72 characters**.
- Reference the issue number if applicable: `feat(packages): add routing (#12)`.

## Pull Request Checklist

Before merging, verify:

- [ ] `dotnet build Freddy.sln` — 0 errors, 0 warnings
- [ ] `dotnet test Freddy.sln` — all tests pass
- [ ] Migrations included if schema changed
- [ ] No secrets or credentials in code
- [ ] PR description explains **what** and **why**

## Workflow Commands

```bash
# 1. Create a branch
git checkout -b feature/my-feature

# 2. Work, commit incrementally
git add -A
git commit -m "feat(scope): description"

# 3. Push and create PR
git push -u origin feature/my-feature
# Then open PR on GitHub

# 4. After merge, clean up
git checkout main
git pull
git branch -d feature/my-feature
```
