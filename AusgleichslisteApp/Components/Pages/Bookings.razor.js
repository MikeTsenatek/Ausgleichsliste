// JavaScript module for Bookings.razor
export function confirmDelete(message) {
    return new Promise((resolve) => {
        // Erstelle Modal HTML
        const modal = document.createElement('div');
        modal.className = 'modal fade show d-block';
        modal.style.backgroundColor = 'rgba(0,0,0,0.5)';
        modal.innerHTML = `
            <div class="modal-dialog modal-dialog-centered">
                <div class="modal-content">
                    <div class="modal-header bg-danger text-white">
                        <h5 class="modal-title">
                            <i class="bi bi-exclamation-triangle-fill me-2"></i>
                            Buchung löschen
                        </h5>
                    </div>
                    <div class="modal-body">
                        <div class="d-flex align-items-start">
                            <div class="text-danger me-3 mt-1">
                                <svg width="24" height="24" fill="currentColor" viewBox="0 0 16 16">
                                    <path d="M8.982 1.566a1.13 1.13 0 0 0-1.96 0L.165 13.233c-.457.778.091 1.767.98 1.767h13.713c.889 0 1.438-.99.98-1.767L8.982 1.566zM8 5c.535 0 .954.462.9.995l-.35 3.507a.552.552 0 0 1-1.1 0L7.1 5.995A.905.905 0 0 1 8 5zm.002 6a1 1 0 1 1 0 2 1 1 0 0 1 0-2z"/>
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
                        <button type="button" class="btn btn-danger" id="confirmBtn">
                            <i class="bi bi-trash me-1"></i>
                            Löschen
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
            // Enter auf Löschen-Button = Bestätigen
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