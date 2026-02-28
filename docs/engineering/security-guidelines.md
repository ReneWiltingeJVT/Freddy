## Security Guidelines

### Authentication

- All endpoints require `[Authorize]` by default
- Use `[AllowAnonymous]` explicitly and sparingly
- JWT Bearer tokens with RS256 signing
- Access token TTL: 15 minutes
- Refresh token TTL: 7 days (rotated on use)

### Authorization

- Claims-based via ASP.NET Core policies
- Never check roles in business logic — use policies
- Define policies in central registration

### Input Validation

- FluentValidation on all commands
- Sanitize all user input before LLM processing
- Maximum input lengths on all string fields
- Reject HTML/script injection

### Data Protection

- Never log passwords, tokens, or full chat content
- Mask PII in logs (email → `r***@example.com`)
- Use `[DataProtection]` for sensitive config values
- Connection strings in environment variables, not appsettings

### HTTP Security

- HTTPS only (redirect HTTP → HTTPS)
- HSTS header with 1-year max-age
- CORS restricted to known origins
- X-Content-Type-Options: nosniff
- X-Frame-Options: DENY
- Content-Security-Policy header

### Rate Limiting

- Chat endpoint: 10 requests/minute per user
- Auth endpoints: 5 requests/minute per IP
- Use ASP.NET Core built-in rate limiting middleware

### Dependencies

- Dependabot enabled for automated updates
- Review dependency licenses
- Pin major versions in Directory.Build.props

### Secrets Management

- Development: `dotnet user-secrets`
- Production: environment variables
- Never commit secrets to git
- `.gitignore` includes `appsettings.*.local.json`
