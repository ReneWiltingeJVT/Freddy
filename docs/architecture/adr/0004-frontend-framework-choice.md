## ADR-0004: Frontend Framework Keuze — React + TypeScript

**Status:** Accepted
**Datum:** 2026-02-28
**Besluitnemers:** Architectuurteam
**Vervangt:** [ADR-0003](0003-mobile-stack-choice.md)

### Context

Freddy heeft een uitgewerkt Figma-design dat zo getrouw mogelijk als frontend moet
worden geïmplementeerd. De developer heeft ervaring met zowel .NET als React/TypeScript.
De oorspronkelijke keuze (Blazor WASM PWA, ADR-0003) werd heroverwogen omdat:

1. Er **geen serieuze Figma-to-code tooling** bestaat voor Blazor
2. Het Blazor UI-componentenecosysteem **beperkt** is (MudBlazor, Radzen)
3. AI-code-generatie (v0, Bolt, Copilot) **dominant React genereert**, niet Blazor
4. De developer **React/TypeScript ervaring** heeft

### Overwogen Opties

| Optie | Figma tooling | Ecosysteem | AI codegen | Leercurve | PWA |
|-------|--------------|-----------|-----------|-----------|-----|
| **React + TS + Vite** | Excellent | Enorm (~40% markt) | Best | Geen (ervaring) | vite-plugin-pwa |
| **Blazor WASM** | Geen | Klein (~3% markt) | Beperkt | Geen (C#) | Ingebouwd |
| **Vue + TS** | Goed | Groot (~20% markt) | Goed | Laag | vite-plugin-pwa |
| **Angular** | Redelijk | Groot (~15% markt) | Matig | Medium | @angular/pwa |
| **Flutter Web** | Matig | Niche | Beperkt | Hoog (Dart) | Ingebouwd |

### Besluit

**React 19 + TypeScript + Vite 6** als frontend framework, met:

- **Tailwind CSS v4** voor styling (1:1 Figma vertaling)
- **shadcn/ui + Radix** voor toegankelijke UI-componenten
- **TanStack Query v5** voor server state management
- **React Router v7** voor routing
- **React Hook Form + Zod** voor formulieren en validatie
- **vite-plugin-pwa** voor PWA-functionaliteit (installeerbaar, service worker)
- **ky** als HTTP client

### Motivatie

1. **Figma-to-code**: React heeft de beste tooling — Figma Dev Mode, Locofy, Anima,
   Builder.io genereren allemaal React/Tailwind code
2. **AI code generation**: v0.dev, Bolt.new, Lovable, en GitHub Copilot genereren
   allemaal hoogwaardige React code
3. **Ecosysteem**: shadcn/ui biedt ongestylde componenten die exact naar Figma-design
   gestyled kunnen worden (in tegenstelling tot opinionated libraries)
4. **Bestaande ervaring**: developer heeft React/TypeScript ervaring, geen leercurve
5. **Onderhoud**: React is het meest gebruikte frontend-framework — grootste community,
   meeste Stack Overflow antwoorden, eenvoudigst om hulp te vinden
6. **Tailwind + Figma**: Figma design tokens vertalen direct naar Tailwind config

### Frontend-Backend Communicatie

De React SPA communiceert via HTTPS/JSON met de ASP.NET Core REST API.
De backend blijft 100% ongewijzigd — alle CQRS handlers, DTOs en endpoints werken
identiek ongeacht het frontend framework.

```text
apps/
  Freddy.Web/          → React 19 + Vite + TypeScript project
    src/
      components/       → Herbruikbare UI-componenten (shadcn/ui based)
      features/         → Feature-modules (chat, auth, documents)
      hooks/            → Custom React hooks
      lib/              → Utility functies, API client
      types/            → TypeScript types (mirroring backend DTOs)
    public/             → Static assets, PWA manifest
    index.html
    vite.config.ts
    tailwind.config.ts
    tsconfig.json
    package.json
```

### Hosting

De React-app wordt gebuild tot static files en geserveerd door **Nginx** in dezelfde
Docker Compose stack op de Hetzner VPS. Nginx fungeert als:

- Static file server voor de React build (`/`)
- Reverse proxy naar de ASP.NET Core API (`/api`)

### Fase 2 Mobiel Pad

In plaats van MAUI Blazor Hybrid: de PWA behouden als die voldoet, of optioneel
**React Native (Expo)** met gedeelde TypeScript types en hooks.

### Consequenties

- **Positief**: beste Figma-to-code pipeline, enorm ecosysteem, AI-generatie optimaal
- **Positief**: developer heeft bestaande React/TS ervaring
- **Positief**: shadcn/ui componenten zijn 1:1 aanpasbaar aan elk design
- **Negatief**: tweede tech-stack naast .NET (Node.js toolchain voor build)
- **Negatief**: TypeScript types moeten handmatig synchroon blijven met C# DTOs
- **Mitigatie**: OpenAPI/Swagger schema + code generation voor TS types (optioneel)
- **Mitigatie**: CI pipeline bevat aparte frontend build + lint stap
