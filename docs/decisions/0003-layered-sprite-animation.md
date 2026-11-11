# 0003 — Compose the character from independently animated 2D sprite layers

- **Status:** Accepted
- **Date:** 2026-11-11

## Context

A small animated assistant should feel expressive and a little old-school while remaining inexpensive to render and easy to enrich. Drawing every combination as a complete animation would multiply art assets and maintenance effort.

## Decision

Use a **modular 2D sprite character** with these five independently animated parts:

1. **Body** — posture, locomotion, gestures and actions.
2. **Head** — direction, inclination and head movement.
3. **Eyes** — gaze, blinking and expressions.
4. **Mouth** — expressions, speech shapes and visemes.
5. **Accessories** — contextual objects or visual elements.

Layers compose in a common coordinate system with stable **attachment points / anchors** and, where useful, **pivots**. Assets must be designed for this composition so joints, occlusion, shadows and proportions remain coherent.

### Animation behavior

- A small **top-level state machine** selects the overall assistant behavior (illustrative states: `idle`, `listening`, `speaking`, `working`, `success`, `error`).
- Individual parts retain their own animation timelines; e.g. the body types, the head tilts, the eyes blink, and the mouth speaks **at the same time**.
- Distinguish **functional** animations (real work/status), **expressive** reactions, and **ambient/decorative** idle behaviors.
- Support prioritization, transitions, interruption, and optional rare/random ambient animations without making the engine complicated.
- Sprite sheets and simple animation metadata are preferred; new animations should normally be **data/assets**, not new rendering code.
- Load and cache assets judiciously: many possible animations do not require keeping every sprite sheet resident in memory.

### Speech

Use a separate mouth layer for lip sync. The mouth animation remains part of DeskMate's presentation model; speech synthesis and audio playback are accessed through replaceable ports (see [0005](0005-ports-and-adapters.md) and [0006](0006-voice-and-speech.md)).

- A basic fallback can choose mouth-open shapes from **audio amplitude**.
- A richer implementation can map **time-coded visemes** from a speech synthesizer to mouth sprites.
- Multiple phonemes may share a viseme: no one-sprite-per-letter requirement.
- Speech timing should not block body, head, or eye animation.

### Out of scope for the initial design

- 3D character rigs or physics engines.
- A full skeletal animation framework.
- Producing all possible body/face combinations as separate sprite sheets.

## Consequences and open details

The animation system is reusable and extensible, with a visually lively character and a modest runtime footprint. It does, however, require carefully authored layers and alignment conventions.

The exact sprite atlas dimensions, number of frames, art pipeline, metadata format, frame rate, TTS provider and viseme set are **not fixed**. The first proof of concept should demonstrate an idle character composed from the five layers in a transparent WPF window.
