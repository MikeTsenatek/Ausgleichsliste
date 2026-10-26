(() => {
    async function initialize() {
        if (!("serviceWorker" in navigator) || !window.isSecureContext) return;
        try {
            await navigator.serviceWorker.register("/service-worker.js", { updateViaCache: "none" });
        } catch (error) {
            console.warn("Service Worker konnte nicht registriert werden.", error);
        }
    }

    if (document.readyState === "complete") {
        initialize();
    } else {
        window.addEventListener("load", initialize, { once: true });
    }
})();
