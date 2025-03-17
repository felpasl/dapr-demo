# Dapr Sample Application

This project demonstrates how to use Dapr (Distributed Application Runtime) for building microservice applications.

## Prerequisites

- VSCode with Remote Containers extension
- Docker Desktop installed and running
- Dapr CLI

## Getting Started

### Setup Development Environment

1. Clone this repository
2. Start a VSCode dev container for this project
3. In terminal, initialize Dapr:
   ```
   dapr init
   ```
4. Verify installation:
   ```
   dapr --version
   ```

### Running the Application

1. Start the application with Dapr:
   ```
   dapr run -f .
   ```

2. Access the Swagger UI:
   - Open http://localhost:5279/swagger/index.html in your browser
   - Call a Post /Order/start endpoint to start the sample processing

## The Application

``` mermaid
```

## Troubleshooting

If you encounter any issues:
- Ensure Docker is running
- Check Dapr services with `dapr list`
- Restart with `dapr stop` followed by `dapr run -f .`

## Learn More

- [Dapr Documentation](https://docs.dapr.io/)
- [Dapr GitHub Repository](https://github.com/dapr/dapr)