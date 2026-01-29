# Ausgleichsliste

Eine moderne Web-Anwendung zur Verwaltung geteilter Ausgaben mit automatischer Berechnung optimaler Ausgleichszahlungen.

## Features

- **Moderne Blazor Web-App** - Responsive Design für Desktop und Mobile
- **Benutzer-Verwaltung** - Hinzufügen und Verwalten von Gruppenmitgliedern  
- **Ausgaben-Tracking** - Einfaches Erfassen von geteilten Ausgaben
- **Automatische Schulden-Optimierung** - Minimiert die Anzahl nötiger Transaktionen
- **Live-Dashboard** - Echtzeitübersicht über Salden und Zahlungsvorschläge
- **Anpassbares Branding** - Eigene Farben, Logos und Bezeichnungen
- **SQLite-Datenbank** - Robuste Datenspeicherung ohne externe Dependencies

## Technologie

- **ASP.NET Core 9.0** mit Blazor Server
- **Entity Framework Core** mit SQLite
- **Bootstrap 5** für responsives Design
- **C# 13** mit modernen Language Features

## Installation & Setup

### Voraussetzungen
- .NET 9.0 SDK
- Moderne Webbrowser (Chrome, Firefox, Safari, Edge)

### Lokale Entwicklung
```bash
# Repository klonen
git clone https://github.com/username/ausgleichsliste.git
cd ausgleichsliste

# Dependencies installieren und Datenbank migrieren
dotnet restore
dotnet ef database update --project AusgleichslisteApp

# Anwendung starten
dotnet run --project AusgleichslisteApp
```

Die Anwendung ist dann unter `http://localhost:5207` verfügbar.

## Verwendung

1. **Benutzer hinzufügen** - Neue Gruppenmitglieder über die Benutzer-Seite anlegen
2. **Ausgaben erfassen** - Einzelne Buchungen oder Sammelbuchungen eingeben
3. **Ausgleich berechnen** - System berechnet automatisch optimale Zahlungen
4. **Salden überwachen** - Dashboard zeigt aktuelle Kontostände aller Mitglieder

## Konfiguration

Die Anwendung kann über die Einstellungen-Seite angepasst werden:
- Anwendungsname und Branding
- Farben und Logo
- Organisationsinformationen

## Contributing

Pull Requests sind willkommen! Für größere Änderungen bitte zuerst ein Issue öffnen.

## Lizenz

[MIT License](LICENSE)