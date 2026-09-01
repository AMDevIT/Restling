# Multipart implementation

## Contracts

- `MultipartRequest` supports `form-data` by default and accepts any MIME multipart subtype.
- Every execution creates and owns fresh `HttpContent`; stream and content factories make request reuse explicit.
- Object parts use the client context's codec snapshot.
- `MultipartContentCodec` is a default reader for buffered `multipart/*` responses.
- `MultipartDocument` preserves preamble, epilogue, order, duplicates, and nested entities.
- `MultipartPart` exposes raw bytes and MIME metadata and can deserialize itself with the originating codec registry.
- `multipart/related` root selection and `multipart/byteranges` ranges have typed accessors.
- Signed and encrypted entities are parsed structurally but are not verified or decrypted.
- `multipart/x-mixed-replace` uses a dedicated `IAsyncEnumerable<MultipartPart>` API with response-header streaming.
- Parser limits cover part count, header bytes, nesting depth, and bytes per part.

## Verification state

Deterministic tests cover sending, codec-backed parts, nesting, related roots, duplicates, and mixed-replace streaming. Restore, build, and tests remain intentionally deferred.
