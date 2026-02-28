## ADR-0003: Mobile Stack Keuze

**Status:** Superseded by [ADR-0004](0004-frontend-framework-choice.md)
**Datum:** 2026-02-28
**Besluitnemers:** Architectuurteam

### Context

Freddy moet beschikbaar zijn op iOS en Android. De teamgrootte (solo .NET developer)
en tijdlijn (4-6 weken MVP) beperken de opties.

### Overwogen Opties

| Optie | Leercurve | Time-to-market | Code hergebruik | Native features |
|-------|-----------|---------------|----------------|----------------|
| **Blazor WASM PWA** | Geen (C#) | Snel | Web+Mobile | Beperkt |
| **MAUI Blazor Hybrid** | Laag (C#) | Medium | Web+Mobile+Native | Volledig |
| **React Native** | Hoog (JS/React) | Langzaam | Geen met .NET | Volledig |
| **Flutter** | Hoog (Dart) | Langzaam | Geen met .NET | Volledig |
| **.NET MAUI (XAML)** | Medium (XAML) | Medium | Geen met web | Volledig |

### Oorspronkelijk Besluit

~~Fase 1 (MVP): Blazor WebAssembly PWA. Fase 2: MAUI Blazor Hybrid wrap.~~

**Vervangen door [ADR-0004](0004-frontend-framework-choice.md):** React 19 + TypeScript +
Vite als frontend framework, met PWA via vite-plugin-pwa.

### Reden voor vervanging

- Figma-design beschikbaar dat 1:1 moet worden overgenomen
- Blazor heeft vrijwel geen Figma-to-code tooling
- Developer heeft React/TypeScript ervaring
- React ecosysteem is 10-20x groter (componenten, AI code generation, community)
- Zie [ADR-0004](0004-frontend-framework-choice.md) voor volledige analyse
