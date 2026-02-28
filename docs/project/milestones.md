## Milestones

### M1 — Foundation (Week 1-2)

**Doel:** Werkende backend met authenticatie, database en Docker setup.

**Definition of Done:**

- [ ] Solution compileert zonder warnings
- [ ] Docker Compose start PostgreSQL + Ollama + Seq
- [ ] Login endpoint retourneert JWT token
- [ ] CI pipeline draait (build + test + format)
- [ ] Database migrations werken

### M2 — AI Core (Week 3)

**Doel:** Werkende RAG pipeline die vragen kan beantwoorden.

**Definition of Done:**

- [ ] 20 documenten ingested in pgvector
- [ ] FAQ matching werkt voor 10+ templates
- [ ] RAG retourneert antwoord met bronvermelding
- [ ] "Weet ik niet" fallback werkt
- [ ] Responstijd <5 seconden

### M3 — Chat API (Week 4)

**Doel:** Volledige chat-functionaliteit via API.

**Definition of Done:**

- [ ] Conversaties aanmaken, bekijken, berichten sturen
- [ ] Intent detection routeert correct (FAQ vs RAG)
- [ ] Bronvermelding bij elk antwoord
- [ ] Auditlog voor chat acties

### M4 — Frontend Launch (Week 5)

**Doel:** Werkende React PWA installeerbaar op telefoon.

**Definition of Done:**

- [ ] Chat interface volledig functioneel
- [ ] Login/register flow werkt
- [ ] PWA installeerbaar op iOS + Android
- [ ] Responsive op mobile devices
- [ ] Conversatiegeschiedenis zichtbaar

### M5 — Pilot Ready (Week 6)

**Doel:** Productie-deployment klaar voor pilotgebruikers.

**Definition of Done:**

- [ ] Deployed op Hetzner VPS
- [ ] HTTPS met geldig certificaat
- [ ] 50 golden tests slagen
- [ ] Backups geconfigureerd
- [ ] Pilot users aangemaakt
- [ ] Onboarding documentatie voor pilotgebruikers
