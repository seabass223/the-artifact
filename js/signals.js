/**
 * signals.js
 * Loads and manages signal data from latest.json.
 */

let signalData = null;

export async function loadSignals() {
  const res = await fetch('latest.json');
  signalData = await res.json();
  return signalData;
}

export function getSignals() {
  return signalData;
}

export function filterByThreshold(signals, threshold) {
  return signals.filter(s => s.anomalyIndex >= threshold);
}

export function sortByAnomaly(signals) {
  return [...signals].sort((a, b) => b.anomalyIndex - a.anomalyIndex);
}
