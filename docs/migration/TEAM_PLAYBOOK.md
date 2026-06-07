# Fineract → CoreBanking Migration Team

A team of four specialised subagents (in `.claude/agents/`) that migrate and modernise Apache Fineract
modules into CoreBanking. This playbook is how they work together.

Source: `/Users/mac/Documents/Projects/fineract` → Target: this repo (CoreBanking).

## The team

| Agent | Role | Writes code? | Model | Tools |
|---|---|---|---|---|
| `migration-adviser` | Delivery lead / orchestrator brain. Sequences slices, reviews each increment for fidelity + delivery, makes the ship/next call. | No (plan + review) | opus | read-only |
| `migration-architect` | Technical-design authority. Designs the target shape (aggregates, CQRS/event/topic model, file structure) and reviews architectural integrity. | No (design + review) | opus | read-only |
| `migration-po` | Product Owner. Turns a Fineract feature into a precise functional spec + acceptance criteria + slice backlog. | No (spec only) | opus | read-only |
| `migration-dba` | Oracle DBA. Migrates DB structure: maps Fineract's schema to CoreBanking's Oracle conventions, authors the EF config + migration, reviews the generated DDL. | Yes (persistence) | sonnet | read/write/bash |
| `migration-dev` | Developer. Implements ONE slice's domain/app/api logic, test-first, following the `corebanking-feature` skill. | Yes | sonnet | read/write/bash |
| `migration-tester` | QA. Verifies behavioural parity with Fineract + runs the gates, adversarially. | Tests only | sonnet | read/write/bash |

## Who orchestrates

**The main thread orchestrates.** In Claude Code a subagent cannot dispatch another subagent, so the adviser
can't literally drive the others — it produces the plan and the verdicts, and the main Claude session
dispatches the team in order. Think of the adviser as the architect you consult, and the main thread as the
delivery lead who runs the loop.

## The loop (per module)

```
1. PLAN      main thread → migration-adviser (Mode A)
             → high-level source map, which service(s), ordered slice backlog, modernisation decisions.
             (Persist substantial plans under docs/superpowers/plans/.)

  ── for each slice in the backlog ──────────────────────────────
2. SPEC      main thread → migration-po
             → functional spec + acceptance criteria for this slice (rules, error codes, edge cases, examples).

2b. DESIGN   (for structurally significant slices: new aggregate/service/event/topic)
             main thread → migration-architect
             → technical design: aggregate/VO decomposition, command/query + event/topic signatures,
               consistency strategy, API surface, file structure. (Hands the column-level data model to the DBA.)

3a. SCHEMA   (only if the slice persists new state) main thread → migration-dba
             → EF config + DbContext registration + generated migration; Oracle DDL reviewed and applies clean.
             Runs BEFORE the dev so the table exists to build on. Touches Persistence/ only.

3b. BUILD    main thread → migration-dev   (pass the slice + the PO spec + Fineract refs + the DBA's table shape)
             → domain/app/api implementation, test-first, gates run, committed. (Touches Domain/App/Api +
               repository methods + outbox map — different files than the DBA, so no conflict.)

4. VERIFY    main thread → migration-tester (pass the slice + acceptance criteria)
             → parity tests + gate output + divergences, with real evidence.
             If the tester finds real defects → back to migration-dev (step 3b), or migration-dba if the
             defect is in the schema/migration, with the findings. Re-verify.

5. REVIEW    main thread → migration-architect (architectural review of the diff) for structurally
             significant slices, then → migration-adviser (Mode B) (pass the diff + tester report +
             architect's review)
             → verdict (ship / fix-then-ship / rework) + the next slice.
  ───────────────────────────────────────────────────────────────

6. REPEAT    until the module's backlog is done, then back to step 1 for the next module.
```

### Dispatch tips for the main thread

- Give each agent **only what it needs**: dev and tester get the specific slice + the PO spec + Fineract
  citations, not the whole module. Tight context = focused agents.
- Run `migration-po` and `migration-adviser` (Mode A) as read-only planning passes before any code changes.
- Keep slices small — a slice is one coherent change (one command/query + its plumbing), like a single task in
  the savings plan. If a slice balloons, ask the adviser to re-split.
- One `migration-dev` at a time per service (parallel devs on the same files conflict). Independent slices in
  different services *can* run in parallel.
- The tester must distinguish "verified green" from "couldn't run (Docker down)" — never let the two blur.

## Conventions every team member shares

All four read the CoreBanking source of truth: `CLAUDE.md`, `ARCHITECTURE.md`, the `corebanking-feature` skill
(`~/.claude/skills/corebanking-feature/`), and the worked example
`docs/superpowers/plans/2026-06-07-savings-transactions-interest-posting.md`. Migration means **port the
behaviour faithfully and modernise the mechanism**: Fineract in-process events → outbox + Kafka; JPA/Liquibase
→ EF Core migrations; `DataValidatorBuilder` → FluentValidation; cross-module calls → events + local read
models; Java idioms → idiomatic C#. Fineract enum/status/error ids are preserved.

## Running it as an automated workflow (optional)

The loop above is a fan-in/fan-out pipeline and can be run as a single deterministic Workflow
(adviser → per-slice [po → architect → dba (if persistence) → dev → tester → architect review] → adviser review) instead of manual dispatch. That spawns many
agents and spends real tokens, so it's opt-in: ask for it explicitly (e.g. "run the migration team as a
workflow on the savings charges module") and the main thread will author and run it.
