# Frontend redesign — white-label modern banking portal

**Date:** 2026-06-29
**Target:** `client/corebanking-ui` (React 18 + Vite + TypeScript + MUI v6)

## Goal

Redesign the CoreBanking staff portal into a modern banking app with a configurable,
**white-label** brand (color + logo + name) so the solution can be re-skinned per bank
with no rebuild, plus a polished **light + dark** theme and a richer, chart-driven dashboard.

## Decisions (from brainstorming)

- **Themes:** both light and dark, with a toggle in the top bar (persisted, honors OS preference on first visit).
- **Scope:** full redesign — theme, app shell, dashboard, and every page (clients, products, accounts, wizard).
- **Brand:** configurable per bank (color, logo, name) — not a fixed accent.
- **White-label mechanism:** runtime `public/branding.json` fetched at startup → theme generated from it. One build, many banks.
- **Dashboard:** add charts (Recharts).

## Architecture

### 1. Branding system (`src/branding/`)
- `BrandConfig` type: `{ bankName, shortName, primaryColor, secondaryColor, logoUrl, logoIconUrl }`.
- `loadBranding()` fetches `public/branding.json`; on failure returns built-in defaults (app never hard-fails).
- `BrandProvider` loads config before first paint (splash), exposes `useBrand()`.
- `public/brand/` holds logo assets. Re-skin = drop in `branding.json` + logos.
- `<BrandLogo />` renders configured logo (full lockup / icon variants) with typographic fallback.

### 2. Theme system (`src/theme/`)
- `createAppTheme(mode, brand)` derives full light/dark palettes from `brand.primaryColor`.
- `ColorModeProvider` + `useColorMode()`; persisted to `localStorage`; honors `prefers-color-scheme`.
- Refined tokens + component overrides (cards, buttons, chips, tables, inputs, drawer, app bar) that read correctly in both modes.

### 3. App shell
- **Sidebar:** brand logo lockup, grouped nav, crisp active/hover states, accent rail on active item.
- **Top bar:** light/dark toggle + user menu; logo moves to sidebar; theme-aware.

### 4. Dashboard
- Add `recharts`. Greeting hero → 4 refined stat cards → balance line/area chart + account-status donut (from accounts data) → role-gated quick actions → recent applications list.

### 5. Pages
- Lists (clients/products/accounts): modern tables, hover, status chips, empty states.
- Detail pages: cleaner summary cards; account transaction history tidied.
- Account-opening wizard: refreshed stepper + review.
- Unauthorized page: branded.

## Guardrails (YAGNI)

- No routing, API, data-model, or auth behavior changes. Uncommitted `AuthProvider.tsx` left functionally as-is.
- One new dependency: `recharts`. Branding/theming reuse MUI + emotion.
- Charts use existing data only. Balance "trend" is a snapshot-derived view when no time-series exists; status donut is exact.
