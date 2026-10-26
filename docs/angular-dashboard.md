# Angular dashboard

Standalone Angular 22 application using built-in `@angular/build:application`, lazy-loaded Router features, HttpClient DTO service, Signals, RxJS polling every 30 seconds, typed Reactive Forms for filters, and Tailwind CSS v4 through PostCSS. Global CSS defines a responsive restaurant workspace with desktop sidebar and mobile navigation. No external image/font dependency is needed.

Overview reads live status, restaurant profile, and database readiness independently; unavailable database data never becomes fake revenue or orders. Menu displays backend categories/prices and bounded pages; search/category filters apply only to the current page and are labeled accordingly. Setup describes the local/hosted requirements. Future features are visibly scheduled, without clickable dead ends.

`public/config.json` contains only `apiBaseUrl`. Empty selects same-origin proxy during development. `scripts/write-config.mjs` writes the public value during Render builds. Runtime validation requires HTTPS except loopback. An exact origin is configured server-side. No frontend environment file can authorize payments or contain secrets.

Authentication, guards, interceptors, logout, write forms, image uploads, finance confirmation, and operational dashboards follow their later phases. The foundation has no pretend staff session. Tests use TestBed with Vitest and HttpTestingController. Playwright smoke tests exercise a seeded real API at desktop/mobile widths.
