# Sequence Game in Unity

A portfolio-focused implementation of the board game **Sequence**, built in **Unity** as a way to learn and demonstrate practical game architecture, multiplayer systems, state management, networking, and production-oriented software engineering.

> **Current status:** Local gameplay and lobby foundation are working. The next major phase is authoritative multiplayer networking.

---

## Project Motivation

This project is intentionally more than a recreation of a board game.

I am building it as a hands-on software engineering project to practice the kinds of problems that appear in real multiplayer applications:

- separating player identity, seat order, and team ownership
- managing shared and private game state
- validating legal actions
- designing deterministic turn flow
- building reusable UI systems
- handling configurable game rules
- preparing a local game architecture for network authority
- planning persistence and home-hosted deployment

The goal is to evolve the project from a local Unity prototype into a complete networked application that can be demonstrated as a portfolio project.

---

## Tech Stack

### Current

- **Unity**
- **C#**
- **TextMeshPro**
- Unity UI
- Event-driven game state updates
- Scriptable/prefab-based UI composition

### Planned

- Multiplayer networking with an **authoritative server**
- Per-player private hand state
- Persistent player/session data
- Home-hosted server deployment
- Dockerized services where appropriate
- Optional backend/service experimentation in **C++**
- Optional AI-assisted game or player features after the core multiplayer system is stable

---

# Current Progress

## 1. Core Board

The game board is generated programmatically as a **10 × 10 Sequence board**.

Implemented:

- correct Sequence card layout
- four wild/free corner spaces
- card artwork loaded through Unity resources
- board cells represented independently from their visual components
- chip ownership stored by **team**
- occupied cells remain interactive when required for Jack behavior

The board separates:

```text
BoardCell      -> game state
BoardCellView  -> Unity visual representation
BoardManager   -> board rules and interaction logic
```

This separation is important for the future networking phase because the visual board should reflect authoritative state rather than own the state itself.

---

## 2. Card and Deck System

Implemented:

- standard playing-card suits and ranks
- two complete decks
- **104-card deck**
- shuffled deck
- dealing based on player count
- replacement card draw after a successful move

Supported hand sizes:

| Players | Cards per Player |
|---:|---:|
| 2 | 7 |
| 3–4 | 6 |
| 6 | 5 |
| 8–9 | 4 |
| 10–12 | 3 |

---

## 3. Hand Interaction

The player hand is generated dynamically in the Unity UI.

Implemented:

- runtime card UI creation
- selectable cards
- selected-card visual state
- legal board-position highlighting
- played card removal
- replacement card draw
- current player's hand refresh when the turn changes

The local version currently swaps the visible hand between players on one machine. This is temporary.

For multiplayer, each client will only receive and display its own private hand.

---

## 4. Jack Rules

Both Jack behaviors are implemented.

### Two-Eyed Jacks

**JC / JD**

- can place a team chip on any empty non-corner board space
- valid targets can be highlighted

### One-Eyed Jacks

**JH / JS**

- remove an opponent team's chip
- cannot remove a chip belonging to the current player's team
- cannot remove a chip that belongs to a completed Sequence
- valid targets can be highlighted

These rules are enforced by `BoardManager`, rather than only by the UI.

---

## 5. Dead Card Replacement

A normal card is considered dead when both matching board spaces are already occupied.

Implemented:

- dead-card detection
- one dead-card replacement per turn
- replacement does **not** consume the player's normal turn
- Jacks are never treated as dead cards
- replacement state resets when the turn advances

---

# Team, Seat, and Player Architecture

One of the most important refactors in the project was separating three concepts that are easy to incorrectly combine:

```text
PlayerId  = who the player is
SeatIndex = where the player sits / turn position
TeamId    = which team owns the player's actions
```

This makes the architecture much more suitable for multiplayer.

A player can join the lobby with one identity, take any available seat, and inherit the team associated with that seat.

Turn order is based on:

```text
SeatIndex
```

not:

```text
PlayerId
```

This means player identity can eventually come from a network connection/account without affecting deterministic turn order.

---

## Team Assignment

Teams alternate by seat.

Example: **6 players / 2 teams**

```text
Seat 1 -> Red
Seat 2 -> Blue
Seat 3 -> Red
Seat 4 -> Blue
Seat 5 -> Red
Seat 6 -> Blue
```

Example: **6 players / 3 teams**

```text
Seat 1 -> Red
Seat 2 -> Blue
Seat 3 -> Green
Seat 4 -> Red
Seat 5 -> Blue
Seat 6 -> Green
```

---

# Lobby System

A configurable local lobby is now implemented.

Players can:

- select the number of players
- select a valid team configuration
- select a seat
- see their assigned team
- mark themselves ready/unready
- start only when the lobby is valid

Supported player counts:

```text
2, 3, 4, 6, 8, 9, 10, 12
```

The team selector automatically prevents invalid configurations.

