# Requirements: ASP.NET Core Transcription API

## 1. Overview

Port the Python FastAPI Persian Transcription API to ASP.NET Core, maintaining all functionality while using .NET patterns and practices.

## 2. User Stories

### 2.1 As an API consumer, I want to check service health
**Acceptance Criteria:**
- GET /api/v1/health endpoint returns service status
- Response includes model loaded status and model size
- Returns 200 OK when service is healthy

### 2.2 As an API consumer, I want to check service capacity
**Acceptance Criteria:**
- GET /api/v1/capacity endpoint returns current load information
- Response includes active jobs, queued jobs, max workers, and available capacity
- Returns 503 when service is unavailable

### 2.3 As an API consumer, I want to upload audio for batch transcription
**Acceptance Criteria:**
- POST /api/v1/transcribe/batch accepts multipart/form-data with audio file
- Supports WAV, MP3, OGG, M4A formats
- Returns job_id and status "pending"
- Validates file size (max 500 MB configurable)
- Returns 415 for unsupported formats
- Returns 413 for files exceeding size limit
- Returns 503 when at capacity

### 2.4 As an API consumer, I want to check transcription status
**Acceptance Criteria:**
- GET /api/v1/transcribe/batch/{job_id} returns job status
- Status values: pending, processing, completed, failed
- Returns transcription text when completed
- Returns error message when failed
- Returns 404 for unknown job_id

### 2.5 As an API consumer, I want real-time streaming transcription
**Acceptance Criteria:**
- WebSocket endpoint at /api/v1/transcribe/stream
- Accepts binary audio chunks
- Returns partial transcription results as JSON
- Returns final transcription on connection close
- Handles errors gracefully with error messages

## 3. Functional Requirements

### 3.1 Audio Processing
- Validate audio format using file headers and extensions
- Convert audio to Whisper format (16kHz mono WAV) using FFmpeg
- Support WAV, MP3, OGG, M4A input formats
- Apply audio normalization during conversion

### 3.2 Transcription Service
- Load Whisper model on startup or first request
- Process transcription jobs asynchronously
- Manage worker pool with configurable concurrency
- Queue jobs when at capacity
- Track job lifecycle (pending → processing → completed/failed)
- Clean up old completed jobs

### 3.3 Job Management
- Thread-safe job creation and updates
- Store jobs in memory with unique IDs
- Support job status queries
- Automatic cleanup of old jobs (configurable age)

### 3.4 Streaming Transcription
- Buffer audio chunks until minimum size reached
- Transcribe buffered audio and return partial results
- Finalize remaining buffer on connection close
- Enforce maximum buffer size limit

### 3.5 Configuration
- Support configuration via appsettings.json
- Allow environment variable overrides
- Configurable: model size, max workers, queue size, file size limit, ports, logging

### 3.6 Error Handling
- Consistent error response format
- Appropriate HTTP status codes
- Detailed error messages
- Logging of all errors

### 3.7 Logging
- Structured logging with context
- Log all requests and responses
- Log job lifecycle events
- Configurable log levels

## 4. Non-Functional Requirements

### 4.1 Performance
- Support concurrent transcription jobs
- Efficient memory management
- Proper resource cleanup (temp files, model memory)

### 4.2 Reliability
- Thread-safe operations
- Graceful error handling
- Proper shutdown procedures

### 4.3 Maintainability
- Clean separation of concerns
- Dependency injection
- Standard ASP.NET Core patterns

### 4.4 Compatibility
- ASP.NET Core 8.0 or later
- Cross-platform (Windows, Linux, macOS)
- FFmpeg dependency for audio processing

## 5. Technical Constraints

### 5.1 Technology Stack
- ASP.NET Core Web API
- FFMpegCore for audio processing
- Whisper.net or similar for model integration
- Built-in DI container
- ILogger for logging

### 5.2 Project Structure
- Location: `dotnet/` folder
- Controllers for API endpoints
- Services for business logic
- Models for data structures
- Configuration via appsettings.json

### 5.3 No Testing Required
- User will test manually
- No unit tests needed
- No integration tests needed

## 6. API Specification

### 6.1 Endpoints

#### Health Check
```
GET /api/v1/health
Response: { status: string, model_loaded: bool, model_size: string }
```

#### Capacity Check
```
GET /api/v1/capacity
Response: { active_jobs: int, queued_jobs: int, max_workers: int, max_queue_size: int, available_capacity: int, at_capacity: bool }
```

#### Batch Transcription Upload
```
POST /api/v1/transcribe/batch
Content-Type: multipart/form-data
Body: audio_file (file)
Response: { job_id: string, status: string }
```

#### Batch Transcription Status
```
GET /api/v1/transcribe/batch/{job_id}
Response: { job_id: string, status: string, transcription: string?, error: string? }
```

#### Streaming Transcription
```
WebSocket /api/v1/transcribe/stream
Send: Binary audio chunks
Receive: { type: "partial"|"final"|"error", text: string, timestamp: float? }
```

### 6.2 Error Response Format
```json
{
  "error": {
    "code": "ERROR_CODE",
    "message": "Human-readable message",
    "details": "Optional additional context"
  }
}
```

## 7. Configuration Options

| Setting | Default | Description |
|---------|---------|-------------|
| WhisperModelSize | medium | Model size (tiny/base/small/medium/large) |
| MaxConcurrentWorkers | 4 | Maximum concurrent transcription workers |
| MaxQueueSize | 100 | Maximum queued jobs |
| MaxFileSizeMB | 500 | Maximum audio file size |
| ApiPort | 5000 | API port |
| ApiHost | 0.0.0.0 | API host |
| LogLevel | Information | Logging level |
| JobCleanupMaxAgeHours | 24 | Hours before job cleanup |
| StreamMinChunkSize | 102400 | Minimum chunk size for streaming (bytes) |
| StreamMaxBufferSize | 10485760 | Maximum buffer size for streaming (bytes) |

## 8. Dependencies

- ASP.NET Core 8.0+
- FFMpegCore (NuGet)
- Whisper.net or equivalent (NuGet)
- FFmpeg binary (system dependency)
