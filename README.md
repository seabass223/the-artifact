# The Artifact

> *An ancient instrument that measures disturbances in the intelligence field.*

The Artifact is an immersive, interactive signal detector for the AI ecosystem. It scans GitHub, arXiv, Hugging Face, and X.com in real time, analyzes developments with GPT-4o, scores their anomaly potential, and synthesizes a spoken narrative via ElevenLabs — all delivered through an atmospheric single-page experience.

Built for an AI competition.

[Interact with The Artifact](https://theartifacge57227mft7u6.z13.web.core.windows.net/)

---

## What It Does

Every day (or on demand), The Artifact:

1. **Fetches signals** from GitHub trending repos, arXiv ML papers, Hugging Face, and X.com
2. **Analyzes each signal** with OpenAI — assigning a field (e.g. *Vision Field*, *Synthetic Minds*), an interpretation, and novelty/momentum/depth scores
3. **Computes an Anomaly Index** — `(Novelty × 0.5) + (Momentum × 0.3) + (Depth × 0.2)` — signals scoring ≥ 8.5 become rare *Deep Signals*
4. **Summarizes findings** into a 3-paragraph narrative
5. **Generates audio** via ElevenLabs with character-level timing data
6. **Classifies the narrative** into sections synced to audio playback timestamps
7. **Caches everything** to Azure Blob Storage for the frontend to consume

The frontend plays back the audio with a polar waveform visualization and real-time classified text that appears and disappears in sync with the narration.

---

## Tech Stack

**Frontend**
- Vanilla JS (ES modules), HTML5, CSS3
- Web Audio API — polar waveform canvas visualization
- No frameworks, no build step

**Backend**
- .NET 10 / C# — Azure Functions (isolated worker)
- OpenAI API (`gpt-4o-mini`) for analysis, summarization, and classification
- ElevenLabs API (`eleven_flash_v2_5`) for text-to-speech with timing alignment
- Azure Blob Storage for caching reports, audio, and classification data
- Application Insights for monitoring

**Infrastructure**
- Azure Functions (Consumption plan)
- Azure Storage (public blob container for frontend reads)
- Bicep IaC — fully reproducible deployment

**Data Sources**
- GitHub API (trending AI/LLM repos, past 7 days)
- arXiv API (recent ML papers)
- Hugging Face API
- X.com API (optional, requires bearer token)

---

## Architecture

```
Signal Sources (GitHub, arXiv, HuggingFace, X)
         │
         ▼
[Azure Function] Fetch & deduplicate signals
         │
         ▼
[OpenAI] Analyze: field + interpretation + scores
         │
         ▼
[Azure Blob] Cache daily report JSON
         │
         ▼
[OpenAI] Summarize into 3-paragraph narrative
         │
         ▼
[ElevenLabs] TTS → MP3 + character-level timing
         │
         ▼
[OpenAI] Classify narrative into timed sections
         │
         ▼
[Azure Blob] Public URLs → Frontend
         │
         ▼
[Browser] Audio playback + synced waveform + text
```

---

## Running Locally

**Prerequisites:** .NET 10 SDK, Azure Functions Core Tools, Azurite

1. Start local blob storage:
   ```bash
   azurite
   ```

2. Configure `server/SignalEngine.Functions/local.settings.json`:
   ```json
   {
     "IsEncrypted": false,
     "Values": {
       "AzureWebJobsStorage": "UseDevelopmentStorage=true",
       "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
       "OPENAI_API_KEY": "...",
       "OPENAI_MODEL": "gpt-4o-mini",
       "ELEVEN_LABS_API_KEY": "...",
       "ELEVEN_LABS_VOICE_KEY": "...",
       "SIGNAL_CONTAINER": "signals"
     }
   }
   ```

3. Run the function app:
   ```bash
   cd server
   ./scripts/run-local.sh
   ```

4. Trigger a scan manually:
   ```bash
   curl -X GET http://localhost:7071/api/orchestrate/flow1
   ```

5. Open `index.html` in a browser (serve from a local HTTP server to avoid CORS issues).

---

## Deploying to Azure

**Prerequisites:** Azure CLI, an Azure subscription

1. Create `server/infra/parameters.json` (copy from `parameters.example.json`) and fill in your API keys.

2. Deploy infrastructure (first time):
   ```bash
   cd server
   ./scripts/deploy-infra.sh <resource-group-name> [location]
   ```
   This creates: Storage Account, Blob Container, App Service Plan, Function App, Application Insights.

3. Deploy the function app:
   ```bash
   ./scripts/deploy-app.sh <resource-group-name>
   ```

4. Point the frontend at the blob URL output from step 2 — update `SIGNALS_BASE_URL` in `js/ui.js`.

The timer trigger runs daily at **00:00 UTC**. The HTTP endpoint is also available:
```
GET https://<function-app-name>.azurewebsites.net/api/orchestrate/flow1
```

---

## Project Structure

```
the-artifact/
├── index.html              # Single-page frontend
├── css/                    # Modular CSS (base, scene, ui, animations)
├── js/                     # Frontend modules
│   ├── main.js             # Boot sequence
│   ├── ui.js               # Scan logic, audio playback, classification sync
│   ├── scene.js            # Hotspot positioning (1536×1024 native coords)
│   ├── preloader.js        # Typewriter narrative + image preload
│   └── waveform.js         # Polar canvas audio visualizer
├── images/                 # Scene images, icons
└── server/
    ├── SignalEngine.Functions/
    │   ├── Functions/       # Azure Function entry points
    │   └── Services/        # Fetchers, OpenAI, ElevenLabs, BlobStorage
    ├── infra/
    │   ├── main.bicep       # Azure infrastructure (IaC)
    │   └── parameters.json  # API keys + config (git-ignored)
    └── scripts/             # deploy-infra.sh, deploy-app.sh, run-local.sh
```

---

## Environment Variables

| Variable | Description |
|---|---|
| `OPENAI_API_KEY` | OpenAI API key |
| `OPENAI_MODEL` | Model name (default: `gpt-4o-mini`) |
| `ELEVEN_LABS_API_KEY` | ElevenLabs API key |
| `ELEVEN_LABS_VOICE_KEY` | ElevenLabs voice ID |
| `X_BEARER_TOKEN` | X.com bearer token (optional) |
| `SIGNAL_CONTAINER` | Blob container name (default: `signals`) |
