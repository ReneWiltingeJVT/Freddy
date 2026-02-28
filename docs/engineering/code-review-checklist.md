## Code Review Checklist

Use this checklist when reviewing pull requests.

### General

- [ ] PR has a clear title and description
- [ ] Changes match the PR description
- [ ] No unrelated changes mixed in
- [ ] Commit messages follow Conventional Commits

### Code Quality

- [ ] Code compiles without warnings
- [ ] No `TODO` or `HACK` comments (create issues instead)
- [ ] No magic strings or numbers
- [ ] Proper use of nullable reference types
- [ ] Guard clauses at method boundaries
- [ ] No unnecessary `async` (return Task directly when possible)

### Architecture

- [ ] Changes follow Clean Architecture dependency rules
- [ ] Controllers only dispatch via MediatR
- [ ] Business logic in Application layer, not in Api or Infrastructure
- [ ] New features have proper CQRS structure
- [ ] DTOs used at API boundary (no entities exposed)

### Testing

- [ ] New code has unit tests
- [ ] Test naming: `Method_Scenario_Expected`
- [ ] Tests cover happy path and error cases
- [ ] Golden test set still passes (AI changes)

### Security

- [ ] No secrets in code
- [ ] New endpoints have correct `[Authorize]` attributes
- [ ] Input validation on all user input
- [ ] No SQL injection risks (parameterized queries / EF Core)
- [ ] Sensitive data not logged

### API

- [ ] Endpoints follow REST conventions
- [ ] Proper HTTP status codes
- [ ] `[ProducesResponseType]` attributes present
- [ ] Pagination on list endpoints
- [ ] No breaking changes to existing endpoints

### AI-Specific

- [ ] Prompt changes tested against golden set
- [ ] Source attribution present in responses
- [ ] "I don't know" fallback works
- [ ] No hallucination regressions
- [ ] Temperature and token limits configured correctly

### Frontend (React)

- [ ] Components are functional with named exports
- [ ] No `any` types — `unknown` with narrowing instead
- [ ] Custom hooks extracted for reusable logic
- [ ] TanStack Query used for API calls (no raw fetch/useEffect)
- [ ] Tailwind utility classes used (no inline CSS, no CSS modules)
- [ ] Responsive design tested on mobile viewports
- [ ] Accessibility: ARIA attributes, keyboard navigation where needed
- [ ] Zod schema matches backend DTO contract
