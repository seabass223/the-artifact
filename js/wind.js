/**
 * wind.js
 * Ambient wind sound effect using Web Audio API.
 */


let ctx, noiseSrc, noiseBuf, windGain, bp, hp, lfo, lfoGain, gustGain;
let analyser, analyserData;

import { SETTINGS } from './settings.js';

// Returns 0–1 RMS of the actual audio output — exactly what the user hears.
// Normalised against SETTINGS.WIND_RMS_NORM; tune that value in settings.js.
export function getWindIntensity() {
  if (!analyser) return 0;
  analyser.getFloatTimeDomainData(analyserData);
  let sum = 0;
  for (let i = 0; i < analyserData.length; i++) sum += analyserData[i] * analyserData[i];
  return Math.min(Math.sqrt(sum / analyserData.length) / SETTINGS.WIND_RMS_NORM, 1.0);
}

function createPinkNoiseBuffer(context, seconds = 2) {
  const sampleRate = context.sampleRate;
  const length = sampleRate * seconds;
  const buffer = context.createBuffer(1, length, sampleRate);
  const data = buffer.getChannelData(0);

  // Simple "Voss-McCartney-ish" pink noise approximation
  let b0 = 0, b1 = 0, b2 = 0, b3 = 0, b4 = 0, b5 = 0, b6 = 0;
  for (let i = 0; i < length; i++) {
    const white = Math.random() * 2 - 1;
    b0 = 0.99886 * b0 + white * 0.0555179;
    b1 = 0.99332 * b1 + white * 0.0750759;
    b2 = 0.96900 * b2 + white * 0.1538520;
    b3 = 0.86650 * b3 + white * 0.3104856;
    b4 = 0.55000 * b4 + white * 0.5329522;
    b5 = -0.7616 * b5 - white * 0.0168980;
    const pink = b0 + b1 + b2 + b3 + b4 + b5 + b6 + white * 0.5362;
    b6 = white * 0.115926;

    data[i] = pink * 0.08; // overall level
  }
  return buffer;
}

export function startWind() {
  ctx = new (window.AudioContext || window.webkitAudioContext)();

  // Noise source (looped buffer)
  noiseBuf = createPinkNoiseBuffer(ctx, 3);
  noiseSrc = ctx.createBufferSource();
  noiseSrc.buffer = noiseBuf;
  noiseSrc.loop = true;

  // Filters to shape wind
  hp = ctx.createBiquadFilter();
  hp.type = "highpass";
  hp.frequency.value = 80;

  bp = ctx.createBiquadFilter();
  bp.type = "bandpass";
  bp.frequency.value = 500;     // "whoosh" center
  bp.Q.value = 0.7;

  // Main wind gain
  windGain = ctx.createGain();
  windGain.gain.value = 0.0;

  // Gust layer gain (adds stronger swells)
  gustGain = ctx.createGain();
  gustGain.gain.value = 0.0;

  // LFO for slow gusting
  lfo = ctx.createOscillator();
  lfo.type = "sine";
  lfo.frequency.value = 0.08; // slow

  lfoGain = ctx.createGain();
  lfoGain.gain.value = 0.25;  // depth of modulation

  // Route: noise -> hp -> bp -> (windGain + gustGain) -> out
  noiseSrc.connect(hp);
  hp.connect(bp);

  bp.connect(windGain);
  bp.connect(gustGain);

  // LFO modulates windGain.gain
  lfo.connect(lfoGain);
  lfoGain.connect(windGain.gain);

  // Optional: LFO also slightly modulates bandpass frequency
  const freqMod = ctx.createGain();
  freqMod.gain.value = 250; // Hz range
  lfo.connect(freqMod);
  freqMod.connect(bp.frequency);

  // Output — tap the master bus with an analyser so getWindIntensity()
  // returns true RMS of what the user actually hears.
  const master = ctx.createGain();
  master.gain.value = 0.7;
  windGain.connect(master);
  gustGain.connect(master);

  analyser = ctx.createAnalyser();
  analyser.fftSize = 2048;  // ~46 ms window — stable RMS, less frame-to-frame noise
  analyserData = new Float32Array(analyser.fftSize);
  master.connect(analyser);
  analyser.connect(ctx.destination);

  // Fade in
  const now = ctx.currentTime;
  windGain.gain.setValueAtTime(0.02, now);
  windGain.gain.linearRampToValueAtTime(0.18, now + 2.0);

  // Random gust scheduler
  function scheduleGust() {
    if (!ctx) return;
    const t = ctx.currentTime;
    const dur = 0.8 + Math.random() * 2.2;     // seconds
    const peak = 0.05 + Math.random() * 0.22;  // gust intensity (0.05–0.27)

    gustGain.gain.cancelScheduledValues(t);
    gustGain.gain.setValueAtTime(0.0, t);
    gustGain.gain.linearRampToValueAtTime(peak, t + dur * 0.35);
    gustGain.gain.linearRampToValueAtTime(0.0, t + dur);

    // vary "whoosh" pitch a bit per gust
    bp.frequency.setTargetAtTime(350 + Math.random() * 650, t, 0.5);

    setTimeout(scheduleGust, 700 + Math.random() * 1800);
  }

  noiseSrc.start();
  lfo.start();
  scheduleGust();
}

function playButtonTick(hiFreq, loFreq, gainAmt, duration) {
  const audioCtx = ctx || new (window.AudioContext || window.webkitAudioContext)();
  const now = audioCtx.currentTime;

  // Short noise burst — the "click" body
  const bufLen = Math.ceil(audioCtx.sampleRate * duration);
  const buf = audioCtx.createBuffer(1, bufLen, audioCtx.sampleRate);
  const data = buf.getChannelData(0);
  for (let i = 0; i < bufLen; i++) data[i] = Math.random() * 2 - 1;

  const noise = audioCtx.createBufferSource();
  noise.buffer = buf;

  // High-pass to cut low rumble, bandpass to focus the click character
  const hp = audioCtx.createBiquadFilter();
  hp.type = 'highpass';
  hp.frequency.value = loFreq;

  const bp = audioCtx.createBiquadFilter();
  bp.type = 'bandpass';
  bp.frequency.value = hiFreq;
  bp.Q.value = 0.8;

  const gain = audioCtx.createGain();
  gain.gain.setValueAtTime(gainAmt, now);
  gain.gain.exponentialRampToValueAtTime(0.001, now + duration);

  noise.connect(hp);
  hp.connect(bp);
  bp.connect(gain);
  gain.connect(audioCtx.destination);
  noise.start(now);
  noise.stop(now + duration);
}

export function playClickDown() {
  playButtonTick(5000, 800, 0.6, 0.018);
}

export function playClickUp() {
  playButtonTick(3200, 500, 0.4, 0.015);
}

export function stopWind() {
  if (!ctx) return;
  const t = ctx.currentTime;
  windGain.gain.cancelScheduledValues(t);
  windGain.gain.setTargetAtTime(0.0, t, 0.2);
  gustGain.gain.cancelScheduledValues(t);
  gustGain.gain.setTargetAtTime(0.0, t, 0.2);

  setTimeout(() => {
    try { noiseSrc.stop(); } catch { }
    try { lfo.stop(); } catch { }
    ctx.close();
    ctx = null;
    analyser = null;
    analyserData = null;
  }, 600);
}
