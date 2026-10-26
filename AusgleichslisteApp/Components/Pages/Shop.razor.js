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
  dotNetReference = reference;
  window.addEventListener("keydown", onKeyDown);
}

export function dispose() {
  window.removeEventListener("keydown", onKeyDown);
  dotNetReference = null;
  buffer = "";
}
