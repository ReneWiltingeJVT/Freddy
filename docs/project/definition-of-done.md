## Definition of Done

Een feature is "Done" wanneer alle onderstaande criteria zijn vervuld.

### Code

- [ ] Code compileert zonder warnings
- [ ] Alle bestaande tests slagen
- [ ] Nieuwe code heeft unit tests (>80% coverage voor nieuwe code)
- [ ] Code voldoet aan formatting rules (`dotnet format --verify-no-changes`)
- [ ] Geen nieuwe analyzer warnings
- [ ] XML documentation op publieke API's
- [ ] Geen TODO's of HACK comments in gemerged code

### Review

- [ ] Pull request is gereviewed en goedgekeurd
- [ ] PR description beschrijft wat en waarom
- [ ] Geen gevoelige data in commits (secrets, wachtwoorden)

### Testing

- [ ] Unit tests geschreven en passing
- [ ] Integration tests waar van toepassing
- [ ] Handmatige test op mobiel device (voor UI features)
- [ ] Golden test set nog steeds passing (voor AI wijzigingen)

### Documentatie

- [ ] API documentatie bijgewerkt (XML comments → OpenAPI)
- [ ] README bijgewerkt als setup-stappen veranderen
- [ ] ADR aangemaakt voor architectuurbeslissingen

### Deployment

- [ ] Feature werkt in Docker Compose omgeving
- [ ] Geen breaking changes in API (of nieuwe versie)
- [ ] Database migratie is reversible
