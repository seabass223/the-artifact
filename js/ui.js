/**
 * ui.js
 * Single-button initiate flow: calls orchestration endpoint, plays audio,
 * syncs classification text display to audio playback time.
 */

import { SETTINGS } from './settings.js';
import { createWaveform } from './waveform.js';

let statusEl, initiateBtn, stopBtn, classificationEl, timestampEl, nextRunEl, waveformCanvas;
let animFrameId = null;
let nextRunInterval = null;
let scanFeedInterval = null;
let _dateParam = null;

export function setDateParam(date) {
  _dateParam = date;
}

const HEX = '0123456789ABCDEF';
function randHex(n) {
  let s = '';
  for (let i = 0; i < n; i++) s += HEX[Math.random() * 16 | 0];
  return s;
}
function randRow(locked) {
  if (locked) return `${randHex(2)} ${randHex(2)} ${randHex(2)} ${randHex(2)} ${randHex(2)} ${randHex(2)}`;
  const tokens = Array.from({ length: 6 }, () =>
    Math.random() < 0.2 ? '--' : randHex(2)
  );
  return tokens.join(' ');
}

function startScanFeed() {
  const feed = document.getElementById('scan-feed');
  const rows = feed.querySelectorAll('.scan-feed-row');
  const lockEl = feed.querySelector('.scan-feed-lock');
  if (!feed) return;

  feed.classList.add('active');
  const lockedRows = new Set();
  const lockMessages = ['ACQUIRING LOCK', 'SIGNAL DETECTED', 'PROCESSING', 'DECODING'];
  let lockMsgIdx = 0;
  let tick = 0;

  scanFeedInterval = setInterval(() => {
    tick++;
    rows.forEach((row, i) => {
      if (lockedRows.has(i)) return;
      row.textContent = randRow(false);
      // randomly lock a row after some ticks
      if (tick > 8 && Math.random() < 0.03 && lockedRows.size < rows.length - 1) {
        lockedRows.add(i);
        row.textContent = randRow(true);
        row.classList.add('locked');
        if (lockMsgIdx < lockMessages.length) {
          lockEl.textContent = lockMessages[lockMsgIdx++];
        }
      }
    });
  }, 80);
}

function stopScanFeed() {
  clearInterval(scanFeedInterval);
  scanFeedInterval = null;
  const feed = document.getElementById('scan-feed');
  if (!feed) return;
  feed.style.transition = '';
  feed.style.opacity = '';
  feed.classList.remove('active');
  feed.querySelectorAll('.scan-feed-row').forEach(r => {
    r.textContent = '';
    r.classList.remove('locked');
  });
  feed.querySelector('.scan-feed-lock').textContent = '';
}

function stopScanFeedAnimated() {
  return new Promise(resolve => {
    clearInterval(scanFeedInterval);
    scanFeedInterval = null;
    const feed = document.getElementById('scan-feed');
    if (!feed) { resolve(); return; }

    const unlocked = [...feed.querySelectorAll('.scan-feed-row:not(.locked)')];
    const lockEl = feed.querySelector('.scan-feed-lock');
    let flashes = 0;
    const totalFlashes = 8; // 4 on + 4 off

    // snap unlocked rows to full-bright (locked style) before flashing
    unlocked.forEach(r => {
      r.style.transition = 'none';
      r.textContent = randRow(true);
      r.classList.add('locked');
    });
    // force reflow so the transition suppression takes effect
    feed.offsetHeight; // eslint-disable-line no-unused-expressions

    function flash() {
      const visible = flashes % 2 === 0;
      unlocked.forEach(r => r.style.opacity = visible ? '0' : '');
      lockEl.style.opacity = visible ? '0' : '';
      flashes++;
      if (flashes < totalFlashes) {
        setTimeout(flash, 50);
      } else {
        // restore and fade entire feed out
        unlocked.forEach(r => r.style.opacity = '');
        lockEl.style.opacity = '';
        feed.style.transition = 'opacity 0.4s ease';
        feed.style.opacity = '0';
        feed.addEventListener('transitionend', () => {
          stopScanFeed();
          resolve();
        }, { once: true });
      }
    }
    flash();
  });
}

export function initUI() {
  statusEl         = document.getElementById('engine-status');
  initiateBtn      = document.getElementById('btn-initiate');
  stopBtn          = document.getElementById('btn-stop');
  waveformCanvas   = document.getElementById('waveform-canvas');
  classificationEl = document.getElementById('classification-display');
  timestampEl      = document.getElementById('engine-timestamp');
  nextRunEl        = document.getElementById('engine-nextrun');

  initiateBtn.addEventListener('click', runOrchestration);

  // Restore cached scan info
  const cachedScan    = localStorage.getItem('scanTimestamp');
  const cachedNextRun = localStorage.getItem('nextRunUtc');

  if (cachedScan && timestampEl) {
    const d = new Date(cachedScan);
    timestampEl.textContent = `LAST SCAN ${d.toUTCString().toUpperCase()}`;
  }
  if (cachedNextRun && nextRunEl) {
    startNextRunCountdown(cachedNextRun);
  }
}

