import { createNfcScanner } from "/js/nfc.js";
let scanner;
export function initialize(reference) {
    scanner?.dispose();
    scanner = createNfcScanner(reference, ".users-page", "HandleNfcEnrollment");
}
export function stopNfc() { scanner?.stop(); }
export function dispose() {
    scanner?.dispose();
    scanner = null;
}
