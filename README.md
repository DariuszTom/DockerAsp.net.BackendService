# TestBackendService

Lightweight ASP.NET Core (.NET 9) backend providing mock data generation and utility/testing endpoints.

## Features
- OpenAPI (Swagger UI in Development at /swagger)
- Mock data endpoints using Bogus:
  - GET /mock/users?count=10&seed=123
  - GET /mock/products?count=10&seed=123
  - GET /mock/company?seed=123
- Utility endpoints:
  - /util/ping, /util/health, /util/ready, /util/version
  - /util/env (environment variables)
  - /util/whoami (host + process info)
  - /util/echo (POST JSON echo)
  - /util/delay/{ms}
  - /util/error/{code}
  - /util/headers
  - /util/files?path= (limited file system inspection under data root)
  - /util/allocate/{mb} and /util/clearallocations (memory pressure testing)
- Health check at /healthz
- ProblemDetails + exception handler + optional HTTPS redirection

## Environment Variables
- DISABLE_HTTPS_REDIRECT=true|1  (disable automatic HTTPS redirect)
- DATA_ROOT=path  (root directory for /util/files; defaults to <app base>/data)

## Build & Run (CLI)
```
dotnet build
DOTNET_ENVIRONMENT=Development dotnet run --project TestBackendService/TestBackendService.csproj
```
Open http://localhost:5000/swagger (HTTP) or https://localhost:7000/swagger (HTTPS) in Development.

## Docker
Example Docker build/run:
```
docker build -t test-backend .
docker run -p 8080:8080 -e ASPNETCORE_HTTP_PORTS=8080 test-backend
```

## Project Structure
- Program.cs: host configuration
- Controllers/: API endpoints
- Models/: DTOs
- Services/: Mock data generation abstraction

## Notes
Deterministic data: provide seed query parameter. Count clamped to 1..1000.
File browsing is confined to DATA_ROOT to mitigate traversal attacks.
## Author
Dariusz Tomczak
## License
MIT (add a LICENSE file if needed).