| Players | Valid Team Counts |
|---:|---|
| 2 | 2 |
| 3 | 3 |
| 4 | 2 |
| 6 | 2 or 3 |
| 8 | 2 |
| 9 | 3 |
| 10 | 2 |
| 12 | 2 or 3 |

---

## Seat Availability

Seat selection is synchronized across the local lobby state.

When a player takes a seat:

- that seat remains visible in that player's own dropdown
- the same seat disappears from every other player's dropdown
- changing/leaving the seat makes it available again

This prevents duplicate seat assignment at the UI layer while the lobby manager also validates seat ownership.

---

## Circular Seat Visualization

The lobby includes a circular seat map.

It displays:

- seat order
- occupied vs. available seats
- player identity
- team-colored player rings
- ready-state indicators

The circular layout is generated mathematically so it can adapt to different supported player counts.

Empty seats are shown as neutral circles without unnecessary text.

This visualization also makes the eventual network lobby easier to understand because the visual seating order directly corresponds to gameplay turn order.

---

# Game Session Configuration

The lobby does not directly create gameplay state.

Instead, it builds a validated:

```text
GameSessionConfig
```

containing:

```text
PlayerId
SeatIndex
TeamId
```

`GameManager` consumes that configuration when the match begins.

This boundary is intentional:

```text
Lobby
   |
   v
GameSessionConfig
   |
   v
GameManager
```

It provides a clean transition point for future networking, where the server will eventually produce/validate the authoritative match configuration.

---

# Turn Flow

The current local flow is:

```text
Select card
    |
    v
Show legal moves
    |
    v
Place / remove chip
    |
    v
Validate Sequence creation
    |
    v
Consume played card
    |
    v
Draw replacement
    |
    v
Advance by SeatIndex
    |
    v
Show next player's hand
```

Turn order wraps back from the final seat to Seat 1.

---

# Sequence Detection

Sequence detection currently checks all four directions:

- horizontal
- vertical
- diagonal `\`
- diagonal `/`

A Sequence is a valid five-cell line belonging to the same team, with corner spaces acting as wild spaces.

A single move can complete more than one Sequence when the geometry allows it.

---

## Sequence Overlap Rule

The current/default rule is:

> Any pair of completed Sequences may share at most **one cell**.

Therefore:

```text
0 shared cells -> valid
1 shared cell  -> valid
2+ shared cells -> invalid
```

This applies across horizontal, vertical, and diagonal Sequences.

Two diagonal Sequences, for example, can cross at a single cell and both count.

### Planned Hard / Strict Mode

A future optional ruleset will make overlap more restrictive.

In the planned strict mode, once a completed Sequence has already used one shared cell, another cell from that same Sequence will not be reusable as the shared cell of another Sequence.

The current pairwise rule will remain the normal/default mode.

---

# Win Conditions

The number of Sequences required to win depends on both **player count** and **team count**.

| Players | Teams | Sequences Required |
|---:|---:|---:|
| 2 | 2 | 3 |
| 3 | 3 | 3 |
| 4 | 2 | 3 |
| 6 | 2 | 3 |
| 6 | 3 | 3 |
| 8 | 2 | 2 |
| 9 | 3 | 2 |
| 10 | 2 | 2 |
| 12 | 2 | 2 |
| 12 | 3 | 1 |

When a team reaches the required number:

- the game enters the game-over state
- the board is locked
- winner UI is displayed
- normal board interaction stops

---

# Debug / Development Tools

The project includes a small permanent development harness.

Currently:

```text
F1 -> create the next valid debug Sequence
      for the current player's team
```

The debug path supports up to three generated Sequences so that different win-condition branches can be tested quickly.

This is useful for testing rules such as:

```text
6 players / 3 teams -> win on Sequence 3
9 players / 3 teams -> win on Sequence 2
12 players / 3 teams -> win on Sequence 1
```

The debug tooling is kept separate from normal gameplay and is intended only for Editor/development builds.

---

# Current Architecture

```text
                         +------------------+
                         |   LobbyManager   |
                         +--------+---------+
                                  |
                                  v
                         +------------------+
                         | GameSessionConfig|
                         +--------+---------+
                                  |
                                  v
+---------------+        +------------------+        +---------------+
|  HandManager  | <----> |   GameManager    | <----> | BoardManager  |
+---------------+        +------------------+        +-------+-------+
                                                           |
                                                           v
                                                    +-------------+
                                                    | Board / Cell|
                                                    +-------------+
