# Product Requirements Document (PRD)

## Project: THE ARTIFACT

**Theme:** Soundwave – Signal vs Noise
**Type:** Interactive Artifact / Signal Detection Instrument
**Goal:** Build a mysterious machine that detects and interprets anomalous signals in the AI ecosystem.

---

# 1. Product Overview

**THE ARTIFACT** is a web-based interactive artifact that behaves like a scientific instrument discovered in ancient ruins. The device scans the modern AI landscape (GitHub, X, arXiv, etc.) and detects **disturbances in the intelligence field**.

Rather than presenting a dashboard or news feed, the system visualizes signals as **tremors on a seismograph-style instrument**. Each tremor corresponds to a new development in AI.

The machine interprets signals using a fictional but grounded scoring system and translates them into human-readable explanations.

The experience should feel like **operating a mysterious machine**, not browsing software.

---

# 2. Core Concept

The system analyzes recent AI-related signals and evaluates them based on three properties:

* **Novelty** – How new or unusual the concept is
* **Momentum** – How quickly attention around it is growing
* **Depth** – How technically significant it appears

These signals combine into an **Anomaly Index**, which determines the magnitude of the disturbance visualized on the instrument.

Large disturbances represent rare and important developments called **Deep Signals**.

---

# 3. Product Goals

### Primary Goal

Create a memorable artifact that demonstrates the theme **Signal vs Noise** by detecting meaningful anomalies in the AI ecosystem.

### Secondary Goals

* Make AI discovery feel mysterious and atmospheric
* Demonstrate AI-assisted categorization and interpretation
* Present information in a novel way rather than a typical dashboard
* Keep the UI extremely simple and focused

---

# 4. User Experience

## Opening Experience

User lands on a dark interface showing a mysterious machine.

Centered text:

```
THE ARTIFACT
awaiting scan
```

A single control is visible:

```
SCAN SIGNALS
```

---

## Scan Sequence

When the user initiates a scan:

1. Device powers on
2. Seismograph begins moving
3. Signals begin appearing as tremors
4. Tremor magnitude corresponds to anomaly score
5. Large signals trigger **Deep Signal events**

---

## Interaction

User can click any signal spike.

The machine translates the signal into an interpretation.

Example:

```
DISTURBANCE DETECTED

Field: Synthetic Minds
Source: GitHub

Interpretation:
A new open-source reasoning model has appeared.
Early signals suggest unusual benchmark performance.

Signal Strength: ▇▇▇▇▇
```

---

# 5. UI Structure

Single-screen layout.

```
 ┌───────────────────────────────┐
 │                               │
 │        SIGNAL DISPLAY         │
 │                               │
 │        Seismograph Drum       │
 │                               │
 ├───────────────┬───────────────┤
 │               │               │
 │ SIGNAL        │ MACHINE       │
 │ TRANSLATOR    │ CONTROLS      │
 │               │               │
 └───────────────┴───────────────┘
```

---

## 5.1 Signal Display

Primary visual instrument.

Features:

* scrolling seismograph
* signal spikes representing events
* spikes clickable
* large spikes represent Deep Signals

---

## 5.2 Signal Translator

Displays machine interpretation of selected signal.

Content includes:

* detected field
* signal source
* machine interpretation
* signal strength indicator

The voice should feel like the **machine speaking**.

---

## 5.3 Machine Controls

Minimal controls only.

Controls include:

```
SCAN SIGNALS
```

Optional controls:

Signal Sensitivity

```
surface chatter
minor disturbance
deep signals
```

Field Filters

```
logic field
vision field
synthetic minds
machine labor
```

---

# 6. Signal Sources

Possible data sources:

* GitHub trending repositories
* X.com AI-related posts
* arXiv papers
* HuggingFace model releases
* AI newsletters or RSS feeds

For the prototype, a smaller set is acceptable.

Recommended initial source:

**GitHub Trending + AI repositories**

---

# 7. Signal Categorization

Each signal is categorized into a field.

Example fields:

```
Logic Field (reasoning models)
Vision Field (multimodal / vision)
Synthetic Minds (agents)
Machine Labor (automation tools)
Training Methods
Evaluation Systems
```

Classification handled via LLM prompt.

---

# 8. Anomaly Scoring Model

Signals are scored using three factors.

### Novelty

Measures conceptual uniqueness.

Scale:

```
incremental → 2
variation → 5
new idea → 8
paradigm shift → 10
```

---

### Momentum

Measures how fast the signal is spreading.

Signals include:

* GitHub star velocity
* social mentions
* repost activity

Scale:

```
quiet → 1
discussion → 4
rapid spread → 7
explosive interest → 10
```

---

### Depth

Measures technical significance.

Signals include:

* benchmark improvements
* research citations
* model scale

Scale:

```
casual project → 2
useful tool → 5
serious research → 8
breakthrough → 10
```

---

### Anomaly Index Formula

```
AnomalyIndex =
(Novelty × 0.5)
+ (Momentum × 0.3)
+ (Depth × 0.2)
```

---

### Deep Signal Threshold

```
AnomalyIndex ≥ 8.5
```

Triggers a **Deep Signal event**.

These should appear rarely.

---

# 9. System Architecture

Simplified pipeline:

```
Data Sources
    ↓
Signal Fetcher
    ↓
LLM Analyzer
    ↓
Anomaly Scoring
    ↓
Signal Renderer
```

---

## Components

### Signal Fetcher

Collects items from sources.

Example fields:

* title
* description
* link
* source
* timestamp
* engagement metrics

---

### LLM Analyzer

Classifies signal and estimates novelty.

Example prompt:

```
Classify this AI-related item into a field and estimate novelty.
Return JSON:
field
novelty_score
summary
```

---

### Anomaly Scorer

Calculates:

```
Novelty
Momentum
Depth
AnomalyIndex
```

---

### Signal Renderer

Visualizes spikes on seismograph.

Spike height = anomaly index.

---

# 10. Visual Design

Mood inspiration:

* Myst
* submarine sonar rooms
* analog scientific instruments

Key elements:

* dark background
* glowing indicators
* brass / mechanical aesthetic
* subtle motion
* faint grid lines

The device should feel **ancient but functional**.

---

# 11. Constraints

To maintain scope:

* single page
* no accounts
* no persistence required
* minimal controls
* no complex filtering

Focus on **experience over completeness**.

---

# 12. Success Criteria

Submission is successful if:

* the artifact feels like a **machine**
* signals are visually compelling
* anomaly detection feels believable
* the UI is extremely simple
* the concept communicates **signal vs noise**

---

# 13. Future Extensions (Optional)

If time permits:

* additional signal sources
* audio feedback for signals
* rare "Deep Signal" cinematic effects
* historical signal replay

These are not required for MVP.

---

# 14. MVP Scope

Must include:

* signal fetch
* anomaly scoring
* seismograph visualization
* signal interpretation panel
* scan interaction

Everything else is optional.

---

# Final Vision

The user should feel like they have discovered a forgotten instrument designed to measure disturbances in the field of intelligence.

Most scans reveal only faint noise.

But occasionally…

The machine detects something deeper.

```
DEEP SIGNAL DETECTED
```
