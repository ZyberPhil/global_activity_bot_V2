# DSharpPlus v5 Bot-Neubau - Master-Plan

Vollstaendiger Neuaufbau des GlobalStatsBot auf DSharpPlus v5 / .NET 9 mit sauberer Architektur, Docker-Deployment auf Ubuntu Home Server und GitHub Actions CI/CD. Das v4-Repo dient als funktionale Referenz, wird aber nicht portiert, sondern modulweise neu implementiert.

## 1) Executive Summary

- Zielzustand: Produktionsreifer Discord-Bot auf DSharpPlus v5 mit Clean-Architecture, EF Core 9/MariaDB, Docker Compose, CI/CD via GitHub Actions
- Neubau statt Portierung: DSharpPlus v5 hat komplett neue API (`DiscordClientBuilder`, Command-Framework via `DSharpPlus.Commands`, Attribut-basierte Event-Handler) - direktes Portieren wuerde v4-Anti-Patterns konservieren
- v4-Schwaechen: Hardcodierte `dotenv.net`-Abhaengigkeit, keine Interfaces/Abstraktion (alle Services sind konkrete Klassen), keine Tests, DB-Modelle mit C#-Konvention-Verletzungen (lowercase Klassennamen), `BotService` erstellt `DiscordClient` manuell statt ueber den v5 `DiscordClientBuilder`, Connection-String wird in `Console.WriteLine` geloggt (Security-Risiko)
- v5-Pattern: `DiscordClientBuilder.CreateDefault()` mit Extension-Methods fuer Commands, Event-Handling, nativem DI
- DB-Schema bleibt: Existierende MariaDB-Tabellen + Stored Procedures werden uebernommen; EF Core Modelle werden in PascalCase umbenannt
- Architektur: Domain/Application/Infrastructure Layer mit Interfaces, Repository-Pattern optional (EF Core DbContext reicht fuer diese Projektgroesse)
- .NET 9 als Target-Framework (Stand Maerz 2026)
- Security: Token nur via Environment Variables, keine Logs von Secrets, non-root Docker User
- Monitoring: Serilog mit strukturiertem JSON-Logging, Healthcheck via Discord Gateway-Status
- CI/CD: GitHub Actions mit Build/Test/Docker-Build/SSH-Deploy auf Home Server
- ANNAHME: Es gibt keine laufende V2-Codebasis; der Neubau startet from scratch im Root des Repos

## 2) Ist-Analyse beider Repos (kurz & technisch)

### Alt-Repo v4: Kernmodule, Staerken, Schwaechen, technische Schulden

**Staerken:**
- Funktional vollstaendiges XP/Badge/Leaderboard-System
- Saubere Stored Procedures fuer Performance-kritische Operationen (XP-Sync, User-UPSERT)
- Multi-Stage Dockerfile, Docker Compose mit MariaDB + Healthchecks
- Vorhandenes Deploy-Script mit Rollback-Strategie

**Schwaechen / technische Schulden:**
- DSharpPlus v4 EOL - `SlashCommands`-Extension ist in v5 entfernt
- `dotenv.net` mit `ignoreExceptions: false` -> erfordert leere `.env`-Datei im Docker-Image (Workaround)
- Connection-String wird auf Console geloggt (`old_v4/bot/GlobalStatsBot/GlobalStatsBot/Program.cs`)
- Modell-Klassen in lowercase (`user`, `guild`, `badge`) - verletzt C#-Konventionen
- Keine Interfaces -> Services nicht testbar, nicht austauschbar
- Kein globaler Error-Handler fuer Slash-Commands
- `BotService` erstellt `DiscordClient` manuell -> v5 nutzt `DiscordClientBuilder` mit nativem DI
- `PingCommandModule` hat unnoetige Abhaengigkeit auf `ProfileService`
- Kein einheitliches Embed-Design-System
- Keine Deferred-Responses bei DB-Queries (nur bei `/admin syncglobalxp`)
- Permission-Check manuell pro Command, kein zentraler Guard
- Level-Formel inkonsistent: SP nutzt `FLOOR(XP/100)`, C# nutzt `FLOOR(SQRT(XP/10))`

