# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## MongoGenericRepository / MongoGenericRepository.HealthChecks

### [Unreleased]

#### Added

- Documentation site with API reference (Blazor WebAssembly + GitHub Pages)
- GitHub Actions CI/CD workflows
- Health check add-on package (`MongoGenericRepository.HealthChecks`)

### [10.2.0]

#### Changed

- Upgraded to MongoDB.Driver 3.6.0
- Multi-target support for net8.0, net9.0, net10.0, and netstandard2.1

#### Added

- `EntityContext` with static `MongoClient` connection caching
- Read/write connection separation for replica set deployments
- `EntityDatabaseAttribute` and `EntityCollectionAttribute` for custom naming
- Pagination support (`page`, `pageSize`) on `GetAll` methods
- `CancellationToken` support on all repository methods
- `Count` overloads with `FilterDefinition`, JSON filter, and expression filter
- Bulk `Update` and `Delete` operations

For older versions, see [GitHub Releases](https://github.com/emuuu/MongoRepository/releases).
