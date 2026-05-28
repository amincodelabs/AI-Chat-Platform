# Security

## Secret Handling

Do not commit real secrets to this repository.

Use environment variables, local `.env` files, .NET User Secrets, or your production secret manager for:

- SQL Server passwords
- Redis passwords or connection strings
- Authentication keys or signing keys
- API keys and tokens
- Certificates and private keys

Local `.env` files, local appsettings overrides, certificates, and private keys are ignored by Git.

## Previously Committed Secrets

This repository previously contained development-style Docker password values in tracked configuration examples. Treat any committed password-like value as exposed.

Rotate any value that was copied from this repository into a real environment, including:

- SQL Server SA passwords
- Redis passwords or connection strings
- Development seed user passwords

## Local Secret Scanning

Run a local secret scan before pushing:

```bash
gitleaks detect --source . --redact
```

If GitHub secret scanning is available for the repository, keep it enabled.
