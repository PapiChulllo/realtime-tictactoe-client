# Realtime Tic-Tac-Toe Client

**A Unity UI client for two-player, server-authoritative tic-tac-toe.** It connects over Unity Transport, stores the assigned player number, renders the 3×3 board from server state, and sends move requests when it is that client’s turn.

**Paired repository:** [realtime-tictactoe-server](https://github.com/PapiChulllo/realtime-tictactoe-server)

---

## How it works

1. On connect, the server assigns `PLAYER|1` or `PLAYER|2` and sends the board state.
2. The client wires a `Button_<x>_<y>` grid; clicks send `MOVE|<player>|<x>|<y>` when local turn checks pass.
3. Board / turn / status UI update from state messages. `WIN` / `DRAW` briefly set result text, but the following final state message typically replaces it with **Game Over!** (observed behavior from the client’s state handler).

**Status / limitations:** educational prototype. Default server address is `127.0.0.1` port **9001**. Two Editor instances (or two checkouts) are needed for a full match. No reconnect; server does not reclaim slots. Client `ResetGame` is **local UI only** — it does not reset authoritative server state. Build Settings scene list empty. No automated tests; Unity unavailable for re-verification in this documentation pass.

## Tech stack

| Area | What it uses |
|---|---|
| Engine | **Unity** `2022.3.46f1` (project under `TicTacToeClient/`) |
| Networking | **Unity Transport** `2.4.0` |
| Endpoint | UDP **9001**, default IP `127.0.0.1` in `NetworkClient.cs` |
| UI | uGUI buttons + TextMesh Pro status |
| Encoding | Unicode + length prefix; reliable sequenced pipeline |

## What's in the project

| System | Key files |
|---|---|
| Connection, protocol parse, grid wiring, move requests, UI | `TicTacToeClient/Assets/_Scripts/NetworkClient.cs` |
| Configured 3×3 button grid + status | `TicTacToeClient/Assets/Scenes/SampleScene.unity` |
| Packages / project settings | `TicTacToeClient/Packages/`, `TicTacToeClient/ProjectSettings/` |

One authored client script (~9.3 KB) covers networking and presentation.

### Code / system highlights

- **Turn gating:** client blocks obvious out-of-turn / post-game clicks; server remains authoritative.
- **Board render:** parses three semicolon-separated rows of comma-separated cell values into button labels / interactable state.
- **Result flicker:** `WIN`/`DRAW` handlers set result text, then the next board state update overwrites status with game-over messaging.

### Message protocol

| Direction | Payload | Client behavior |
|---|---|---|
| Server → client | `PLAYER\|<1-or-2>` | Store assigned number |
| Server → client | board state CSV | Render cells + turn/status |
| Client → server | `MOVE\|<player>\|<x>\|<y>` | Request clicked cell |
| Server → clients | `WIN\|…` / `DRAW` | Brief result; often overwritten by following state |

## Scenes

| Scene | Purpose |
|---|---|
| `TicTacToeClient/Assets/Scenes/SampleScene.unity` | Client UI — Play after the server is listening; second Editor instance for Player 2 |

## Run (Editor)

1. Open the [server](https://github.com/PapiChulllo/realtime-tictactoe-server) `TicTacToeServer/` project, `SampleScene`, Play.
2. Open this repo’s `TicTacToeClient/` with the same Editor version, `SampleScene`, Play (Player 1).
3. Second client checkout/Editor for Player 2. For a remote host, change `IPAddress` in `NetworkClient.cs`.

## Third-party assets

Unity packages only. Authored work is `NetworkClient.cs` plus the configured SampleScene UI.

## About this repository

Public educational / portfolio showcase under **PapiChulllo**. Pair with [realtime-tictactoe-server](https://github.com/PapiChulllo/realtime-tictactoe-server). Claims stay within committed source behavior.
