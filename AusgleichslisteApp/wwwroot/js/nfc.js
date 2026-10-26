// Start scan() directly in the browser click handler to retain user activation.
export function createNfcScanner(reference, scope, readMethod) {
    let controller = null;
    let disposed = false;
    const report = message => reference.invokeMethodAsync("HandleNfcStatus", message).catch(() => {});
    function stop() {
        controller?.abort();
        controller = null;
    }
    async function onClick(event) {
        const button = event.target.closest(`${scope} [data-nfc-start], ${scope} [data-nfc-stop]`);
        if (!button || button.disabled) return;
        stop();
        if (button.hasAttribute("data-nfc-stop")) {
            await report("");
            return;
        }
        if (!window.isSecureContext || !("NDEFReader" in window)) {
            await report("Web NFC ist hier nicht verfügbar. Bitte HTTPS und einen NFC-fähigen Browser verwenden, z. B. Chrome auf Android.");
            return;
        }
        const target = button.getAttribute("data-nfc-start");
        const label = button.getAttribute("data-nfc-label");
        const current = new AbortController();
        controller = current;
        const reader = new NDEFReader();
        reader.onreadingerror = () => report("Karte konnte nicht gelesen werden. Bitte eine kompatible NDEF-Karte erneut vorhalten.");
        reader.onreading = async event => {
            if (current.signal.aborted || disposed) return;
            const serial = event.serialNumber?.trim();
            if (!serial) {
                await report("Diese Karte liefert keine NFC-ID. Bitte eine andere Karte verwenden.");
                return;
            }
            stop();
            try {
                await reference.invokeMethodAsync(readMethod, target, serial);
            } catch {
                if (!disposed) await report("NFC-Zuordnung fehlgeschlagen. Bitte erneut versuchen.");
            }
        };
        try {
            await reader.scan({ signal: current.signal });
            if (!current.signal.aborted) await report(label ? `NFC bereit für ${label} – bitte Karte vorhalten.` : "NFC bereit – bitte Karte vorhalten.");
        } catch (error) {
            if (current.signal.aborted || disposed) return;
            stop();
            await report(error.name === "NotAllowedError"
                ? "NFC-Zugriff wurde nicht erlaubt. Bitte die Browser-Berechtigung prüfen und erneut starten."
                : "NFC konnte nicht gestartet werden. Bitte NFC am Gerät einschalten und erneut versuchen.");
        }
    }
    document.addEventListener("click", onClick);
    return {
        stop,
        dispose() {
            disposed = true;
            stop();
            document.removeEventListener("click", onClick);
        }
    };
}
