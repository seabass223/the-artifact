/**
 * main.js
 * Entry point. Orchestrates preloader -> vignette reveal -> scene -> UI.
 */

import { runPreloader } from './preloader.js';
import { revealScene }  from './vignette.js';
import { initScene }    from './scene.js';
import { initUI, setDateParam } from './ui.js';
import { startWind } from './wind.js';
import { initDust } from './dust.js';

// Parse optional ?date=YYYY-MM-DD query param
const rawDate = new URLSearchParams(window.location.search).get('date');
const dateParam = /^\d{4}-\d{2}-\d{2}$/.test(rawDate ?? '') ? rawDate : null;

// Init UI bindings immediately (DOM is ready since module scripts are deferred)
initUI();
if (dateParam) setDateParam(dateParam);

// Boot sequence
if (dateParam) {
  // Hide preloader immediately and go straight to powered-on scene + UI
  const preloaderEl = document.getElementById('preloader');
  if (preloaderEl) preloaderEl.style.display = 'none';
  revealScene({ targetSize: '45%' });
  initScene({ skipToReady: true });
  // Audio/dust require a user gesture — start on first interaction
  function onFirstGesture() {
    startWind();
    initDust();
    document.removeEventListener('click', onFirstGesture);
    document.removeEventListener('keydown', onFirstGesture);
  }
  document.addEventListener('click', onFirstGesture);
  document.addEventListener('keydown', onFirstGesture);
} else {
  runPreloader(() => {
    startWind();
    initDust();
    revealScene({ targetSize: '45%' });
    initScene();
  });
}
