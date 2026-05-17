# Boj.Rackspace

# Overview

Boj.Rackspace is an internal integration service
designed to centralize and simplify access to RackSpace APIs within the organization.

Main goals:

* Centralize RackSpace access into a single entry point
* Reduce integration complexity
* Support streaming file operations
* Manage authentication centrally
* Simplify RackSpace usage for internal systems
* Provide an internal SDK / Gateway for the organization

---

# Architecture Style

The system is designed based on:

```text id="arch-style"
Clean Architecture
SOLID Principle
Dependency Injection
Streaming First Design
```

---

# Project Goal

This system is not designed as a multi-cloud platform.

Instead, it is designed as a:

```text id="project-goal"
RackSpace Integration Provider
RackSpace Gateway API
RackSpace Client SDK
```

Because of this, RackSpace-specific naming is acceptable and recommended.

Examples:

* IRackSpaceObjectClient
* IRackSpaceContainerClient
* IRackSpaceAuthService

These abstractions accurately reflect the actual domain and responsibilities of the system.

---

# Recommended Structure

```text id="recommended-structure"
src/
 ├── Boj.Rackspace.Api
 │
 ├── Application
 │    ├── Abstractions
 │    │    ├── IRackSpaceAuthService
 │    │    ├── IRackSpaceObjectClient
 │    │    ├── IRackSpaceContainerClient
 │    │    └── ITokenProvider
 │    │
 │    ├── DTOs
 │    ├── Services
 │    └── Validators
 │
 ├── Domain
 │    ├── Models
 │    │    ├── RackSpaceObject
 │    │    ├── Container
 │    │    └── Metadata
 │    │
 │    └── ValueObjects
 │
 └── Infrastructure
      ├── Authentication
      │    ├── RackSpaceAuthService
      │    ├── TokenProvider
      │    ├── JwtHandler
      │    └── TokenCache
      │
      ├── Storage
      │    ├── RackSpaceObjectClient
      │    └── RackSpaceContainerClient
      │
      ├── Http
      └── Extensions
```

---

# Layer Responsibilities

# Application

Responsible for:

* Use cases
* Service contracts
* DTOs
* Validation
* Workflow orchestration

Examples:

```text id="application-examples"
IRackSpaceObjectClient
IRackSpaceContainerClient
```

The Application Layer owns the abstractions used by the system.

---

# Domain

Contains:

* Models
* Entities
* Value Objects

Examples:

```text id="domain-examples"
RackSpaceObject
Container
Metadata
```

The domain layer is intentionally lightweight
because this system is primarily an integration service,
not a complex business domain system.

---

# Infrastructure

Responsible for external integrations and technical implementations.

Examples:

* RackSpace API integration
* Authentication
* HttpClient
* Token management

Infrastructure implements the abstractions defined by the Application Layer.

---

# Dependency Direction

```text id="dependency-direction"
Infrastructure
    ↓ implements
Application Abstractions
```

The Application Layer does not know implementation details.

---

# Streaming First Design

This system is designed with:

```text id="streaming-first"
Streaming First
```

to support:

* Large file handling
* Low memory usage
* Better performance

---

# Recommended Download Pattern

```csharp id="download-pattern"
var response = await _httpClient.GetAsync(
    url,
    HttpCompletionOption.ResponseHeadersRead,
    ct);

return await response.Content
    .ReadAsStreamAsync(ct);
```

---

# Recommended Upload Pattern

```csharp id="upload-pattern"
using var content =
    new StreamContent(stream);

await _httpClient.PutAsync(
    url,
    content,
    ct);
```

---

# Important Design Principles

# 1. Do Not Over Engineer

This system is a:

```text id="integration-service"
RackSpace Integration Service
```

There is no need to introduce:

* Multi-cloud support
* Generic storage abstraction
* Event sourcing
* Complex DDD patterns

from the beginning.

---

# 2. Keep the Design Honest

This system works specifically with RackSpace.

Therefore, names such as:

```text id="specific-naming"
IRackSpaceObjectClient
```

are completely acceptable.

There is no need to prematurely abstract everything into:

```text id="premature-abstraction"
IStorageProvider
```

without real business requirements.

---

# 3. Infrastructure Handles Technical Details

The following concerns belong in Infrastructure:

* Token management
* JWT handling
* Authentication
* HttpClient
* Retry policies
* Cache management

These are technical concerns, not business rules.

---

# 4. Application Owns Contracts

The Application Layer owns the abstractions used by the system.

Example:

```text id="contract-example"
IRackSpaceObjectClient
```

Infrastructure provides the implementations.

---

# Recommended Starting Features

# Version 1

* Authenticate
* List Containers
* List Objects
* Upload File
* Download File
* Delete File
* Metadata
* Streaming Support

---

# Recommended Technologies

| Purpose       | Technology        |
| ------------- | ----------------- |
| API           | ASP.NET Core      |
| HTTP          | HttpClientFactory |
| Retry         | Polly             |
| Logging       | Serilog           |
| Validation    | FluentValidation  |
| Documentation | Swagger/OpenAPI   |

---