### V2-Repo: aktueller Stand, Luecken, Risiken

- Nur README vorhanden
- `old_v4/`-Ordner als Referenz eingebettet
- Keine neue Codebasis, keine Solution, keine Tests

### Tabelle: Komponente | Alt-Status | V2-Status | Handlung

| Komponente | Alt-Status | V2-Status | Handlung (Uebernehmen/Neu bauen/Verwerfen) |
|---|---|---|---|
| `BotService` (Client-Setup) | `IHostedService` + manueller `DiscordClient` | Nicht vorhanden | Neu bauen mit `DiscordClientBuilder` |
| `SlashCommands` Extension | `DSharpPlus.SlashCommands` v4 | Nicht vorhanden | Neu bauen mit `DSharpPlus.Commands` v5 |
| `XpMessageHandler` | Event-Handler direkt an Client | Nicht vorhanden | Neu bauen als v5 Event-Handler |
| `StatsService` | EF Core + SP-Fallback | Nicht vorhanden | Uebernehmen (Logik), Interfaces ergaenzen |
| `UserService` | SP + EF-Fallback | Nicht vorhanden | Uebernehmen (Logik), refactoren |
| `GuildService` | EF Core direkt | Nicht vorhanden | Uebernehmen, Interface ergaenzen |
| `BadgeService` | EF Core direkt | Nicht vorhanden | Uebernehmen, Interface ergaenzen |
| `ProfileService` | Orchestrierung | Nicht vorhanden | Uebernehmen |
| `GlobalXpSyncService` | `BackgroundService` | Nicht vorhanden | Uebernehmen, verbessern |
| DB-Schema + SPs | MariaDB, 7 Tabellen, 6 SPs | Nicht vorhanden | Uebernehmen |
| Dockerfile | Multi-Stage .NET 8 | Nicht vorhanden | Neu bauen fuer .NET 9 |
| Docker Compose | Bot + MariaDB | Nicht vorhanden | Uebernehmen, aktualisieren |
| Deploy-Script | Shell-basiert mit Rollback | Nicht vorhanden | Ersetzen durch GitHub Actions |
| `dotenv.net` | Env-Loading | Nicht vorhanden | Verwerfen |
| `DiscordBotOptions` | Minimal | Nicht vorhanden | Neu bauen mit Options Pattern |
| `guildsubscription` | Vorhanden, ungenutzt | Nicht vorhanden | Verwerfen oder spaeter entscheiden |
| Tests | Nicht vorhanden | Nicht vorhanden | Neu bauen |
| Error-Handler global | Nicht vorhanden | Nicht vorhanden | Neu bauen |
| Embed-Designsystem | Inkonsistent | Nicht vorhanden | Neu bauen |
| i18n | Nicht vorhanden | Nicht vorhanden | Optional in Phase 3 |

## 3) Zielarchitektur v5 (Detail)

### Layer/Module

```
/src
  GlobalActivityBot/
    Program.cs
    Bot/
      BotSetup.cs
      EventHandlers/
        XpMessageHandler.cs
        GuildJoinHandler.cs
    Commands/
      PingCommand.cs
      ProfileCommand.cs
      TopCommandGroup.cs
      BadgeCommandGroup.cs
      GuildCommand.cs
      AdminCommandGroup.cs
    Common/
      EmbedFactory.cs
      ResponseHelper.cs
      PermissionGuards.cs
    Configuration/
      BotOptions.cs
      DatabaseOptions.cs
    Domain/
      Entities/
        User.cs
        Guild.cs
        UserStat.cs
        ChannelStat.cs
        Badge.cs
        UserBadge.cs
    Infrastructure/
      Data/
        BotDbContext.cs
    Services/
      Interfaces/
        IUserService.cs
        IStatsService.cs
        IGuildService.cs
        IBadgeService.cs
        IProfileService.cs
      UserService.cs
      StatsService.cs
      GuildService.cs
      BadgeService.cs
      ProfileService.cs
    BackgroundJobs/
      GlobalXpSyncJob.cs
  GlobalActivityBot.Tests/
/database
  db.sql
/docker
  Dockerfile
  docker-compose.yml
  .env.example
/.github
  workflows/
    ci.yml
    deploy.yml
```

