# Security Policy

## Scope

Freddy verwerkt zorgprotocollen en organisatiedocumenten. Hoewel er geen patiëntgegevens
worden verwerkt, nemen we security serieus gezien de zorgsector-context.

## Ondersteunde Versies

| Versie | Ondersteund |
|--------|------------|
| main (latest) | ✅ |
| Oudere releases | ❌ |

## Kwetsbaarheid Melden

**Meld kwetsbaarheden NIET via publieke GitHub Issues.**

Stuur een e-mail naar: **security@freddy.nl**

Vermeld:

- Beschrijving van de kwetsbaarheid
- Stappen om te reproduceren
- Mogelijke impact
- Eventuele suggestie voor fix

We streven ernaar binnen **48 uur** te reageren en binnen **7 dagen** een eerste beoordeling
te delen.

## Security Maatregelen

- TLS 1.3 verplicht voor alle communicatie
- JWT Bearer authenticatie met korte token-levensduur
- Wachtwoorden opgeslagen met Argon2id (ASP.NET Identity)
- Alle AI-verwerking lokaal (geen data naar externe API's)
- Audit logging op alle gevoelige operaties
- Dependency scanning via Dependabot

Zie [docs/04-security-compliance.md](docs/04-security-compliance.md) voor het volledige
security-ontwerp.
