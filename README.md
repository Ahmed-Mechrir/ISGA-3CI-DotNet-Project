# OWASP ASVS 4.0 — Interactive Security Checklist

A web application built with **ASP.NET Core (.NET 9) MVC** that lets you go
through the **OWASP Application Security Verification Standard (ASVS) 4.0**
requirements, track your project's compliance status, persist progress in a
local SQLite database, and get **AI-generated explanations** of each
requirement (powered by Google Gemini).

Built for the **ISGA 3CI** .NET project.

> **Author:** Ahmed Mechrir

---

## Features

- **Full ASVS 4.0 checklist** — 14 categories (Architecture, Authentication,
  Session Management, Access Control, Input Validation, Cryptography, Error
  Handling, Data Protection, Communication Security, Malicious Code, Business
  Logic, Files & Resources, API/Web Service, Configuration).
- **Filter by level (1 / 2 / 3)**, by category, by sub-category, and free-text
  search on ID, description, area, CWE, etc.
- **Per-requirement state** — *Conforme*, *Non conforme*, *En cours*, *Non
  applicable* — stored in SQLite (`asvs_checklist.db`).
- **Source-code reference / tool used / free comment** attached to every
  requirement.
- **Dashboard & chart** — totals, progression percentage, per-category bar
  chart (Chart.js).
- **CSV export** of the current filtered view.
- **AI explanation per requirement** — sends the requirement to the Gemini
  API and returns a structured French explanation (what it means / why it
  matters / how to implement / recommended tools), optionally tailored to a
  given tech stack (Angular, .NET, Spring Boot, Flutter, etc.).
- **Dark "cyberpunk" UI** with collapsible sidebar.

---

## Tech stack

| Layer       | Choice                                     |
|-------------|--------------------------------------------|
| Backend     | ASP.NET Core 9 MVC + Web API               |
| Database    | SQLite via Entity Framework Core 9         |
| Frontend    | Razor views + vanilla JS + Chart.js        |
| AI          | Google Gemini (`generativelanguage.googleapis.com`) |
| Container   | Docker (multi-stage, `mcr.microsoft.com/dotnet/aspnet:9.0`) |

---

## Project layout

```
Controllers/
  HomeController.cs          MVC controller (Index, Stats, Error)
  ChecklistController.cs     REST API under /api/checklist/*
Models/
  AppDbContext.cs            EF Core DbContext (Progress table)
  AsvsRequirement.cs         In-memory model loaded from JSON
  RequirementProgress.cs     Persisted state per requirement
Services/
  AsvsDataService.cs         Loads the 14 JSON files at startup
Views/
  Home/Index.cshtml          Main SPA-like checklist page
  Shared/_Layout.cshtml      Layout + Chart.js include
wwwroot/
  assets/data/*.json         Source of truth for ASVS requirements
  css/site.css               Dark theme
  js/site.js                 Client-side rendering + API calls
asvs_checklist.db            SQLite database (auto-created on first run)
Dockerfile                   Multi-stage build
```

---

## Running it

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- (Optional) A free Gemini API key from
  [aistudio.google.com](https://aistudio.google.com/app/apikey) if you want
  the AI explanations.

### Locally

```bash
dotnet restore
dotnet run
```

Then open <http://localhost:5259>.

### With Docker

```bash
docker build -t owasp-asvs .
docker run -p 8080:8080 owasp-asvs
```

Then open <http://localhost:8080>.

---

## Gemini API key

The key can be supplied in two ways:

1. **Per-browser** — click the **🔑 API Key** button in the header and paste
   your key (stored in `localStorage` only).
2. **Server-side default** — set it in `appsettings.json`:
   ```json
   "Gemini": {
     "ApiKey": "AIza...",
     "DefaultModel": "gemini-3-flash-preview"
   }
   ```

> Do **not** commit a real API key. Treat `appsettings.json` as
> environment-specific.

---

## REST API summary

| Method | Route                                | Description                                    |
|--------|--------------------------------------|------------------------------------------------|
| GET    | `/api/checklist/requirements`        | List requirements (filterable by level/cat/q)  |
| GET    | `/api/checklist/categories`          | Categories + sub-areas + per-area progress     |
| GET    | `/api/checklist/stats`               | Dashboard counters + per-level + per-category  |
| POST   | `/api/checklist/progress/{id}`       | Save status / comment / tool for a requirement |
| DELETE | `/api/checklist/progress`            | Reset **all** progress                         |
| GET    | `/api/checklist/export`              | Export the current filter as CSV               |
| POST   | `/api/checklist/explain/{id}`        | Ask Gemini to explain a requirement            |
| GET    | `/api/checklist/gemini-models`       | List of selectable Gemini models               |
