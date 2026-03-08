/**
 * scene.js
 * Scene hotspot interaction. Invisible click areas positioned relative to
 * the native image dimensions, scaling correctly when the image is resized.
 */

import { showUI } from './ui.js';
import { playClickDown, playClickUp } from './wind.js';

// Native resolution of scene1.jpg — hotspot coords are in these pixel units
const NATIVE_W = 1536;
const NATIVE_H = 1024;

const HOTSPOTS = [
  { id: 'hotspot-a', x: 920, y: 460, w: 150, h: 85, targets: ['images/scene3-off.jpg'] },
  { id: 'hotspot-b', x: 340, y: 460, w: 137, h: 159, targets: ['images/scene3-on.jpg'], onDone: showUI },
];

/**
 * Compute the rendered rect of an object-fit:contain image within its element.
 * Returns scale factor and pixel offsets (letterbox / pillarbox).
 */
function getContainRect(img) {
  const cw = window.innerWidth;
  const ch = window.innerHeight;
  const scale = Math.min(cw / NATIVE_W, ch / NATIVE_H);
  return {
    scale,
    ox: (cw - NATIVE_W * scale) / 2,
    oy: (ch - NATIVE_H * scale) / 2,
  };
}

function positionEl(el, hotspot, rect) {
  el.style.left = `${rect.ox + hotspot.x * rect.scale}px`;
  el.style.top = `${rect.oy + hotspot.y * rect.scale}px`;
  el.style.width = `${hotspot.w * rect.scale}px`;
  el.style.height = `${hotspot.h * rect.scale}px`;
}

function crossfade(targetSrc) {
  return new Promise(resolve => {
    const img = document.getElementById('scene-image');
    const container = document.getElementById('scene-container');

    const overlay = document.createElement('img');
    overlay.src = targetSrc;
    overlay.draggable = false;
    Object.assign(overlay.style, {
      position: 'absolute',
      inset: '0',
      width: '100%',
      height: '100%',
      objectFit: 'contain',
      objectPosition: 'center',
      userSelect: 'none',
      pointerEvents: 'none',
      opacity: '0',
      transition: 'opacity 0.8s ease-in-out',
    });
    container.appendChild(overlay);

    requestAnimationFrame(() => requestAnimationFrame(() => {
      overlay.style.opacity = '1';
    }));

    overlay.addEventListener('transitionend', () => {
      img.src = targetSrc;
      overlay.remove();
      resolve();
    }, { once: true });
  });
}

export function initScene({ skipToReady = false } = {}) {
  const img = document.getElementById('scene-image');
  const container = document.getElementById('scene-container');

  // Build all elements up front but don't mount them yet
  const entries = HOTSPOTS.map(hotspot => {
    const el = document.createElement('div');
    el.id = hotspot.id;
    el.className = 'scene-hotspot';
    Object.assign(el.style, {
      position: 'absolute',
      zIndex: '5',
    });
    return { el, hotspot };
  });

  function updatePositions() {
    const rect = getContainRect(img);
    document.documentElement.style.setProperty('--scene-scale', rect.scale);
    entries.forEach(({ el, hotspot }) => {
      if (el.isConnected) positionEl(el, hotspot, rect);
    });
  }

  function mountHotspot(index) {
    if (index >= entries.length) return;
    const { el, hotspot } = entries[index];
    container.appendChild(el);
    updatePositions();

    if (hotspot.id === 'hotspot-b') {
      el.addEventListener('mousedown', () => playClickDown(), { once: true });
      el.addEventListener('mouseup', () => playClickUp(), { once: true });
    }
    el.addEventListener('click', async () => {
      el.style.pointerEvents = 'none';
      el.remove();
      for (const target of hotspot.targets) await crossfade(target);
      hotspot.onDone?.();
      mountHotspot(index + 1);
    }, { once: true });
  }

  async function doSkipToReady() {
    // Immediately show the powered-on scene image and the UI
    const rect = getContainRect(img);
    document.documentElement.style.setProperty('--scene-scale', rect.scale);
    img.src = 'images/scene3-on.jpg';
    showUI();
    window.addEventListener('resize', updatePositions);
  }

  if (skipToReady) {
    if (img.complete) {
      doSkipToReady();
    } else {
      img.addEventListener('load', doSkipToReady, { once: true });
    }
    return;
  }

  if (img.naturalWidth > 0) {
    mountHotspot(0);
  } else {
    img.addEventListener('load', () => mountHotspot(0), { once: true });
  }

  window.addEventListener('resize', updatePositions);
}
