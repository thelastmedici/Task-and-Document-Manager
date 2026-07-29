# Testing Strategy

The project uses a layered testing strategy so each test checks the right level of behavior.

## Current State

The active automated suite lives in:

```text
Application/Tests/
```

The CI pipeline runs the full xUnit suite on every push and pull request.

Current verified baseline:

```text
152 passing tests
```

## Test Layers

| Layer | Purpose | Current Project Role |
|---|---|---|
| Unit tests | Verify isolated domain entities, application use cases, validators, and small services | Primary current layer |
| Integration tests | Verify database repositories, authentication wiring, controllers, middleware, and infrastructure boundaries | Partially covered with lightweight infrastructure tests; deeper database-backed tests are future work |
| End-to-end tests | Verify complete user journeys through the real API shape | Planned after database-backed persistence and stable hosting/test environment |

## Unit Tests

Unit tests should be fast, isolated, and deterministic.

Use unit tests for:

- domain entities and business rules
- application use cases
- authorization decisions
- validation behavior
- background job use cases
- notification/audit trigger behavior

Unit tests should mock:

- repositories
- file storage
- notification dispatchers
- metrics/logging where needed

Unit tests should not:

- start the real API host
- require PostgreSQL
- read or write production-like storage paths
- depend on test ordering

## Integration Tests

Integration tests should prove that multiple real pieces work together.

Use integration tests for:

- EF Core repository behavior
- database persistence and query filters
- authentication and authorization wiring
- controller routing and model binding
- middleware behavior
- health checks

Integration tests may use:

- a real test database
- isolated test containers
- `WebApplicationFactory`
- disposable file storage folders

Integration tests should not use production databases, shared developer databases, or real user data.

## End-To-End Tests

End-to-end tests should cover important user journeys from the outside.

Recommended first scenarios:

- register
- login
- upload document
- share document
- download shared document
- revoke access
- confirm revoked user can no longer download

End-to-end tests should be fewer than unit tests because they are slower and more expensive to maintain.

## CI Expectations

Every pull request should pass:

- restore
- Release build
- full xUnit test suite
- publish

When integration and end-to-end suites become heavier, split them by category in CI:

- unit tests on every push and pull request
- integration tests on pull requests and main branch
- end-to-end tests before deployment or on a scheduled run

## Next Testing Milestone

The next valuable testing improvement is database-backed integration testing once repositories move fully to persistent storage. That should prove tenant isolation, ownership-aware search, document sharing, audit queries, and notification queries against the real persistence layer.
