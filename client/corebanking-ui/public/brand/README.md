# White-label branding

This portal is re-skinned **at runtime** — no rebuild required.

To brand the app for a bank, edit `public/branding.json` and (optionally) drop logo
assets into this `public/brand/` folder:

```json
{
  "bankName": "Acme Bank",
  "tagline": "Operations Console",
  "shortName": "AB",
  "primaryColor": "#0D9488",
  "secondaryColor": "#0F766E",
  "logoUrl": "/brand/acme-logo.svg",
  "logoIconUrl": "/brand/acme-icon.svg"
}
```

- **`primaryColor`** (required-ish): the entire light + dark MUI palette is derived from this
  single hex value. `secondaryColor` is optional.
- **`logoUrl`**: wide logo lockup shown in the sidebar header. When omitted, a tinted
  monogram tile + `bankName` is shown instead.
- **`logoIconUrl`**: square mark for compact/mobile contexts. Falls back to `logoUrl`,
  then to the monogram.

Any missing or malformed field falls back to a safe default, so a bad config never
prevents the app from booting. SVG logos are recommended (crisp at any size).
