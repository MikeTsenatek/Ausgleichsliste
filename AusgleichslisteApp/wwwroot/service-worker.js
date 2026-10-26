const CACHE_NAME = "ausgleichsliste-static-v1";
const OFFLINE_URL = "/offline.html";
const PRECACHE_URLS = [
    OFFLINE_URL,
    "/app.css",
    "/favicon.png",
    "/icons/icon-192.png",
    "/icons/icon-512.png",
    "/icons/icon-maskable-512.png"
];

self.addEventListener("install", event => {
    event.waitUntil(
        caches.open(CACHE_NAME)
            .then(cache => cache.addAll(PRECACHE_URLS))
            .then(() => self.skipWaiting())
    );
});

self.addEventListener("activate", event => {
    event.waitUntil(
        caches.keys()
            .then(keys => Promise.all(
                keys
                    .filter(key => key.startsWith("ausgleichsliste-") && key !== CACHE_NAME)
                    .map(key => caches.delete(key))
            ))
            .then(() => self.clients.claim())
    );
});

self.addEventListener("fetch", event => {
    const request = event.request;
    const url = new URL(request.url);

    if (request.method !== "GET" || url.origin !== self.location.origin) {
        return;
    }

    if (request.mode === "navigate") {
        event.respondWith(fetch(request).catch(() => caches.match(OFFLINE_URL)));
        return;
    }

    if (!isStaticAsset(url.pathname)) {
        return;
    }

    event.respondWith(
        fetch(request)
            .then(response => {
                if (!response.ok) {
                    return response;
                }

                const responseCopy = response.clone();
                return caches.open(CACHE_NAME)
                    .then(cache => cache.put(request, responseCopy))
                    .then(() => response);
            })
            .catch(() => caches.match(request))
    );
});

function isStaticAsset(pathname) {
    return pathname.startsWith("/_framework/")
        || pathname.startsWith("/_content/")
