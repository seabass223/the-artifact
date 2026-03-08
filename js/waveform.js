/**
 * waveform.js
 * Polar time-domain audio trace — waveform samples mapped around a circle.
 * Silent = perfect ring. Active = ring deforms with the signal.
 */

const GLOW_COLOR  = '#7cffcb';
const TRACE_COLOR = 'rgba(124, 255, 203, 0.3)';
const RING_COLOR  = 'rgba(58, 110, 90, 0.4)';

export function createWaveform(audio, canvas) {
  const audioCtx = new AudioContext();
  audioCtx.resume();
  const source   = audioCtx.createMediaElementSource(audio);
  const analyser = audioCtx.createAnalyser();

  analyser.fftSize = 512;
  analyser.smoothingTimeConstant = 0.8;

  source.connect(analyser);
  analyser.connect(audioCtx.destination);

  const bufLen = analyser.fftSize;
  const data   = new Uint8Array(bufLen);

  const dpr  = window.devicePixelRatio || 1;
  const size = 80; // matches CSS width/height
  canvas.width  = size * dpr;
  canvas.height = size * dpr;
  const ctx = canvas.getContext('2d');
  ctx.scale(dpr, dpr);

  const cx = size / 2;
  const cy = size / 2;
  const baseR = size * 0.28;

  let animId = null;

  function draw() {
    animId = requestAnimationFrame(draw);
    analyser.getByteTimeDomainData(data);

    ctx.clearRect(0, 0, size, size);

    // Static reference ring
    ctx.beginPath();
    ctx.arc(cx, cy, baseR, 0, Math.PI * 2);
    ctx.strokeStyle = RING_COLOR;
    ctx.lineWidth = 0.5;
    ctx.shadowBlur = 0;
    ctx.stroke();

    // Polar waveform trace
    ctx.beginPath();
    for (let i = 0; i <= bufLen; i++) {
      const idx   = i % bufLen;
      const angle = (idx / bufLen) * Math.PI * 2 - Math.PI / 2;
      const amp   = (data[idx] / 128.0) - 1.0;   // -1 … +1
      const r     = baseR + amp * baseR * 0.55;
      const x     = cx + r * Math.cos(angle);
      const y     = cy + r * Math.sin(angle);
      i === 0 ? ctx.moveTo(x, y) : ctx.lineTo(x, y);
    }
    ctx.closePath();
    ctx.strokeStyle = TRACE_COLOR;
    ctx.lineWidth   = 1;
    ctx.shadowBlur  = 6;
    ctx.shadowColor = GLOW_COLOR;
    ctx.stroke();
  }

  draw();

  return {
    stop() {
      cancelAnimationFrame(animId);
      ctx.clearRect(0, 0, size, size);
      source.disconnect();
      analyser.disconnect();
      audioCtx.close();
    }
  };
}
