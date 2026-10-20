# Contributing to Sprintle

First off, thank you for considering contributing to Sprintle! 

## Getting Started

1. Ensure you have the .NET 10.0 SDK installed.
2. Clone the repository.
3. Run `dotnet restore` and `dotnet build` to ensure the project builds locally.
4. Run `dotnet test` to verify all tests pass.

## Branching Strategy

- `main`: Stable release branch.
- `dev`: Active development branch.
- Feature branches should be created off of `dev` (e.g. `feature/add-new-service`).

## Pull Requests

1. Create your feature branch from `dev`.
2. Make sure your code passes all build warnings (treated as errors in this project) and tests.
3. Keep PRs small and focused on a single logical change.
4. Update documentation if your changes affect the API or project structure.

## Code Style

- The project uses `.editorconfig` to enforce code style. Ensure your IDE supports it.
- All code must compile with `TreatWarningsAsErrors=true`.
- We use Clean Architecture for all microservices. Please follow the `API -> Application -> Domain <- Infrastructure` dependency flow.
