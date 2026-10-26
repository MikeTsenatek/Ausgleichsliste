let nfcController = null;
let nfcEnabled = false;

function stopNfc() {
  nfcController?.abort();
  nfcController = null;
}

function startNfc() {
  if (nfcController || !window.isSecureContext || !("NDEFReader" in window)) return;
  const current = new AbortController();
  nfcController = current;
  const reader = new NDEFReader();
  let lookupInFlight = false;
  let lastSerial = null;
  let lastReadAt = -Infinity;
  reader.onreading = async event => {
    const serial = event.serialNumber?.trim();
    if (!serial || current.signal.aborted || !nfcEnabled || lookupInFlight || !dotNetReference) return;
    const now = performance.now();
    // Android can repeatedly report a card that stays near the antenna.
    // Suppress duplicate lookups without delaying a different card.
    if (serial === lastSerial && now - lastReadAt < 2000) return;
    lastSerial = serial;
    lastReadAt = now;
    lookupInFlight = true;
    try {
      await dotNetReference.invokeMethodAsync("HandleNfcSelection", "", serial);
    } catch (error) {
      if (!current.signal.aborted) console.warn("Kartenauswahl fehlgeschlagen.", error);
    } finally {
      lookupInFlight = false;
    }
  };
  // Keep listening after unreadable or unknown cards so another card can be used.
  reader.onreadingerror = () => {};
  reader.scan({ signal: current.signal }).catch(error => {
    if (current.signal.aborted) return;
    if (nfcController === current) nfcController = null;
    console.warn("Kartenleser konnte nicht gestartet werden.", error);
  });
}

export function setNfcEnabled(enabled) {
  if (nfcEnabled === enabled) return;
  nfcEnabled = enabled;
  if (enabled) startNfc();
  else stopNfc();
}

function onProductClick(event) {
  // Stop immediately on manual selection or cancellation, before the server
  // round trip, so card events cannot compete with the tap.
  if (event.target.closest(".shop-page .shop-user-button, .shop-page .shop-toolbar button")) {
    setNfcEnabled(false);
    return;
  }
  const product = event.target.closest(".shop-page .shop-product-card");
  if (product && !product.disabled) {
    // Start within the product click, before the server round trip, so Chrome
    // can request permission using this existing user gesture.
    setNfcEnabled(true);
  }
}
let dotNetReference = null;
let buffer = "";
let lastKeyAt = 0;

function onKeyDown(event) {
  if (event.ctrlKey || event.altKey || event.metaKey) {
    return;
  }

  const now = Date.now();
  if (now - lastKeyAt > 120) {
    buffer = "";
  }
  lastKeyAt = now;

  if (event.key === "Enter") {
    const barcode = buffer.trim();
    buffer = "";

    if (barcode.length >= 3 && dotNetReference) {
      setNfcEnabled(true);
      dotNetReference.invokeMethodAsync("HandleBarcodeScan", barcode).catch(() => {});
    }
    return;
  }

  if (event.key.length === 1) {
    buffer += event.key;
  }
}

export function initialize(reference) {
  window.removeEventListener("keydown", onKeyDown);
  document.removeEventListener("click", onProductClick, true);
  setNfcEnabled(false);
  document.addEventListener("click", onProductClick, true);
  dotNetReference = reference;
  window.addEventListener("keydown", onKeyDown);
}

export function dispose() {
  setNfcEnabled(false);
  document.removeEventListener("click", onProductClick, true);
  window.removeEventListener("keydown", onKeyDown);
  dotNetReference = null;
  buffer = "";
}
