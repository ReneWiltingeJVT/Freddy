## Acceptatiecriteria

### Chat Functionaliteit

| # | Criterium | Verificatie |
|---|----------|------------|
| AC-01 | Gebruiker kan nieuwe conversatie starten | UI test |
| AC-02 | Gebruiker kan vraag stellen in Nederlands | UI test |
| AC-03 | Systeem geeft antwoord binnen 10 seconden | Performance test |
| AC-04 | Antwoord bevat bronvermelding | Automated test |
| AC-05 | Bij onbekende vraag: expliciet "ik weet het niet" | Golden test |
| AC-06 | Conversatiegeschiedenis is zichtbaar | UI test |
| AC-07 | Eerdere conversaties zijn opnieuw te openen | UI test |

### FAQ Matching

| # | Criterium | Verificatie |
|---|----------|------------|
| AC-10 | Exacte match retourneert template antwoord | Unit test |
| AC-11 | Fuzzy match (>0.85) retourneert antwoord met bevestiging | Unit test |
| AC-12 | Geen match leidt tot RAG pipeline | Integration test |

### RAG

| # | Criterium | Verificatie |
|---|----------|------------|
| AC-20 | Relevante chunks worden opgehaald (similarity >0.7) | Unit test |
| AC-21 | Antwoord is gebaseerd op opgehaalde context | Golden test |
| AC-22 | Bron-document wordt correct vermeld | Automated test |
| AC-23 | Bij geen relevante chunks: weigeringsrespons | Unit test |

### Security

| # | Criterium | Verificatie |
|---|----------|------------|
| AC-30 | Ongeauthenticeerde requests worden geweigerd (401) | API test |
| AC-31 | Gebruiker ziet alleen eigen conversaties | API test |
| AC-32 | Wachtwoord voldoet aan complexity rules | Unit test |
| AC-33 | JWT token verloopt na 15 minuten | Unit test |

### Performance

| # | Criterium | Verificatie |
|---|----------|------------|
| AC-40 | Gemiddelde responstijd <3 seconden (FAQ) | Load test |
| AC-41 | Gemiddelde responstijd <5 seconden (RAG) | Load test |
| AC-42 | Systeem blijft stabiel bij 50 concurrent users | Load test |
