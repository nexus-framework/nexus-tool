# Usage

## Initializing a Solution
```powershell
nexus init -n "<solution_name>"
```

e.g.
```powershell
nexus init -n "HelloWorld"
```

## Adding a service
```powershell
nexus add-service -n "<service_name>"
```

e.g.
```powershell
nexus add-service "people"
```

## Running locally
```powershell
nexus run local
```

## Running in Docker
```powershell
nexus run docker
```