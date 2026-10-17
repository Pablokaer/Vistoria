# 0025 — Uploaded photos lose their GPS position, nothing else

**Context.** Phone photos usually carry the place where they were taken (EXIF GPS, sometimes also in XMP). Inspection photos were stored exactly as uploaded and shown to tenants and through public share links, so anyone could read the coordinates. The position points at the property, whose address is already in the report, so the leak is small. The rest of the metadata is useful, though: the date taken can be evidence in a dispute, and the orientation makes the photo display the right way up. The product owner chose to remove **only the GPS data**.

**Decision.**

- **Where.** `MediaService` calls `PhotoLocationScrubber.RemoveLocation(bytes, detectedType)` after the content type is detected from the file's first bytes and **before** the SHA-256 is computed and the file is saved. The stored hash is therefore the hash of the stored bytes. The audit record of the upload notes `locationRemoved`.
- **In place, no re-encoding.** The file keeps its length and its pixels. Formats handled:
  - **EXIF (TIFF block)** in JPEG APP1, the PNG `eXIf` chunk and the WebP `EXIF` chunk, in both byte orders: the GPS sub-IFD's entries and their out-of-line values are zeroed and its entry count becomes 0. IFD0 keeps pointing at the empty IFD, because a 0 pointer confuses some readers. Every other tag (orientation, date taken, camera) stays byte for byte.
  - **XMP** in JPEG APP1 (standard and extended), uncompressed PNG `iTXt` and the WebP `XMP ` chunk: the values of `*:GPS*` attributes and elements become spaces of the same length.
  - **PNG CRCs:** a changed PNG chunk gets its CRC-32 recomputed (`PngCrc32`, no package).
- **Never fails an upload.** Every offset is bounds-checked. Malformed, truncated or unknown structures are left untouched. The XMP regular expressions have a 250 ms timeout, and a timeout leaves the packet as it was.
- **No new dependency.** Image libraries would re-encode (losing quality and metadata) or bring licence and native-binary concerns.

**Consequences.**

- Tenants and share-link viewers no longer get the camera position. The date taken and the orientation still reach the report.
- Not covered: compressed PNG `iTXt` XMP, vendor-specific location tags outside the GPS IFD and `*:GPS*` XMP names (for example `drone-dji:GpsLatitude`), and HEIC (not accepted for upload).
- Photos uploaded before this change keep their GPS data. Re-processing stored files would change their recorded SHA-256, and finalized reports reference those hashes.
