# THE ARTIFACT — Server

Azure Function that scans AI signal sources every 3 hours, analyzes them with OpenAI, scores anomalies, and writes results to blob storage as JSON.

## Architecture

```
GitHub Trending ─┐
HuggingFace    ──┤
arXiv papers   ──┼──→ OpenAI Analyzer ──→ Anomaly Scorer ──→ Blob Storage
X.com posts    ──┘                                            ├── YYYY-MM-dd-HH.json
                                                              └── latest.json
```

## Prerequisites

- .NET 10 SDK
- Azure Functions Core Tools v4 (`npm i -g azure-functions-core-tools@4`)
- Azurite for local blob storage (`npm i -g azurite`)
- Azure CLI (for deployment)

## Configuration

Copy and fill in your keys in `SignalEngine.Functions/local.settings.json`:

| Setting | Required | Description |
|---------|----------|-------------|
| `OPENAI_API_KEY` | Yes | OpenAI API key |
| `OPENAI_MODEL` | No | Model to use (default: `gpt-4o-mini`) |
| `X_BEARER_TOKEN` | No | X.com API bearer token (skipped if empty) |
| `SIGNAL_CONTAINER` | No | Blob container name (default: `signals`) |

## Local Development

1. Start Azurite (local blob storage emulator):
   ```bash
   azurite
   ```

2. Set your OpenAI API key in `local.settings.json`

3. Run the function:
   ```bash
   ./scripts/run-local.sh
   ```
   Or directly:
   ```bash
   cd SignalEngine.Functions
   func start
   ```

The timer trigger runs every 3 hours. To trigger manually during development, use the Azure Functions admin endpoint:
```bash
curl -X POST http://localhost:7071/admin/functions/SignalScan -H "Content-Type: application/json" -d "{}"
```

## Build

```bash
./scripts/build.sh
```

## Deploy to Azure

### 1. Deploy infrastructure (first time)

Create `infra/parameters.json` from `infra/parameters.example.json` and fill in your keys.

```bash
./scripts/deploy-infra.sh <resource-group-name> [location]
```

### 2. Deploy the function app

```bash
./scripts/deploy-app.sh <resource-group-name>
```

## Output Format

The function writes JSON to blob storage. The frontend reads from the public blob URL.

- `signals/latest.json` — most recent scan
- `signals/YYYY-MM-dd-HH.json` — historical scans

```json
{
  "scanTimestamp": "2026-03-04T21:00:00Z",
  "totalSignals": 45,
  "deepSignalCount": 2,
  "signals": [
    {
      "title": "example/repo",
      "description": "...",
      "url": "https://github.com/example/repo",
      "source": "GitHub",
      "field": "Synthetic Minds",
      "interpretation": "A new autonomous agent framework has emerged...",
      "novelty": 7.0,
      "momentum": 6.0,
      "depth": 8.0,
      "anomalyIndex": 7.9,
      "isDeepSignal": false,
      "timestamp": "2026-03-04T18:30:00Z"
    }
  ]
}
```

## Anomaly Scoring

```
AnomalyIndex = (Novelty × 0.5) + (Momentum × 0.3) + (Depth × 0.2)
Deep Signal threshold: AnomalyIndex >= 8.5
```
