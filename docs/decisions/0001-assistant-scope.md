# 0001 — DeskMate is a desktop assistant, not just an animated pet

- **Status:** Accepted
- **Date:** 2026-11-11

## Context

DeskMate is not simply an animated character viewer. The character is the visual interface of an assistant capable of interacting with its user and, through integrations, invoking existing tools and AI agents.

## Decision

- Build an **extensible personal desktop assistant**.
- Keep **assistant behavior and integrations separate from the animated character**.
- Keep the name and personality of the character independent of **DeskMate**, the project/repository name.
- Prefer connecting to existing tools and AI agents rather than rebuilding their capabilities.
- Do not tie the assistant to one AI provider, CLI, or renderer.

Repository description:

> An extensible desktop assistant that connects to AI agents and tools through an interactive, animated character.

## Consequences

The assistant's logic, agent integrations, and animation can evolve independently. The project is not a pet simulator and does not require an AI agent or voice engine to be built from scratch.

## Possible future extensions — not committed features

Voice interaction, text-to-speech, coding agent integrations (such as Claude Code or Codex), reactions to system events, and configurable character appearance or personality. Exact integrations and supported actions remain to be decided.
