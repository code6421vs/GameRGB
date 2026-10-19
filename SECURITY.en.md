# Security Policy

[繁體中文](SECURITY.md) | **English**

## Reporting a vulnerability

Do not post exploit details, credentials, or complete diagnostic logs in a public issue. Use the GitHub repository's **Security → Report a vulnerability** feature. If private vulnerability reporting is unavailable, contact the repository owner and disclose only the minimum information necessary.

## Binary dependencies

GameRGB downloads and runs OpenRGB and, with explicit user approval, installs the PawnIO kernel driver. `setup.ps1` pins official HTTPS URLs and SHA-256 hashes and verifies PawnIO's Authenticode signature. Stop if any verification fails; do not disable the checks or use third-party mirrors.

When updating a dependency, maintainers must recheck its official release page, license, hash, and signature, then update both third-party notice files.