```

Responsibilities:

### `LobbyManager`

Owns:

- lobby players
- seat assignment
- readiness
- valid lobby configuration

### `GameManager`

Owns:

- game session
- deck
- players
- turns
- hand dealing
- win flow

### `BoardManager`

Owns:

- legal board actions
- Jack behavior
- dead-card board checks
- team chip ownership
- Sequence detection
- Sequence protection

### `HandManager`

Owns:

- hand presentation
- card selection
- dead-card replacement request UI

This separation is being maintained intentionally so the project can move toward server-authoritative multiplayer without rewriting the entire game.

---

# Current Development Status

## Completed / Working Locally

- [x] 10 × 10 Sequence board
- [x] board card layout
- [x] two-deck card system
- [x] dynamic hand sizes
- [x] hand UI
- [x] card selection
- [x] legal move highlighting
- [x] normal card placement
- [x] two-eyed Jack behavior
- [x] one-eyed Jack behavior
- [x] dead-card replacement
- [x] team-based board ownership
- [x] seat-based turn ordering
- [x] Sequence detection
- [x] completed-Sequence chip protection
- [x] Sequence overlap validation
- [x] configurable win conditions
- [x] 2-team and 3-team configurations
- [x] lobby player/team configuration
- [x] ready/unready system
- [x] occupied-seat filtering
- [x] circular seat visualization
- [x] lobby-to-game transition
- [x] game status / winner UI
- [x] development Sequence test harness

---

# Roadmap

## Phase 1 — Local Game Foundation

**Status: mostly complete**

Focus:

- gameplay rules
- board state
- lobby state
- team/seat architecture
- UI validation
- configurable rules

---

## Phase 2 — Multiplayer Architecture

**Next**

The next major task is moving from one-machine local state to a networked model.

Planned architecture:

```text
Client
   |
   | requests
   v
Authoritative Server
   |
   | validates
   v
Shared Match State
   |
   +----> Client A
   +----> Client B
   +----> Client C
```

The server should become authoritative for:

- lobby membership
- seat assignment
- readiness
- deck order
- card dealing
- current turn
- board state
- legal moves
- Sequence registration
- winner detection

Clients should request actions rather than directly modify game state.

---

## Phase 3 — Private Player State

A major networking requirement is keeping hands private.

Planned rule:

```text
Server knows every hand.

Player A receives Player A's hand.
Player B receives Player B's hand.

Shared clients receive only public information
about other players.
```

This is one of the reasons the current `PlayerId / SeatIndex / TeamId` separation was implemented early.

---

## Phase 4 — Persistence

Planned:

- player profiles
- persistent identity
- match/session records
- reconnect support
- game history
- statistics

---

## Phase 5 — Home-Hosted Deployment

The target is to host the multiplayer service on personal/home-lab infrastructure.

Potential work includes:

- dedicated server build
- Docker deployment
- networking/firewall configuration
- logging
- health checks
- persistent storage
- monitoring

---

## Phase 6 — Advanced Modes and Portfolio Extensions

Possible additions:

- strict/hard Sequence overlap mode
- configurable rule presets
- spectator mode
- match replay
- AI-assisted opponent/player features
- server telemetry
- analytics dashboard
- automated tests
- CI/CD
- backend-service experimentation in C++

---

# Engineering Goals

The project is being used to practice and demonstrate:

### Software Architecture

- separation of concerns
- state ownership
- event-driven systems
- reusable components
- clear data boundaries

### Multiplayer Systems

- server authority
- state synchronization
- private vs. public state
- deterministic turn flow
- lobby management
- reconnection and persistence

### Backend / Systems Engineering

Planned future work:

- persistent services
- networking
- server deployment
- Docker
- logging
- telemetry
- home-lab hosting

### Game Development

- Unity UI
- prefab workflows
- board-game state machines
- turn-based systems
- rule validation
- player feedback

---

# Why This Project Matters in My Portfolio

Many portfolio games focus primarily on visuals or isolated gameplay mechanics.

For this project, I am intentionally focusing on the **engineering underneath the game**.

The board game provides a constrained ruleset, while the implementation creates opportunities to work on:

- multiplayer architecture
- authoritative state
- identity and session management
- distributed application boundaries
- private player data
- persistence
- testing
- deployment

The long-term goal is for the repository to show not only a playable Unity game, but also the engineering process used to evolve a local prototype into a networked, deployable system.

---

# Screenshots / Demo

> Screenshots and gameplay video will be added as the UI and networking phases mature.

Suggested future media:

```text
docs/
├── lobby.png
├── circular-seat-map.png
├── board-gameplay.png
├── sequence-detection.png
├── multiplayer-demo.gif
└── architecture-diagram.png
```

---

# Repository Structure

The exact structure will continue to evolve, but the project currently follows a separation similar to:

```text
Assets/
├── Prefabs/
│   └── UI/
│
├── Resources/
│   └── Cards/
│
└── Scripts/
    ├── Board/
    ├── Cards/
    ├── Debug/
    ├── Game/
    ├── Lobby/
    └── UI/
```

---

# Current Focus

The local lobby and gameplay foundations are now stable enough to begin the next phase:

> **Convert the current architecture into an authoritative multiplayer model without allowing clients to directly own or mutate shared game state.**

That transition is the next major engineering milestone for this repository.

---

## License

A license will be selected before public release.

---

## Author

**Mohammad Jahed Murad Sunny**

Computer Science Ph.D. Student  
Software Engineering • Game Development • Multiplayer Systems • Mixed Reality • Data Systems
