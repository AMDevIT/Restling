# Wiki update — 2026-09-10

## Objective and status

Updated `Restling.wiki` to document the functionality present on `origin/main`, while also documenting the development-only
status-based response mapping API at the user's request. The mapping page explicitly states that it is not included in the
current main release.

## Decisions

- Existing wiki pages were refreshed where the subject already existed.
- New pages cover content codecs, cookie persistence, multipart, proxy routing, ownership/disposal, and experimental status mapping.
- Documentation distinguishes released functionality from the status-mapping work present only in the development checkout.
- Cookie persistence documentation uses the current public provider contract and the optional JSON/SQLite packages; obsolete
  `ICookieStorageProviderEncrypter` examples were removed.

## Affected files

- `Restling.wiki/_Sidebar.md`
- `Restling.wiki/Home.md`
- `Restling.wiki/Client-Configuration.md`
- `Restling.wiki/Cookies.md`
- `Restling.wiki/Requests.md`
- `Restling.wiki/Responses-and-Errors.md`
- `Restling.wiki/Security.md`
- `Restling.wiki/Serialization.md`
- Added `Content-Codecs.md`, `Cookie-Persistence.md`, `Multipart.md`, `Proxy-Routing.md`,
  `Ownership-and-Disposal.md`, and `Experimental-Status-Mapping.md`.

## Checks performed

- Fetched repository references before analysis; no pull or merge was needed.
- Compared `origin/main` documentation/source with the wiki and excluded status mapping from the released-feature baseline.
- Checked the wiki worktree status and ran `git diff --check`.
- Checked internal wiki links and searched for obsolete cookie-encryption names.

## Open issues and recommended next step

- The wiki changes are not committed or pushed.
- Build and test execution were not run because this was a documentation-only task and the requested confirmation covered static
  documentation checks rather than solution verification.
