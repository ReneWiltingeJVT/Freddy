## API Guidelines

### General Rules

- RESTful design with resource-oriented URLs
- URL versioning: `/api/v1/resource`
- JSON request/response bodies
- HTTP status codes used correctly
- `CancellationToken` on all async endpoints

### URL Conventions

```
GET    /api/v1/conversations              → List
POST   /api/v1/conversations              → Create
GET    /api/v1/conversations/{id}         → Get by ID
PUT    /api/v1/conversations/{id}         → Update
DELETE /api/v1/conversations/{id}         → Delete
GET    /api/v1/conversations/{id}/messages → Nested resource
```

### Status Codes

| Code | When |
|------|------|
| 200 OK | Successful GET, PUT |
| 201 Created | Successful POST (with Location header) |
| 204 No Content | Successful DELETE |
| 400 Bad Request | Validation errors |
| 401 Unauthorized | Missing/invalid authentication |
| 403 Forbidden | Authenticated but not authorized |
| 404 Not Found | Resource does not exist |
| 429 Too Many Requests | Rate limit exceeded |
| 500 Internal Server Error | Unhandled exception |

### Pagination

All list endpoints support pagination:

```
GET /api/v1/conversations?page=1&pageSize=20
```

Response includes pagination metadata:

```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "totalCount": 42,
  "totalPages": 3
}
```

Defaults: `page=1`, `pageSize=20`, max `pageSize=100`.

### Error Format (RFC 7807)

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Validation Error",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "errors": {
    "content": ["Message content is required."]
  }
}
```

### Versioning Policy

- Additive changes within existing version (new fields, new endpoints)
- Breaking changes require new version (`/api/v2/`)
- Never remove or rename existing fields in current version
- Deprecation notice 1 version before removal

### Documentation

- XML comments on all controller actions
- `[ProducesResponseType]` attributes for OpenAPI
- Swagger UI enabled in Development
