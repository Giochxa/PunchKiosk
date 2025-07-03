self.addEventListener('install', (event) => {
    console.log('[Service Worker] Installed');
});

self.addEventListener('fetch', (event) => {
    // Default fetch behavior (could add caching later)
    event.respondWith(fetch(event.request));
});
