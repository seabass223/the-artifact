/**
 * vignette.js
 * Controls the radial vignette overlay.
 *
 * The CSS transition on #vignette handles the animation when
 * --vignette-size changes. JS just drives the value.
 */

const root = document.documentElement;

/**
 * Reveal the scene: fade in the scene container and widen the vignette.
 * @param {object} opts
 * @param {string} opts.targetSize  - CSS % value for the clear center, e.g. '45%'
 * @param {number} opts.sceneDelay  - ms before scene becomes visible
 */
export function revealScene({ targetSize = '45%', sceneDelay = 80 } = {}) {
  const sceneEl = document.getElementById('scene-container');

  // Make the scene image visible first (slight delay so transition fires)
  setTimeout(() => {
    sceneEl.classList.add('visible');
  }, sceneDelay);

  // Widen the vignette — CSS transition takes it from 0% to targetSize
  // We need one rAF to ensure the initial 0% value is painted before changing
  requestAnimationFrame(() => {
    requestAnimationFrame(() => {
      root.style.setProperty('--vignette-size', targetSize);
    });
  });
}

/**
 * Programmatically set the vignette opening size at any time.
 * @param {string} size - CSS value, e.g. '60%' or '20%'
 */
export function setVignetteSize(size) {
  root.style.setProperty('--vignette-size', size);
}
