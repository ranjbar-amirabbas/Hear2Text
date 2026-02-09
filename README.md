# Transcription API - Multi-Platform Implementation

A production-ready REST API service for audio transcription using OpenAI's Whisper model. This project provides two complete implementations: **Python (FastAPI)** and **.NET (ASP.NET Core)**, both containerized and ready for deployment.

## Overview

This service provides offline audio transcription capabilities with support for multiple audio formats, concurrent processing, and both batch and streaming transcription modes. Choose the implementation that best fits your technology stack.

### Key Features

- **Dual Implementation**: Python (FastAPI) and .NET (ASP.NET Core)
- **Batch Processing**: Upload audio files for asynchronous transcription
- **Real-time Streaming**: WebSocket-based streaming transcription
- **Offline Operation**: Runs completely locally after initial setup
- **Multiple Formats**: WAV, MP3, OGG, M4A support
- **Docker Ready**: Full containerization with docker-compose
- **Concurrent Processing**: Handle multiple requests simultaneously
- **Health Monitoring**: Built-in health checks and capacity management
- **Production Ready**: Comprehensive logging, error handling, and validation

## Quick Start

### Prerequisites

- Docker 20.10+
- Docker Compose 2.0+
- 4-8 GB RAM (depending on model size)
- 4-8 CPU cores recommended

### Start Both Services

```bash
# Start both Python and .NET services
docker-compose up -d

# View logs
docker-compose logs -f

# Check health
curl http://localhost:8000/api/v1/health  # Python
curl http://localhost:5226/api/health     # .NET
```

### Start Individual Services

**Python Service Only:**
```bash
cd python
docker-compose up -d
```

**.NET Service Only:**
```bash
cd dotnet
docker-compose up -d
```

## Architecture

```
transcription-api/
├── python/                    # Python FastAPI implementation
│   ├── app/                   # Application code
│   ├── tests/                 # Test suite
│   ├── Dockerfile             # Python container
│   ├── docker-compose.yml     # Python service config
│   └── README.md              # Python-specific docs
│
├── dotnet/                    # .NET ASP.NET Core implementation
│   ├── TranscriptionApi/      # API project
│   │   ├── Controllers/       # API endpoints
│   │   ├── Services/          # Business logic
│   │   ├── Middleware/        # Request pipeline
│   │   ├── Models/            # Data models
│   │   └── Dockerfile         # .NET container
│   ├── docker-compose.yml     # .NET service config
│   └── README_DOCKER.md       # .NET-specific docs
│
└── docker-compose.yml         # Multi-service orchestration
```

## Implementation Comparison

| Feature | Python (FastAPI) | .NET (ASP.NET Core) |
|---------|------------------|---------------------|
| **Language** | Python 3.11+ | C# / .NET 10.0 |
| **Framework** | FastAPI | ASP.NET Core |
| **Whisper Library** | openai-whisper | Whisper.net |
| **Default Port** | 8000 | 5226 |
| **Async Support** | ✅ Native | ✅ Native |
| **Streaming** | WebSocket | WebSocket |
| **Health Checks** | ✅ | ✅ |
| **Swagger/OpenAPI** | ✅ | ✅ |
| **Docker Support** | ✅ | ✅ |
| **Performance** | Fast | Very Fast |
| **Memory Usage** | Moderate | Lower |
| **Startup Time** | Fast | Very Fast |

### When to Choose Python

- Rapid development and prototyping
- Python ecosystem integration
- Data science workflows
- Familiar with Python/FastAPI
- Need extensive Python libraries

### When to Choose .NET

- Enterprise environments
- High-performance requirements
- Windows infrastructure
- Familiar with C#/.NET
- Need strong typing and tooling

## API Endpoints

Both implementations provide similar REST APIs:

### Python (FastAPI) - Port 8000

```
GET  /api/v1/health                    # Health check
GET  /api/v1/capacity                  # Capacity info
POST /api/v1/transcribe/batch          # Upload for transcription
GET  /api/v1/transcribe/batch/{job_id} # Get transcription status
WS   /api/v1/transcribe/stream         # Streaming transcription
```

### .NET (ASP.NET Core) - Port 5226