function startNextRunCountdown(nextRunUtc) {
  if (nextRunInterval) clearInterval(nextRunInterval);
  function update() {
    const diff = new Date(nextRunUtc) - Date.now();
    if (!nextRunEl) return;
    if (diff <= 0) {
      nextRunEl.textContent = 'NEXT SCAN IMMINENT';
      clearInterval(nextRunInterval);
      return;
    }
    const h = Math.floor(diff / 3600000);
    const m = Math.floor((diff % 3600000) / 60000);
    const s = Math.floor((diff % 60000) / 1000);
    const parts = [];
    if (h) parts.push(`${h}H`);
    if (h || m) parts.push(`${m}M`);
    parts.push(`${s}S`);
    nextRunEl.textContent = `NEXT SCAN IN ${parts.join(' ')}`;
  }
  update();
  nextRunInterval = setInterval(update, 1000);
}

export function showUI() {
  document.getElementById('engine-ui').classList.add('visible');
}

export function hideUI() {
  return new Promise(resolve => {
    const el = document.getElementById('engine-ui');
    if (!el.classList.contains('visible')) { resolve(); return; }
    el.classList.remove('visible');
    el.addEventListener('transitionend', (e) => {
      if (e.propertyName === 'opacity') resolve();
    }, { once: true });
  });
}

async function runOrchestration() {
  initiateBtn.disabled = true;
  initiateBtn.style.display = 'none';
  statusEl.textContent = 'CONNECTING';
  statusEl.className = 'engine-status scanning';
  startScanFeed();

  let result;
  try {
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), 5 * 60 * 1000);

    const endpoint = _dateParam
      ? `${SETTINGS.ORCHESTRATE_ENDPOINT}?date=${_dateParam}`
      : SETTINGS.ORCHESTRATE_ENDPOINT;
    const res = await fetch(endpoint, { signal: controller.signal });
    clearTimeout(timeoutId);

    if (!res.ok) throw new Error(`HTTP ${res.status}`);
    result = await res.json();
  } catch (err) {
    stopScanFeed();
    statusEl.textContent = 'ERROR';
    statusEl.className = 'engine-status';
    initiateBtn.textContent = 'RETRY';
    initiateBtn.disabled = false;
    initiateBtn.style.display = '';
    console.error('Orchestration failed:', err);
    return;
  }

  const { audioBlobPath, articleClassifications, executedUtc, report } = result;

  const scanTs = report?.scanTimestamp || executedUtc;
  if (scanTs) {
    localStorage.setItem('scanTimestamp', scanTs);
    const d = new Date(scanTs);
    const dateStr = d.toISOString().slice(0, 10);
    history.replaceState(null, '', `?date=${dateStr}`);
    timestampEl.textContent = `LAST SCAN ${d.toUTCString().toUpperCase()}`;
  }
  const nextRunUtc = report?.nextRunUtc;
  if (nextRunUtc) {
    localStorage.setItem('nextRunUtc', nextRunUtc);
    startNextRunCountdown(nextRunUtc);
  }
  const classifications = articleClassifications || [];

  // Play audio and sync classification display
  const audio = new Audio();
  audio.crossOrigin = 'anonymous';
  audio.src = audioBlobPath;
  audio.addEventListener('error', () => {
    statusEl.textContent = 'AUDIO ERROR';
    statusEl.className = 'engine-status';
  });

  await stopScanFeedAnimated();
  statusEl.textContent = 'TRANSMITTING';
  statusEl.className = 'engine-status active';

  let waveform = null;
  try {
    waveform = createWaveform(audio, waveformCanvas);
  } catch (err) {
    console.warn('Waveform unavailable:', err);
  }

  audio.play().catch(err => console.error('Audio play failed:', err));

  function resetToIdle() {
    audio.pause();
    cancelAnimationFrame(animFrameId);
    waveform?.stop();
    waveformCanvas.classList.remove('visible');
    classificationEl.classList.remove('visible');
    stopBtn.classList.remove('visible');
    statusEl.textContent = 'AWAITING';
    statusEl.className = 'engine-status';
    initiateBtn.textContent = 'SCAN FOR SIGNALS';
    initiateBtn.disabled = false;
    initiateBtn.style.display = '';
  }

  stopBtn.onclick = resetToIdle;

  function tick() {
    const t = audio.currentTime;
    const active = classifications.find(
      c => t >= c.startTimeSeconds && t <= c.endTimeSeconds
    );

    if (active) {
      if (classificationEl.textContent !== active.classification) {
        classificationEl.textContent = active.classification;
        classificationEl.classList.add('visible');
      }
    } else {
      classificationEl.classList.remove('visible');
    }

    if (!audio.ended && !audio.paused) {
      animFrameId = requestAnimationFrame(tick);
    }
  }

  audio.addEventListener('play', () => {
    waveformCanvas.classList.add('visible');
    stopBtn.classList.add('visible');
    animFrameId = requestAnimationFrame(tick);
  });

  audio.addEventListener('ended', () => {
    cancelAnimationFrame(animFrameId);
    waveform?.stop();
    waveformCanvas.classList.remove('visible');
    classificationEl.classList.remove('visible');
    stopBtn.classList.remove('visible');
    statusEl.textContent = 'COMPLETE';
    statusEl.className = 'engine-status active';
    initiateBtn.textContent = 'SCAN FOR SIGNALS';
    initiateBtn.disabled = false;
    initiateBtn.style.display = '';
  });
}