### Entkopplung (Interfaces, DI, Service-Layer)

- Command-Layer ruft nur Service-Interfaces auf
- Service-Layer kapselt Business-Logik
- DbContext nur in Services/Infrastructure
- DI-Registrierung ueber `IServiceCollection`

### Datenhaltung

- MariaDB + Pomelo EF Core 9
- Bestehendes Schema aus `old_v4/database/db.sql` initial weiterverwenden
- Stored Procedures fuer kritische Flows behalten (`sp_GetOrCreateUserByDiscordId`, `sp_SyncGlobalXpCache`)
- Migrationsstrategie: zuerst schema-first, spaeter EF-Migrations fuer neue Features

### Eventing/Background Jobs

- MessageCreated-Handler fuer XP-Vergabe
- `BackgroundService` fuer periodischen Global-XP-Sync (alle 5 Minuten)

### Fehlerbehandlung

- Global: zentraler Handler fuer Command-Exceptions
- Command-spezifisch: erwartbare Validierungsfehler als nutzerfreundliche Ephemeral-Responses

### Observability

- Strukturierte Logs (JSON via Serilog Console)
- Log-Scopes mit CorrelationId
- Metriken minimal: Command-Dauer, Fehlerquote, Reconnects

### Konfigurationsmodell

- `appsettings.json` fuer Defaults
- `appsettings.Production.json` fuer Produktionswerte ohne Secrets
- Secrets nur ueber ENV (`DISCORD_BOT_TOKEN`, `DB_CONNECTION_STRING`)
- `IOptions<T>` fuer typed config

## 4) Priorisierter Umsetzungsplan

### Phase 1: Quick Wins (1-3 Tage)

| Massnahme | Problem | Konkrete Loesung | Aufwand | Impact | Risiko | Abhaengigkeiten | Akzeptanzkriterien |
|---|---|---|---|---|---|---|---|
| 1.1 Projektgeruest | Keine V2-Codebasis | Solution + Console + Testprojekt + NuGet-Grundsetup | S | High | Low | Keine | `dotnet build` erfolgreich |
| 1.2 Bootstrap | Kein lauffaehiger Bot | `Program.cs` mit DI, Logging, `DiscordClientBuilder` | S | High | Low | 1.1 | Bot verbindet erfolgreich |
| 1.3 Ping | Kein Smoke-Command | `/ping` als erster Slash-Command | S | Med | Low | 1.2 | `/ping` antwortet stabil |
| 1.4 Dockerfile | Kein Containerbetrieb | Multi-stage Dockerfile, non-root | S | High | Low | 1.1 | Container startet botprozess |
| 1.5 Compose | Kein lokales/prod Stack-Runbook | Compose fuer Bot+DB + Healthchecks | S | High | Low | 1.4 | `docker compose up -d` laeuft |

### Phase 2: Kernsystem (1-2 Wochen)

