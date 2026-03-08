/**
 * dust.js
 * Canvas-based desert sand / dust particle system.
 * Particles drift rightward in wind gusts with subtle turbulence.
 * Controlled by SETTINGS in settings.js.
 */

import { SETTINGS } from './settings.js';
import { getWindIntensity } from './wind.js';

// Sandy palette — slight variation across particles
const COLORS = [
  [200, 172, 118],  // warm mid-sand
  [215, 190, 145],  // pale dune
  [182, 152, 98],  // amber dust
  [225, 205, 162],  // fine bright dust
  [190, 160, 105],  // ochre grit
];

let canvas, ctx, animId;
let debugCanvas, debugCtx;
let W = 0, H = 0;
let particles = [];

// Smoothed speed multiplier — lerps toward wind.js intensity each frame
let gustMult = 1.0;

// ─── Particle factory ────────────────────────────────────────────────────────

function spawn(scatter) {
  const r = 0.5 + Math.random() * 1.9;
  const speed = SETTINGS.DUST_WIND_SPEED * (0.25 + Math.random() * 0.85);
  const col = COLORS[Math.floor(Math.random() * COLORS.length)];

  // Smaller/finer particles are more transparent — mimics depth
  const alphaBase = (0.08 + Math.random() * 0.92) * SETTINGS.DUST_OPACITY_MAX;
  const alpha = alphaBase * Math.min(1, r / 1.4);

  return {
    x: scatter ? Math.random() * W : -(r + 2),
    y: Math.random() * H,
    vx: speed,
    vy: (Math.random() - 0.38) * 0.28,  // bias toward slight downward
    r,
    alpha,
    col,
    phase: Math.random() * Math.PI * 2,     // turbulence phase
    phaseSpeed: 0.018 + Math.random() * 0.022,
  };
}

function initParticles() {
  particles = Array.from({ length: SETTINGS.DUST_PARTICLE_COUNT }, () => spawn(true));
}

// ─── Resize ──────────────────────────────────────────────────────────────────

function resize() {
  W = canvas.width = window.innerWidth;
  H = canvas.height = window.innerHeight;
  if (debugCanvas) {
    debugCanvas.width = W;
    debugCanvas.height = H;
  }
}

// ─── Main loop ───────────────────────────────────────────────────────────────

function tick() {
  // Pause rendering when tab is hidden — free the CPU entirely
  if (document.hidden) {
    animId = requestAnimationFrame(tick);
    return;
  }

  gustMult = getWindIntensity() * SETTINGS.DUST_GUST_MULTIPLIER;

  ctx.clearRect(0, 0, W, H);

  for (let i = 0; i < particles.length; i++) {
    const p = particles[i];

    // Turbulence: sinusoidal micro-jitter orthogonal to wind direction
    p.phase += p.phaseSpeed;
    const jx = Math.sin(p.phase * 1.4) * 0.10;
    const jy = Math.cos(p.phase) * 0.07;

    p.x += p.vx * gustMult + jx;
    p.y += p.vy + jy;

    // Recycle when blown off right edge (or far below/above — rare)
    if (p.x > W + 4 || p.y < -8 || p.y > H + 8) {
      particles[i] = spawn(false);
      continue;
    }

    // Draw as a horizontally-elongated ellipse — suggests motion blur
    const [r, g, b] = p.col;
    ctx.globalAlpha = p.alpha;
    ctx.fillStyle = `rgb(${r},${g},${b})`;
    ctx.beginPath();
    ctx.ellipse(p.x, p.y, p.r * 1.6, p.r * 0.55, 0, 0, Math.PI * 2);
    ctx.fill();
  }

  ctx.globalAlpha = 1;

  if (SETTINGS.DUST_DEBUG) {
    debugCtx.clearRect(0, 0, W, H);
    drawDebug();
  }

  animId = requestAnimationFrame(tick);
}

// ─── Debug overlay ───────────────────────────────────────────────────────────

function drawDebug() {
  const PAD = 14;
  const BAR_W = 160;
  const BAR_H = 10;
  const ROW = 22;
  const x0 = PAD;
  let y0 = PAD;

  const intensity = getWindIntensity();   // 0–1
  const speed = gustMult;             // smoothed, ~0–2

  // Background pill
  const dc = debugCtx;

  dc.globalAlpha = 0.72;
  dc.fillStyle = '#000';
  dc.beginPath();
  dc.roundRect(x0 - 6, y0 - 6, BAR_W + 100, ROW * 2 + BAR_H + 18, 6);
  dc.fill();
  dc.globalAlpha = 1;

  dc.font = '11px "Share Tech Mono", monospace';
  dc.textAlign = 'left';

  // ── Row 1: wind intensity (source) ──
  dc.fillStyle = '#7cffcb';
  dc.fillText('WIND INTENSITY', x0, y0 + 10);
  dc.fillStyle = '#333';
  dc.fillRect(x0, y0 + ROW - BAR_H, BAR_W, BAR_H);
  dc.fillStyle = '#7cffcb';
  dc.fillRect(x0, y0 + ROW - BAR_H, BAR_W * intensity, BAR_H);
  dc.fillStyle = '#fff';
  dc.fillText(intensity.toFixed(3), x0 + BAR_W + 8, y0 + ROW);

  y0 += ROW + BAR_H + 4;

  // ── Row 2: smoothed gust multiplier (dust speed) ──
  const speedNorm = Math.min(speed / SETTINGS.DUST_GUST_MULTIPLIER, 1);
  dc.fillStyle = '#ffcc00';
  dc.fillText('GUST MULT', x0, y0 + 10);
  dc.fillStyle = '#333';
  dc.fillRect(x0, y0 + ROW - BAR_H, BAR_W, BAR_H);
  dc.fillStyle = '#ffcc00';
  dc.fillRect(x0, y0 + ROW - BAR_H, BAR_W * speedNorm, BAR_H);
  dc.fillStyle = '#fff';
  dc.fillText(speed.toFixed(3) + '×', x0 + BAR_W + 8, y0 + ROW);
}

// ─── Public API ──────────────────────────────────────────────────────────────

export function initDust() {
  if (!SETTINGS.DUST_ENABLED) return;

  canvas = document.createElement('canvas');
  Object.assign(canvas.style, {
    position: 'fixed',
    inset: '0',
    zIndex: '8',       // above scene (z0) & hotspots (z5), below vignette (z10)
    pointerEvents: 'none',
  });
  document.body.appendChild(canvas);

  ctx = canvas.getContext('2d');

  if (SETTINGS.DUST_DEBUG) {
    debugCanvas = document.createElement('canvas');
    Object.assign(debugCanvas.style, {
      position: 'fixed',
      inset: '0',
      zIndex: '11',      // above vignette (z10)
      pointerEvents: 'none',
    });
    document.body.appendChild(debugCanvas);
    debugCtx = debugCanvas.getContext('2d');
  }

  resize();
  window.addEventListener('resize', resize);

  initParticles();
  animId = requestAnimationFrame(tick);
}

export function stopDust() {
  if (animId) {
    cancelAnimationFrame(animId);
    animId = null;
  }
  canvas?.remove();
  canvas = null;
  debugCanvas?.remove();
  debugCanvas = null;
}
