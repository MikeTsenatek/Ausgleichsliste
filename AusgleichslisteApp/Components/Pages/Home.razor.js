// JavaScript module for Home.razor
export function confirmApplySettlement(message) {
    return new Promise((resolve) => {
        // Erstelle Modal HTML
        const modal = document.createElement('div');
        modal.className = 'modal fade show d-block';
        modal.style.backgroundColor = 'rgba(0,0,0,0.5)';
        modal.innerHTML = `
            <div class="modal-dialog modal-dialog-centered">
                <div class="modal-content">
                    <div class="modal-header bg-primary text-white">
                        <h5 class="modal-title">
                            <i class="bi bi-arrow-left-right me-2"></i>
                            Ausgleichszahlung durchführen
                        </h5>
                    </div>
                    <div class="modal-body">
                        <div class="d-flex align-items-start">
                            <div class="text-primary me-3 mt-1">
                                <svg width="24" height="24" fill="currentColor" viewBox="0 0 16 16">
                                    <path d="M1 14s-1 0-1-1 1-4 6-4 6 3 6 4-1 1-1 1H1zm5-6a3 3 0 1 0 0-6 3 3 0 0 0 0 6z"/>
                                    <path fill-rule="evenodd" d="M13.5 5a.5.5 0 0 1 .5.5V7h1.5a.5.5 0 0 1 0 1H14v1.5a.5.5 0 0 1-1 0V8h-1.5a.5.5 0 0 1 0-1H13V5.5a.5.5 0 0 1 .5-.5z"/>
                                </svg>
                            </div>
                            <div>
                                <pre class="mb-0" style="font-family: inherit; white-space: pre-wrap; background: none; border: none; padding: 0;">${message}</pre>
                            </div>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" id="cancelBtn">
                            <i class="bi bi-x-circle me-1"></i>
                            Abbrechen
                        </button>
                        <button type="button" class="btn btn-primary" id="confirmBtn">
                            <i class="bi bi-check-circle me-1"></i>
                            Durchführen
                        </button>
                    </div>
                </div>
            </div>
        `;
        
        // Füge Modal zum DOM hinzu
        document.body.appendChild(modal);
        
        // Focus auf Abbrechen-Button (sicherere Standard-Aktion)
        const cancelBtn = modal.querySelector('#cancelBtn');
        const confirmBtn = modal.querySelector('#confirmBtn');
        
        setTimeout(() => cancelBtn.focus(), 100);
        
        // Event Handler
        const cleanup = () => {
            modal.remove();
        };
        
        cancelBtn.onclick = () => {
            cleanup();
            resolve(false);
        };
        
        confirmBtn.onclick = () => {
            cleanup();
            resolve(true);
        };
        
        // ESC-Taste = Abbrechen
        const handleKeydown = (e) => {
            if (e.key === 'Escape') {
                cleanup();
                document.removeEventListener('keydown', handleKeydown);
                resolve(false);
            }
            // Enter auf Abbrechen-Button = Abbrechen
            if (e.key === 'Enter' && document.activeElement === cancelBtn) {
                cleanup();
                document.removeEventListener('keydown', handleKeydown);
                resolve(false);
            }
            // Enter auf Bestätigen-Button = Bestätigen
            if (e.key === 'Enter' && document.activeElement === confirmBtn) {
                cleanup();
                document.removeEventListener('keydown', handleKeydown);
                resolve(true);
            }
        };
        
        document.addEventListener('keydown', handleKeydown);
        
        // Klick außerhalb = Abbrechen
        modal.onclick = (e) => {
            if (e.target === modal) {
                cleanup();
                document.removeEventListener('keydown', handleKeydown);
                resolve(false);
            }
        };
    });
}