| Massnahme | Problem | Konkrete Loesung | Aufwand | Impact | Risiko | Abhaengigkeiten | Akzeptanzkriterien |
|---|---|---|---|---|---|---|---|
| 2.1 Datenmodell | Kein DB-Layer | EF Core Context + Entities in PascalCase | M | High | Med | 1.x | CRUD auf Kernentitaeten funktioniert |
| 2.2 XP-Core | Kein XP-Tracking | `StatsService` + `XpMessageHandler` v5 | M | High | Med | 2.1 | XP steigt bei Messages (Cooldown aktiv) |
| 2.3 Profil | Kein Userprofil | `/me` + `ProfileService` + EmbedFactory | M | High | Low | 2.1,2.2 | Profil zeigt XP/Level/Badges |
| 2.4 Leaderboards | Kein Ranking | `/top global|guild|channel` | M | High | Low | 2.2 | Korrekte Top-Listen |
| 2.5 Badges | Kein Badge-Flow | `/badge give|list|listall` | M | Med | Low | 2.1 | Badge-Vergabe/Anzeige stabil |
| 2.6 Admin | Kein operativer Eingriff | `/admin syncglobalxp` + zentraler Permission-Guard | S | Med | Low | 2.2 | Nur berechtigte User koennen ausfuehren |
| 2.7 Guildinfo | Kein Serverstatus | `/guildinfo` | S | Low | Low | 2.1 | Guild-Infos korrekt |
| 2.8 Sync-Job | Cache drift moeglich | `GlobalXpSyncJob` alle 5 Min | S | Med | Low | 2.2 | Sync-Logs sichtbar, Werte konsistent |
| 2.9 Error-Handling | Uneinheitliche Fehlerpfade | Global Command Error Handler + standardisierte Fehlerembeds | S | High | Low | 1.2 | Keine rohen Exceptions beim User |
| 2.10 Tests | Keine Regression-Sicherung | Unit-Tests fuer Services | M | Med | Low | 2.x | Kernfaelle automatisiert getestet |
| 2.11 Level-Formel | Inkonsistenz C# vs SP | Eine Formel festlegen und in C#/SP angleichen | S | Med | Med | 2.1 | Gleiche Levelwerte in allen Pfaden |

### Phase 3: Stabilisierung & Skalierung (ab 1 Monat)

| Massnahme | Problem | Konkrete Loesung | Aufwand | Impact | Risiko | Abhaengigkeiten | Akzeptanzkriterien |
|---|---|---|---|---|---|---|---|
| 3.1 CI/CD | Manuelles Deploy fehleranfaellig | GitHub Actions mit Build/Test/Docker/Deploy | M | High | Med | 1.x,2.x | Push auf `main` deploybar |
| 3.2 UX-Paging | Lange Leaderboards unkomfortabel | Buttons fuer Seitenwechsel | M | Med | Low | 2.4 | Paging ohne Fehler |
| 3.3 Rate-Limits | Command-Spam Risiko | Cooldowns/Checks pro User | S | Med | Low | 1.2 | Spam reduziert, keine Abuse-Spikes |
| 3.4 Monitoring | Betrieb schwer observierbar | JSON Logs + Basis-Metriken + Healthchecks | M | Med | Low | 1.2 | Probleme schnell identifizierbar |
| 3.5 Schema-Evolution | Aenderungen schwer reproduzierbar | EF Migrations fuer neue Aenderungen | L | Med | Med | 2.1 | Versionskontrollierte DB-Aenderungen |
| 3.6 i18n optional | Nur DE-Texte | Optional DE/EN Ressourcensystem | L | Low | Low | 2.x | Umschaltbare Texte |

## 5) Discord UX/UI Verbesserungsdesign (nur Bot)

