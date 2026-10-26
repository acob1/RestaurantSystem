# Cloudinary storage architecture (Phase 5/6)

No upload endpoint or SDK adapter is active in Phase 1. A future infrastructure adapter will use the official Cloudinary .NET SDK; pin its version and add controlled upload doubles when implementing it.

Menu photos: authenticated staff upload through API, validate signatures/actual file format, bound dimensions/size, use public image delivery. Persist asset ID, public ID, version, resource type, delivery type, byte count, MIME and timestamps; replace/delete via the backend with audit events.

Receipts: fetch WhatsApp media only from trusted Graph/media endpoints after metadata validation. Stream with a hard byte limit, check actual format, compute SHA-256 and check duplicates, upload with delivery type `authenticated`. Persist identifiers and hash, never an unrestricted URL. Access requires staff finance policy and short-lived server-generated authorization. Audit upload/read/delete and enforce retention. Verify the selected free account actually supports authenticated receipt delivery before enabling it; otherwise keep evidence ingestion disabled.

OCR is optional and informational. Staff assess evidence separately from verifying receipt of funds. Development approval must be explicit and only work in a server-enforced test environment. Financial verification is never inferred from an image.

Cloud name is public; API key/secret remain backend-only environment secrets. No storage on Render local disks. Deletion must be idempotent and coordinated with retention, audit, and provider failure retries.
