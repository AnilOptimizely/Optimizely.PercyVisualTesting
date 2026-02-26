# Optimizely.Percy — Visual Testing for Optimizely CMS 12

> Automatically detect unintended visual changes every time content is published.  
> Powered by [Percy (BrowserStack)](https://www.browserstack.com/percy) and built for Optimizely CMS 12.

---

## Table of Contents

- [What Is It?](#what-is-it)
- [Why Visual Testing Matters](#why-visual-testing-matters)
- [Key Features](#key-features)
  - [Automatic Snapshots on Publish](#1-automatic-snapshots-on-publish)
  - [Multi-Viewport Responsive Testing](#2-multi-viewport-responsive-testing)
  - [Admin Dashboard](#3-admin-dashboard)
  - [Batch Snapshot Testing](#4-batch-snapshot-testing)
  - [Content Type Filtering](#5-content-type-filtering)
  - [Percy API Integration](#6-percy-api-integration)
  - [REST API for Editor UI](#7-rest-api-for-editor-ui)
- [How It Helps Your Team](#how-it-helps-your-team)
  - [For Content Editors](#-for-content-editors)
  - [For Developers](#-for-developers)
  - [For UX Designers](#-for-ux-designers)
  - [For Product Teams](#-for-product-teams)
  - [For Users / End Visitors](#-for-users--end-visitors)
- [Getting Started](#getting-started)
  - [1. Install the NuGet Package](#1-install-the-nuget-package)
  - [2. Register Services](#2-register-services)
  - [3. Configure Options](#3-configure-options)
- [Configuration Reference](#configuration-reference)
- [Architecture Overview](#architecture-overview)
- [Example Workflows](#example-workflows)
  - [Publishing a Page](#publishing-a-page)
  - [Manually Triggering a Snapshot](#manually-triggering-a-snapshot)
  - [Reviewing Builds via the API](#reviewing-builds-via-the-api)
- [Frequently Asked Questions](#frequently-asked-questions)
- [License](#license)

---

## What Is It?

**Optimizely.Percy** is a NuGet package that integrates [Percy by BrowserStack](https://www.browserstack.com/percy) — a visual regression testing platform — directly into Optimizely CMS 12. It captures pixel-perfect screenshots of your CMS pages at multiple viewport sizes, compares them against a visual baseline, and highlights any differences, so your team can catch unintended layout, styling, or content changes before they reach your visitors.

---

## Why Visual Testing Matters

| Without visual testing | With Optimizely.Percy |
|---|---|
| Visual regressions ship to production unnoticed | Every publish is automatically screenshotted and compared |
| Manual QA across devices is slow and error-prone | Multi-viewport snapshots cover mobile, tablet, and desktop in seconds |
| Content editors have no visibility into visual impact | Editors and reviewers see diffs directly in the Percy dashboard |
| Design drift accumulates over time | Pixel-level baselines keep the design consistent |
| Release sign-off relies on gut feeling | Objective, side-by-side visual evidence for every change |

---

## Key Features

### 1. Automatic Snapshots on Publish

When a content editor publishes a page in Optimizely CMS, the package automatically triggers a Percy snapshot — no manual steps required. This is powered by an initializable module (`ContentPublishPercyHandler`) that listens to the CMS `PublishedContent` event.

- **Zero friction** for editors — publishing works exactly the same as before.
- **Fire-and-forget** — the snapshot runs asynchronously and never blocks the publish pipeline.
- **Configurable** — disable automatic triggers with `TriggerOnPublish: false` if you prefer manual control.

### 2. Multi-Viewport Responsive Testing

Every snapshot is captured at multiple viewport widths to test responsive behavior out of the box.

**Default widths:** `375px` (mobile), `768px` (tablet), `1280px` (desktop)

You can customise the widths and minimum height to match your design breakpoints:

```json
{
  "Optimizely": {
    "Percy": {
      "SnapshotWidths": [320, 768, 1024, 1440],
      "MinHeight": 900
    }
  }
}
```

This means a single page publish generates snapshots across **all configured viewports**, giving your team comprehensive responsive coverage automatically.

### 3. Admin Dashboard

A built-in admin dashboard is available at `/percy-admin` (restricted to CMS admin users). It provides:

- **Configuration status** — at-a-glance indicator showing whether Percy is properly configured.
- **Recent builds list** — see the status (Finished, Pending, Failed), snapshot count, comparison count, and unreviewed comparisons for each build.
- **Manual trigger form** — enter one or more relative URLs and an optional label to capture on-demand snapshots.
- **Build links** — click through directly to the Percy web app to review visual diffs.
- **Configuration reference** — embedded JSON example of all available settings.

### 4. Batch Snapshot Testing

Capture multiple pages in a single Percy build using the batch snapshot feature. This is useful for:

- **Full-site regression testing** before a release.
- **Spot-checking** a set of critical pages after a CMS upgrade or theme change.
- **Manual QA workflows** where an editor selects specific pages to verify.

Batch snapshots can be triggered via the admin dashboard or the REST API:

```http
POST /percy-admin/trigger
Content-Type: application/json

{
  "urls": ["/en/home", "/en/about", "/en/products"],
  "label": "pre-release-check"
}
```

### 5. Content Type Filtering

Control which content types are included in automatic publish snapshots using include/exclude lists:

```json
{
  "Optimizely": {
    "Percy": {
      "IncludeContentTypes": ["StandardPage", "ProductPage", "LandingPage"],
      "ExcludeContentTypes": ["MediaData", "ImageFile"]
    }
  }
}
```

- **IncludeContentTypes** — when set, _only_ these types trigger snapshots (whitelist).
- **ExcludeContentTypes** — these types are _always_ skipped (blocklist).
- Leave both empty to snapshot all publishable content types.

### 6. Percy API Integration

The package includes a full HTTP client (`IPercyApiClient`) that communicates with the Percy REST API (`https://percy.io/api/v1/`), enabling:

| Method | Description |
|---|---|
| `GetBuildsAsync(projectSlug, limit)` | List recent builds for your project |
| `GetBuildAsync(buildId)` | Get details for a specific build |
| `GetSnapshotsForBuildAsync(buildId)` | List all snapshots within a build |
| `GetBuildWebUrlAsync(buildId)` | Get the browser URL for a build's diff view |

### 7. REST API for Editor UI

A dedicated API controller (`/api/percy`) is available for CMS editors (requires `episerver:cmseditor` policy) to:

- **GET `/api/percy/builds`** — retrieve recent builds as JSON.
- **GET `/api/percy/builds/{buildId}/diff`** — get the diff URL for a specific build.
- **POST `/api/percy/snapshot`** — programmatically trigger a snapshot for a list of URLs.

This API can power custom editor gadgets, CI/CD integrations, or monitoring dashboards.

---

## How It Helps Your Team

### 📝 For Content Editors

| Benefit | Detail |
|---|---|
| **Publish with confidence** | Every time you publish a page, Percy automatically screenshots it and compares it to the previous version. If something looks wrong, you'll know immediately. |
| **No extra steps** | Visual testing runs silently in the background — your publishing workflow doesn't change at all. |
| **See what changed** | Open the Percy build link to see side-by-side visual diffs highlighting exactly what changed on the page. |
| **Catch content mistakes** | Accidentally delete a block? Move an image to the wrong slot? Percy's diff will show the visual impact right away. |
| **Multi-device preview** | Snapshots at mobile, tablet, and desktop widths let you verify that your content looks correct on every device. |

### 👩‍💻 For Developers

| Benefit | Detail |
|---|---|
| **Automated visual regression testing** | Catch CSS regressions, broken layouts, or missing assets without writing browser tests by hand. |
| **CI/CD friendly** | Trigger batch snapshots from your deployment pipeline to validate releases before going live. |
| **Extensible API** | Use `IPercySnapshotService` and `IPercyApiClient` via dependency injection to build custom workflows, gadgets, or integrations. |
| **Clean architecture** | The package follows standard ASP.NET Core patterns — DI registration, options configuration, typed HTTP clients — making it easy to understand and extend. |
| **Content type filtering** | Focus snapshots on the page types that matter and ignore media or utility content. |
| **Custom CSS injection** | Use `PercyCss` to hide dynamic elements (carousels, ads, timestamps) that would create false-positive diffs. |

### 🎨 For UX Designers

| Benefit | Detail |
|---|---|
| **Visual consistency enforcement** | Maintain pixel-level fidelity to your designs across every page and every release. |
| **Responsive design validation** | Verify that layouts adapt correctly at every configured breakpoint — mobile, tablet, and desktop. |
| **Design drift detection** | Over time, small incremental changes can drift away from the original design. Percy baselines catch this automatically. |
| **Stakeholder communication** | Share Percy build links to show stakeholders exactly what visual changes are included in a release. |
| **Selective element hiding** | Use `IgnoreSelectors` to exclude dynamic content (e.g., live feeds, A/B test variations) from comparisons so reviews focus on intentional design changes. |

### 📊 For Product Teams

| Benefit | Detail |
|---|---|
| **Quality gate for releases** | Require Percy builds to pass (no unreviewed visual changes) before signing off on a release. |
| **Audit trail** | Every build is recorded with timestamps, labels, snapshot counts, and diff URLs — providing a complete visual history of your site. |
| **Risk reduction** | Reduce the risk of shipping broken pages by adding automated visual checks to your publishing and deployment workflows. |
| **Dashboard visibility** | The admin dashboard gives product managers a non-technical view of build status, recent comparisons, and configuration health. |
| **Batch site reviews** | Trigger a full-site visual review before major content launches, CMS upgrades, or design system updates. |

### 👥 For Users / End Visitors

While end visitors don't interact with this package directly, they benefit from:

- **Consistent, polished experiences** — visual bugs are caught before they reach production.
- **Reliable responsive layouts** — pages render correctly across devices and screen sizes.
- **Faster issue resolution** — when visual bugs are detected early, fixes ship sooner.
- **Higher overall quality** — teams that adopt visual testing deliver more reliable digital experiences.

---

## Getting Started

### 1. Install the NuGet Package

```bash
dotnet add package Optimizely.Percy
```

### 2. Register Services

In your `Startup.cs` or `Program.cs`, register the Percy services:

```csharp
using Optimizely.Percy.Extensions;

builder.Services.AddPercyVisualTesting(builder.Configuration);
```

This single call registers:

- `IPercySnapshotService` — for triggering snapshots and fetching build data.
- `IPercyApiClient` — typed HTTP client for the Percy REST API.
- `IPageUrlResolver` — resolves Optimizely content to public URLs.
- `PercyOptions` — bound from `Optimizely:Percy` configuration section.

### 3. Configure Options

Add the following to your `appsettings.json`:

```json
{
  "Optimizely": {
    "Percy": {
      "Token": "YOUR_PERCY_TOKEN",
      "BaseUrl": "https://your-cms-site.com",
      "ProjectSlug": "your-org/your-project",
      "TriggerOnPublish": true,
      "SnapshotWidths": [375, 768, 1280],
      "MinHeight": 1024,
      "EnableDashboard": true,
      "IncludeContentTypes": [],
      "ExcludeContentTypes": [],
      "IgnoreSelectors": [],
      "PercyCss": "",
      "PercyCliPath": "npx"
    }
  }
}
```

> **Tip:** Store your `Token` in a secure secrets manager (e.g., Azure Key Vault, environment variables) rather than in `appsettings.json`.

---

## Configuration Reference

| Option | Type | Default | Description |
|---|---|---|---|
| `Token` | `string` | `""` | **(Required)** Percy API token (`PERCY_TOKEN`). |
| `BaseUrl` | `string` | `""` | Base URL of the CMS site for generating absolute snapshot URLs. |
| `ProjectSlug` | `string` | `""` | Percy project identifier in `org/project` format for API calls. |
| `TriggerOnPublish` | `bool` | `true` | Automatically trigger snapshots when content is published. |
| `SnapshotWidths` | `int[]` | `[375, 768, 1280]` | Viewport widths (in pixels) for each snapshot. |
| `MinHeight` | `int` | `1024` | Minimum viewport height (in pixels) for snapshots. |
| `EnableDashboard` | `bool` | `true` | Enable or disable the admin dashboard at `/percy-admin`. |
| `IncludeContentTypes` | `string[]` | `[]` | Whitelist of content type names to snapshot (empty = all). |
| `ExcludeContentTypes` | `string[]` | `[]` | Blocklist of content type names to exclude from snapshots. |
| `IgnoreSelectors` | `string[]` | `[]` | CSS selectors for elements to hide in snapshots. |
| `PercyCss` | `string` | `""` | Custom CSS injected into each snapshot. |
| `PercyCliPath` | `string` | `"npx"` | Path or command to invoke the Percy CLI. |

---

## Architecture Overview

```
┌──────────────────────────────────────────────────────────┐
│                   Optimizely CMS 12                      │
│                                                          │
│  ┌─────────────────┐    PublishedContent Event            │
│  │  Content Editor  │───────────────────────┐            │
│  │  publishes page  │                       │            │
│  └─────────────────┘                       ▼            │
│                              ┌──────────────────────┐    │
│                              │ ContentPublishPercy-  │    │
│                              │ Handler               │    │
│                              │ (Initializable Module) │    │
│                              └──────────┬───────────┘    │
│                                         │                │
│                                         ▼                │
│                              ┌──────────────────────┐    │
│  ┌─────────────────┐         │ IPercySnapshotService │    │
│  │  Admin Dashboard │────────│                      │    │
│  │  /percy-admin    │        │  • TriggerSnapshot    │    │
│  └─────────────────┘        │  • TriggerBatch       │    │
│                              │  • GetRecentBuilds    │    │
│  ┌─────────────────┐         │  • GetBuildDiffUrl    │    │
│  │  REST API        │────────│                      │    │
│  │  /api/percy      │        └──────────┬───────────┘    │
│  └─────────────────┘                   │                │
│                                         ▼                │
│                              ┌──────────────────────┐    │
│                              │   Percy CLI           │    │
│                              │   (npx percy snapshot)│    │
│                              └──────────┬───────────┘    │
│                                         │                │
└─────────────────────────────────────────┼────────────────┘
                                          │
                                          ▼
                               ┌──────────────────────┐
                               │   Percy (BrowserStack)│
                               │   Cloud Service       │
                               │                      │
                               │  • Renders pages     │
                               │  • Compares snapshots │
                               │  • Generates diffs   │
                               └──────────────────────┘
```

**Flow:**

1. A content editor publishes a page in Optimizely CMS.
2. The `ContentPublishPercyHandler` event handler fires asynchronously.
3. It calls `IPercySnapshotService.TriggerSnapshotAsync()`, which resolves the page URL, generates a YAML configuration, and executes the Percy CLI.
4. The Percy CLI sends the page to Percy's cloud service, which renders it at the configured viewport widths and compares the screenshots against the visual baseline.
5. Results are available in the Percy web app and can be retrieved via `IPercyApiClient` or the admin dashboard.

---

## Example Workflows

### Publishing a Page

1. Editor opens a page in Optimizely CMS and clicks **Publish**.
2. The package detects the publish event and resolves the page URL.
3. A Percy snapshot is triggered automatically (if `TriggerOnPublish` is enabled).
4. Within minutes, the Percy dashboard shows the new build with visual diffs.
5. The team reviews the diffs — approve if the changes are intentional, or flag if something looks wrong.

### Manually Triggering a Snapshot

1. Navigate to `/percy-admin` in your browser (requires CMS admin access).
2. Enter one or more URLs in the **Trigger Snapshot** form, e.g.:
   ```
   /en/home
   /en/about
   /en/products/featured
   ```
3. Optionally add a build label like `pre-release-v2.5`.
4. Click **Trigger Snapshot**.
5. The batch snapshot runs and appears in the **Recent Builds** section.

### Reviewing Builds via the API

The CMS REST API endpoints (`/api/percy`) are protected by the `episerver:cmseditor` authorization policy, so you must authenticate using your CMS credentials (e.g., a cookie-based session or the authentication mechanism configured in your Optimizely site):

```bash
# Get recent builds (authenticate with your CMS session)
curl -b YOUR_CMS_AUTH_COOKIE \
     https://your-site.com/api/percy/builds?count=5

# Get diff URL for a specific build
curl -b YOUR_CMS_AUTH_COOKIE \
     https://your-site.com/api/percy/builds/BUILD_ID/diff
```

---

## Frequently Asked Questions

**Q: Does this package slow down content publishing?**  
A: No. Snapshots run asynchronously in the background using a fire-and-forget pattern. The publish pipeline returns immediately.

**Q: What happens if Percy is not configured or the token is missing?**  
A: The package gracefully skips snapshot operations and logs a warning. Publishing continues normally.

**Q: Can I use this with Optimizely CMS 11?**  
A: This package targets .NET 8 and Optimizely CMS 12. It is not compatible with CMS 11 (which runs on .NET Framework).

**Q: How do I hide dynamic elements (e.g., timestamps, carousels) from comparisons?**  
A: Use the `IgnoreSelectors` option to specify CSS selectors for elements to hide, or use `PercyCss` to inject custom CSS that hides or stabilizes dynamic content.

**Q: Is the Percy token stored securely?**  
A: The token is read from configuration. We recommend storing it in environment variables or a secrets manager (e.g., Azure Key Vault) rather than in plain text config files.

**Q: Can I trigger snapshots from a CI/CD pipeline?**  
A: Yes. Use the REST API (`POST /api/percy/snapshot`) or the batch snapshot service to integrate visual testing into your deployment workflow.

---

## License

This project is licensed under the [MIT License](LICENSE).