| Punkt | Vorher (typisch alt) | Nachher (konkret neuer Text/Flow) | Warum besser |
|---|---|---|---|
| Slash-Command UX | Uneinheitliche Antworten | Einheitliche Command-Response-Muster ueber `ResponseHelper` | Konsistente User-Erwartung |
| Antwortkonsistenz | Mischung aus Plaintext/Embeds | Success/Error/Info immer als definierte Embed-Typen | Schnell erfassbar |
| Fehler-/Hilfetexte | Technisch oder knapp | Klare Usertexte inkl. naechster Aktion | Weniger Frust, weniger Supportaufwand |
| Thinking/Deferred | Nur teilweise defer | Alle DB-lastigen Commands defer + edit response | Keine 3s Timeouts |
| Embed-Designsystem | Farben/Felder variieren | Definierte Palette, Header/Footer/Timestamp, standardisierte Felder | Professioneller Look |
| Buttons/Selects/Modals | Quasi nicht genutzt | Leaderboard-Paging per Buttons, Confirm-Flow per Modal | Bessere Interaktion ohne Command-Spam |
| Permission Feedback | Manuell und inkonsistent | Einheitlicher Permission Guard + Ephemeral-Error mit benoetigter Berechtigung | Klarer und sicherer |
| Optional DE/EN i18n | Nur DE | Ressourcenbasiert, Fallback DE | Skalierbar fuer mehr Communities |

## 6) Produktionsreife: Ubuntu + Docker + CI/CD

### 6.1 Laufzeit auf Ubuntu Home Server

- Betriebsmodell: Docker Compose (optional durch Systemd-Unit gestartet)
- Verzeichnislayout: `/opt/globalstatsbot/` mit `docker-compose.yml`, `.env`, `database/`, `backups/`
- Volumes: DB-Volume persistent
- Restart-Policy: `unless-stopped`
- Backup-Strategie: taeglicher `mysqldump` + rotierende Backups
- Update ohne lange Downtime: nur `bot` neu starten, DB weiterlaufen lassen

### 6.2 Docker

- Multi-stage Dockerfile (`sdk` -> `runtime`)
- Security-Hardening:
  - non-root user
  - minimales Runtime-Image
  - read-only filesystem optional (`read_only: true` in Compose)
- Healthcheck fuer Bot-Prozess

### 6.3 Docker Compose

- Services:
  - `bot`
  - optional `redis`/`postgres` **ANNAHME:** aktuell nicht noetig, MariaDB bleibt Standard
- `.env`-Einbindung fuer Secrets
- Logging-Optionen (json-file, Rotation)
- Produktionsnahes Compose-Template:

```yaml
services:
  db:
    image: mariadb:11.4
    restart: unless-stopped
    environment:
      MYSQL_ROOT_PASSWORD: ${MYSQL_ROOT_PASSWORD}
      MYSQL_DATABASE: ${MYSQL_DATABASE:-discord_identity}
      MYSQL_USER: ${DB_USER}
      MYSQL_PASSWORD: ${DB_PASSWORD}
    volumes:
      - mariadb_data:/var/lib/mysql
      - ./database/db.sql:/docker-entrypoint-initdb.d/01-schema.sql:ro
    healthcheck:
      test: ["CMD", "healthcheck.sh", "--connect", "--innodb_initialized"]
      interval: 15s
      timeout: 5s
      retries: 5
      start_period: 30s

  bot:
    image: ghcr.io/zyberphil/global_activity_bot_v2:latest
    restart: unless-stopped
    environment:
      DISCORD_BOT_TOKEN: ${DISCORD_BOT_TOKEN}
      DB_CONNECTION_STRING: ${DB_CONNECTION_STRING}
      DOTNET_ENVIRONMENT: Production
    depends_on:
      db:
        condition: service_healthy
    logging:
      driver: json-file
      options:
        max-size: "10m"
        max-file: "3"

volumes:
  mariadb_data:
```

### 6.4 GitHub Actions CI/CD

- Stages: restore -> build -> test -> format/lint -> security scan -> docker build/push -> deploy
- Branch-Strategie:
  - `dev`: Integration
  - `main`: Produktion
  - `release/*`: Stabilisierungszweig optional
- Secrets:
  - `DISCORD_BOT_TOKEN`
  - `DB_CONNECTION_STRING`
  - `SSH_PRIVATE_KEY`, `SSH_HOST`, `SSH_USER`
