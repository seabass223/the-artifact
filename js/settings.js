/**
 * settings.js
 * Hardcoded feature flags and tuning values.
 * Change DUST_ENABLED to false to completely disable the particle system.
 */

export const SETTINGS = {
  // ── Dust / Sand particle system ─────────────────────────────────────────
  DUST_ENABLED: true,

  // Number of simultaneous particles. 200–300 is imperceptible CPU overhead.
  DUST_PARTICLE_COUNT: 350,

  // Base horizontal wind speed in px/frame (at 60 fps).
  // Higher = faster drift. 0.8–2.0 is a believable desert breeze.
  DUST_WIND_SPEED: 2.1,

  // Maximum alpha any particle can reach. Keep low for subtlety (0.0–1.0).
  DUST_OPACITY_MAX: 0.32,

  // ── Wind → Dust coupling ─────────────────────────────────────────────────
  // RMS normalization ceiling: the analyser RMS value that maps to 1.0.
  // WORKFLOW: open the debug overlay, watch WIND INTENSITY at rest.
  //   - If it reads < 0.2 at base wind → lower this value (e.g. 0.005)
  //   - If it's always pegged at 1.0 → raise it (e.g. 0.05)
  //   Target: base breeze ≈ 0.4, strong gust ≈ 0.9
  WIND_RMS_NORM: 0.052,

  // Multiplier applied to wind intensity to get the dust speed factor.
  // gustMult = WIND INTENSITY × DUST_GUST_MULTIPLIER
  // At base (intensity ≈ 0.4): speed = 0.4 × 3.5 = 1.4×
  // At gust (intensity ≈ 0.9): speed = 0.9 × 3.5 = 3.15×
  DUST_GUST_MULTIPLIER: 90,

  // Show a wind-force debug overlay (intensity bar + live values).
  // Set to true to verify gust coupling is working, false for production.
  DUST_DEBUG: false,

  // ── Orchestration API ────────────────────────────────────────────────────
  ORCHESTRATE_ENDPOINT: 'http://localhost:7071/api/orchestrate/flow1',
};