```
GET  /api/health                       # Health check
GET  /api/capacity                     # Capacity info
POST /api/transcription/batch          # Upload for transcription
GET  /api/transcription/batch/{job_id} # Get transcription status
WS   /api/transcription/stream         # Streaming transcription
```

## Usage Examples

### Batch Transcription

**Python:**
```bash
# Upload file
curl -X POST http://localhost:8000/api/v1/transcribe/batch \
  -F "audio_file=@audio.mp3"

# Check status
curl http://localhost:8000/api/v1/transcribe/batch/{job_id}
```

**.NET:**
```bash
# Upload file
curl -X POST http://localhost:5226/api/transcription/batch \
  -F "audio_file=@audio.mp3"

# Check status
curl http://localhost:5226/api/transcription/batch/{job_id}
```

### Health Check

```bash
# Python
curl http://localhost:8000/api/v1/health

# .NET
curl http://localhost:5226/api/health
```

### Python Client Example

```python
import requests

# Python API
response = requests.post(
    "http://localhost:8000/api/v1/transcribe/batch",
    files={"audio_file": open("audio.mp3", "rb")}
)
job_id = response.json()["job_id"]

# .NET API
response = requests.post(
    "http://localhost:5226/api/transcription/batch",
    files={"audio_file": open("audio.mp3", "rb")}
)
job_id = response.json()["job_id"]
```

## Configuration

Both services support similar configuration options via environment variables:

### Common Configuration

| Variable | Default | Description |
|----------|---------|-------------|
| `WHISPER_MODEL_SIZE` | `medium` | Model size: tiny, base, small, medium, large |
| `MAX_CONCURRENT_WORKERS` | `4` | Maximum concurrent transcription workers |
| `MAX_QUEUE_SIZE` | `100` | Maximum queued jobs |
| `MAX_FILE_SIZE_MB` | `500` | Maximum audio file size |
| `LOG_LEVEL` | `INFO` | Logging level |
| `JOB_CLEANUP_MAX_AGE_HOURS` | `24` | Job retention time |

### Python-Specific

```bash
API_PORT=8000
API_HOST=0.0.0.0
```

### .NET-Specific

```bash
ASPNETCORE_ENVIRONMENT=Production
Transcription__WhisperModelSize=medium
Transcription__MaxConcurrentWorkers=4
```

## Model Sizes

| Model | RAM | Speed | Accuracy | Use Case |
|-------|-----|-------|----------|----------|
| tiny | 1 GB | Very Fast | Low | Testing |
| base | 1 GB | Fast | Low | Development |
| small | 2 GB | Moderate | Good | Balanced |
| medium | 5 GB | Moderate | Very Good | **Production** ⭐ |
| large | 10 GB | Slow | Excellent | High accuracy |

## Docker Compose Services

The root `docker-compose.yml` orchestrates both services:

```yaml
services:
  python-transcription-api:
    ports: ["8000:8000"]
    # Python FastAPI service
  
  dotnet-transcription-api:
    ports: ["5226:5226"]
    # .NET ASP.NET Core service
```

### Service Management

```bash
# Start all services
docker-compose up -d

# Start specific service
docker-compose up -d python-transcription-api
docker-compose up -d dotnet-transcription-api

# View logs
docker-compose logs -f python-transcription-api
docker-compose logs -f dotnet-transcription-api

# Stop all services
docker-compose down

# Rebuild services
docker-compose build
docker-compose up -d
```

## Development

### Python Development

```bash
cd python

# Create virtual environment
python -m venv venv
source venv/bin/activate  # Windows: venv\Scripts\activate

# Install dependencies
pip install -r requirements.txt

# Run locally
uvicorn app.main:app --reload

# Run tests
pytest
```

### .NET Development

```bash
cd dotnet/TranscriptionApi

# Restore dependencies
dotnet restore

# Run locally
dotnet run

# Run tests (if available)
dotnet test
```

## API Documentation

Both implementations provide interactive API documentation:

### Python (FastAPI)
- Swagger UI: http://localhost:8000/docs
- ReDoc: http://localhost:8000/redoc

### .NET (ASP.NET Core)
- Swagger UI: http://localhost:5226/swagger

## Performance Considerations

### Resource Requirements