- Rollback:
  - Vor Deploy altes Image als `backup-<timestamp>` taggen
  - Bei Fehler auf Backup-Tag zurueck

Beispiel-Workflow (verkürzt):

```yaml
name: ci-cd
on:
  push:
    branches: ["main", "dev"]
  pull_request:

jobs:
  build-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "9.0.x"
      - run: dotnet restore
      - run: dotnet build --configuration Release --no-restore
      - run: dotnet test --configuration Release --no-build
      - run: dotnet format --verify-no-changes

  docker:
    if: github.ref == 'refs/heads/main'
    needs: build-test
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: docker/login-action@v3
        with:
          registry: ghcr.io
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}
      - uses: docker/build-push-action@v6
        with:
          context: .
          push: true
          tags: ghcr.io/zyberphil/global_activity_bot_v2:latest
```

## 7) Code-Qualitaet & Engineering-Standards

- C# Guidelines strikt anwenden (PascalCase, saubere Naming-Konventionen)
- Nullable Reference Types aktiv
- Analyzers aktivieren, Warnings in Release als Errors
- `dotnet format` verpflichtend in CI
- Testpyramide:
  - Unit-Tests (Service-Logik)
  - Integration-Tests (DbContext + SQL)
  - Contract-Tests fuer externe APIs (falls genutzt)
- Definition of Done:
  - Build/Test/Format gruen
  - Security checks ohne kritische Findings
  - Doku aktualisiert
- PR Checklist + Versioning (SemVer + Release Notes)

## 8) Sicherheits-Checkliste (nur Bot)

- Token/Secrets nie im Repo, nur ENV/Secret Store
- Input Validation fuer alle Command-Parameter
- Anti-Spam/Rate Limits/Cooldowns auf XP + Commands
- Permission/Rollenpruefung zentral und testbar
- Sichere Fehlerausgaben (keine internen Details fuer User)
- Dependency/Container Security Scans in CI
- Recovery bei API-Ausfaellen:
  - Discord reconnect
  - Backoff/Retry fuer externe APIs

## 9) 4-Wochen-Roadmap

### Woche 1
- Ziele: Grundgeruest, Bot online, Docker lokal
- Tasks: Phase 1 komplett
- Deliverables: laufender v5-Bot mit `/ping`
- DoD: Build + Start + Ping stabil
- Risiken: v5 API-Unterschiede -> Gegenmassnahme: kleine Spike-Implementationen

### Woche 2
- Ziele: XP, Profil, Top, Badge
- Tasks: Phase 2 Kernfeatures (2.1-2.7)
- Deliverables: Funktionsparitaet zu v4 fuer Hauptcommands
- DoD: Kerncommands und XP-Tracking produktionsnah
- Risiken: SQL/Entity Mapping -> Gegenmassnahme: fruehe Integrationstests

### Woche 3
- Ziele: Stabilitaet, Error-Handling, Tests
- Tasks: 2.8-2.11 + Testabdeckung ausbauen
- Deliverables: robuste Fehlerpfade, reproduzierbare Tests
- DoD: keine ungefangenen Errors, Tests in CI
- Risiken: Race Conditions -> Gegenmassnahme: DB-Constraints + idempotente Flows

### Woche 4
- Ziele: CI/CD, Deployment, Monitoring
- Tasks: Phase 3 priorisierte Punkte (3.1, 3.3, 3.4)
- Deliverables: automatisiertes Deploy auf Ubuntu Home Server
- DoD: Push auf `main` deploybar mit Rollback
- Risiken: SSH/Netzwerk/Firewall -> Gegenmassnahme: Dry Runs + fallback script

## 10) Sofort nutzbare Codebeispiele (C#, DSharpPlus v5)

