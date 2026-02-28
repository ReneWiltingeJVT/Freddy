## Productdefinitie

### Doelgroep

| Segment | Beschrijving | Prioriteit |
|---------|-------------|------------|
| Primair | Zorgmedewerkers (verpleegkundigen, verzorgenden) | MVP |
| Secundair | Teamleiders / coördinatoren | MVP |
| Tertiair | Beheerders / kwaliteitsmedewerkers (backoffice) | Fase 3 |

### Probleemstelling

Zorgmedewerkers besteden kostbare tijd aan het zoeken naar protocollen, procedures en
formulieren in versnipperde systemen (intranet, mappen, mail). Dit leidt tot:

- **Tijdverlies**: gemiddeld 15-30 minuten per shift aan zoeken
- **Inconsistentie**: medewerkers handelen op basis van verouderde informatie
- **Onzekerheid**: "Doe ik dit wel volgens protocol?"
- **Drempel**: nieuw personeel kent de weg niet in het documentenlandschap

### Kernfunctionaliteit MVP

| # | Functie | Beschrijving |
|---|---------|-------------|
| 1 | Chat-interface | Natuurlijke taalinvoer in het Nederlands |
| 2 | Intent detection | Automatisch herkennen: FAQ, protocol-vraag, of aanvraag |
| 3 | FAQ/Template antwoorden | Directe antwoorden op veelgestelde vragen |
| 4 | RAG document retrieval | Relevante protocolfragmenten ophalen en samenvatten |
| 5 | Bronvermelding | Altijd tonen welk document het antwoord onderbouwt |
| 6 | Conversatiegeschiedenis | Eerdere vragen terugzien binnen een sessie |
| 7 | Authenticatie | Veilig inloggen met rolgebaseerde toegang |

### Wat valt buiten MVP?

| Onderdeel | Reden | Gepland in |
|-----------|-------|-----------|
| Web backoffice | Documenten handmatig laden volstaat | Fase 3 |
| Multi-tenant | Pilot is voor één organisatie | Fase 4 |
| Push notificaties | Nice-to-have | Fase 2 |
| Offline modus | Te complex | Fase 3 |
| App Store publicatie | PWA volstaat voor pilot | Fase 2 |
| Spraak-invoer | Geen core behoefte | Fase 4 |

### Succescriteria

- [ ] Relevant antwoord in <10 seconden
- [ ] 80%+ correcte antwoorden (pilot-feedback)
- [ ] Bronvermelding bij elk RAG-antwoord
- [ ] Expliciet "ik weet het niet" bij onbekende vragen
- [ ] <3 seconden gemiddelde responstijd
- [ ] 10-50 pilotgebruikers actief binnen 2 weken na launch
- [ ] Uptime >99%
