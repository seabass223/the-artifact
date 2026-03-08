/**
 * preloader.js
 * Handles the loading sequence: preloads assets, types out status lines,
 * then fades out the overlay and fires an onReady callback.
 */

const IMAGE_SRCS = [
  'images/scene1.jpg',
  'images/scene3-off.jpg',
  'images/scene3-on.jpg',
];

const NARRATIVE = `I had been walking for hours across the cold windy desert, the sand rasping under my boots, when something metallic caught the starlight.

Half buried in the dunes was a strange cube, faintly glowing...

...it seemed to know something about the future.`;

const TYPE_SPEED = 38;       // ms per character
const PAUSE_COMMA = 150;     // extra pause at commas
const PAUSE_PERIOD = 400;    // extra pause at sentence ends
const PAUSE_NEWLINE = 600;   // extra pause at paragraph breaks
const HOLD_AFTER = 1200;     // hold after fully typed before fade

let _skipResolve = null;
let _skipped = false;

function sleep(ms) {
  if (_skipped) return Promise.resolve();
  return new Promise(resolve => {
    _skipResolve = resolve;
    setTimeout(() => { _skipResolve = null; resolve(); }, ms);
  });
}

function skip() {
  _skipped = true;
  if (_skipResolve) { _skipResolve(); _skipResolve = null; }
}

function preloadImage(src) {
  return new Promise((resolve, reject) => {
    const img = new Image();
    img.onload = resolve;
    img.onerror = reject;
    img.src = src;
  });
}

export async function runPreloader(onReady) {
  const textEl = document.getElementById('preloader-text');
  const preloaderEl = document.getElementById('preloader');

  // Allow Enter to skip the typing animation
  function onKey(e) {
    if (e.key === 'Enter') skip();
  }
  document.addEventListener('keydown', onKey);

  // Start image preload immediately
  const imagePromise = Promise.all(IMAGE_SRCS.map(src => preloadImage(src).catch(() => { })));

  // Type out the narrative character by character
  textEl.classList.add('cursor');
  let displayed = '';

  for (let i = 0; i < NARRATIVE.length; i++) {
    const ch = NARRATIVE[i];
    displayed += ch;
    textEl.textContent = displayed;

    // Variable pacing based on punctuation
    if (ch === '\n') {
      await sleep(PAUSE_NEWLINE);
    } else if (ch === '.' || ch === '!') {
      await sleep(PAUSE_PERIOD);
    } else if (ch === ',') {
      await sleep(PAUSE_COMMA);
    } else {
      await sleep(TYPE_SPEED);
    }
  }

  // If skipped, show the full text immediately
  if (_skipped) textEl.textContent = NARRATIVE;
  textEl.classList.remove('cursor');
  document.removeEventListener('keydown', onKey);

  // Ensure images are loaded before prompting
  await imagePromise;

  // Show enter prompt and wait for user gesture (unlocks audio autoplay)
  const enterEl = document.getElementById('preloader-enter');
  enterEl.classList.add('visible');

  await new Promise(resolve => {
    function onEnter(e) {
      if (e.type === 'click' || e.key === 'Enter') {
        document.removeEventListener('keydown', onEnter);
        enterEl.removeEventListener('click', onEnter);
        resolve();
      }
    }
    document.addEventListener('keydown', onEnter);
    enterEl.addEventListener('click', onEnter);
  });

  enterEl.classList.remove('visible');

  // Fade out preloader
  preloaderEl.classList.add('fade-out');

  preloaderEl.addEventListener('transitionend', () => {
    preloaderEl.style.display = 'none';
    onReady();
  }, { once: true });
}