Die folgenden Snippets sind als sofort umzusetzen geplant:
1. Globaler Error-Handler
2. Einheitliche Response/Embed-Factory
3. Standardisierte Success/Error/Info Responses
4. Slash Command mit Button + Modal
5. Permission Guard
6. Config + Secret Handling (`appsettings` + env)
7. Typed HttpClient + Retry/Timeout (Polly)
8. Background Service Beispiel
9. Unit Test Beispiel fuer Service/Command-Handler
10. Minimaler `Program.cs` Bootstrap mit DI und Logging

Umsetzungsvorgabe:
- Moderne .NET Patterns: Host Builder, DI, ILogger, Options Pattern
- Snippets so halten, dass sie mit minimalen Anpassungen lauffaehig sind

## 11) Migrationsmatrix v4 -> v5

| v4-Komponente | v5-Ersatz/Pattern | Migrationsansatz | Risiko | Teststrategie |
|---|---|---|---|---|
| `DiscordClient` + `UseSlashCommands` | `DiscordClientBuilder` + Commands Extension | Komplett neu aufsetzen | Med | Smoke + Integration |
| `ApplicationCommandModule` v4 | v5 Commands API | Modulweise Neuentwicklung | Med | Command-Tests |
| MessageCreated Handler direkt | v5 EventHandler-Konfiguration | Handler neu anbinden | Low | Event-Integrationstest |
| Konkrete Services ohne Interfaces | Service-Interfaces + DI | Schrittweise Entkopplung | Low | Unit-Tests pro Interface |
| Lowercase EF-Modelle | PascalCase Entities + Mapping | Technischer Refactor | Low | Mapping-Integrationstest |
| Manuelle Permission-Checks | Zentraler Guard/Checks | Querschnittsrefactor | Low | Permission-Tests |
| `dotenv.net` | Native .NET Config | Entfernen | Low | Start-up Konfigurationstest |

## 12) Offene Punkte / Entscheidungen (ADR-Kandidaten)

| Entscheidung | Optionen | Empfehlung | Auswirkungen |
|---|---|---|---|
| .NET Version | 8 LTS / 9 | .NET 9 (ANNAHME: Zielsystem kompatibel) | Neuere APIs, genaue Runtime in Docker pinnen |
| DB-Migrationsstrategie | Raw SQL / EF Migrations / Hybrid | Hybrid (bestehendes SQL behalten, neue Aenderungen via EF) | Minimales Risiko bei Migration |
| Level-Formel | linear / sqrt-basiert | Eine Formel verbindlich fuer C# + SP festlegen | Konsistente UX |
| Architektur-Tiefe | Einfacher Monolith / Clean Layers | Clean Layers im Monolith | Wartbar fuer kleines Team |
| i18n Scope | Nur DE / DE+EN | DE jetzt, EN optional in Phase 3 | Time-to-market bleibt hoch |
| Premium/Subscriptions | jetzt / spaeter | spaeter (ANNAHME: kein akuter Business-Bedarf) | Fokus auf Kernbot |

## Start heute - Checklist (Top 15 naechste Schritte)

1. DSharpPlus v5 Paketstand und kompatible Versionen final festlegen
2. Neue Solution + Projekte (`src`, `tests`) anlegen
3. Basis `Program.cs` mit DI/Logging/Options erstellen
4. Config-Dateien + `.env.example` definieren
5. `BotOptions` und `DatabaseOptions` implementieren
6. `/ping` als ersten End-to-End Command bauen
7. `BotDbContext` + Kernentities in PascalCase mappen
8. `IUserService`, `IGuildService`, `IStatsService` Interfaces definieren
9. XP-Message-Handler in v5 Eventing integrieren
10. `/me` Command + `ProfileService` implementieren
11. `/top global|guild|channel` migrieren
12. `/badge give|list|listall` migrieren
13. Globalen Error-Handler + `EmbedFactory` einbauen
14. Dockerfile + `docker-compose.yml` fuer Ubuntu Production finalisieren
15. GitHub Actions Workflow fuer Build/Test/Deploy aufsetzen