**Minimum (small model):**
- 2 CPU cores
- 4 GB RAM
- 5 GB disk

**Recommended (medium model):**
- 4-8 CPU cores
- 8 GB RAM
- 10 GB disk

**High-Performance (large model):**
- 8+ CPU cores
- 16 GB RAM
- 15 GB disk

### Concurrent Processing

- Each worker processes one file at a time
- Workers share the same model instance (memory efficient)
- Additional requests are queued
- Requests beyond queue limit receive 503 errors

## Troubleshooting

### Common Issues

**Service won't start:**
- Check logs: `docker-compose logs`
- Verify ports are available
- Ensure sufficient disk space (3-5 GB for model)

**Out of memory:**
- Use smaller model: `WHISPER_MODEL_SIZE=small`
- Reduce workers: `MAX_CONCURRENT_WORKERS=2`
- Increase Docker memory limit

**Slow transcription:**
- Use faster model: `WHISPER_MODEL_SIZE=small`
- Reduce concurrent workers
- Check CPU usage

**Port conflicts:**
- Change ports in docker-compose.yml
- Stop conflicting services

### Debug Mode

Enable detailed logging:

```bash
# Python
LOG_LEVEL=DEBUG

# .NET
Logging__LogLevel__Default=Debug
```

## Project Documentation

### Python Implementation
- [Python README](python/README.md) - Detailed Python documentation
- [API Documentation](python/API_DOCUMENTATION.md) - API reference
- [Docker Setup](python/DOCKER.md) - Docker configuration

### .NET Implementation
- [.NET Docker Guide](dotnet/DOCKER_COMPLETE_GUIDE.md) - Complete Docker guide
- [Architecture](dotnet/ARCHITECTURE.md) - System architecture
- [Getting Started](dotnet/GETTING_STARTED_DOCKER.md) - Quick start guide
- [Local Development](dotnet/LOCAL_DEVELOPMENT_SETUP.md) - Development setup

## Production Deployment

### Docker Compose (Recommended)

```bash
# Production configuration
docker-compose -f docker-compose.yml up -d

# With custom environment
docker-compose --env-file .env.production up -d
```

### Kubernetes

Both services include Dockerfile configurations suitable for Kubernetes deployment. See individual implementation docs for Kubernetes manifests.

### Load Balancing

For high availability, deploy multiple instances behind a load balancer:

```yaml
# Example nginx configuration
upstream python_backend {
    server python-api-1:8000;
    server python-api-2:8000;
}

upstream dotnet_backend {
    server dotnet-api-1:5226;
    server dotnet-api-2:5226;
}
```

## Monitoring

### Health Checks

Both services provide health endpoints for monitoring:

```bash
# Python
curl http://localhost:8000/api/v1/health

# .NET
curl http://localhost:5226/api/health
```

### Capacity Monitoring

```bash
# Python
curl http://localhost:8000/api/v1/capacity

# .NET
curl http://localhost:5226/api/capacity
```

### Logs

Logs are available via Docker:

```bash
# View logs
docker-compose logs -f

# Export logs
docker-compose logs > logs.txt
```

## Security Considerations

- Services run on localhost by default
- No authentication included (add as needed)
- File size limits enforced
- Input validation on all endpoints
- Temporary files cleaned up automatically

## License

(License information to be added)

---

## Quick Reference

### Essential Commands

```bash
# Start all services
docker-compose up -d

# Check health
curl http://localhost:8000/api/v1/health  # Python
curl http://localhost:5226/api/health     # .NET

# Transcribe (Python)
curl -X POST http://localhost:8000/api/v1/transcribe/batch \
  -F "audio_file=@audio.mp3"

# Transcribe (.NET)
curl -X POST http://localhost:5226/api/transcription/batch \
  -F "audio_file=@audio.mp3"

# View logs
docker-compose logs -f

# Stop services
docker-compose down
```

### Key URLs

**Python:**
- API: http://localhost:8000/api/v1
- Docs: http://localhost:8000/docs

**.NET:**
- API: http://localhost:5226/api
- Swagger: http://localhost:5226/swagger

### Support

For implementation-specific questions, see:
- [Python README](python/README.md)
- [.NET Documentation](dotnet/DOCKER_COMPLETE_GUIDE.md)
