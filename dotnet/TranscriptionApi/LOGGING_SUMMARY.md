# Logging Implementation Summary

This document summarizes the comprehensive structured logging implementation added to the ASP.NET Core Transcription API.

## Overview

The application now includes extensive structured logging throughout all layers, providing detailed visibility into:
- HTTP request/response lifecycle
- Job lifecycle events
- Audio processing steps
- Model loading and transcription operations
- Error handling with stack traces

## Logging Components

### 1. Request/Response Logging Middleware

**File:** `Middleware/RequestResponseLoggingMiddleware.cs`

**Features:**
- Logs all incoming HTTP requests with method, path, query string, content type, and content length
- Logs file upload information for multipart form data
- Logs outgoing responses with status code, duration, and content information
- Assigns unique request IDs for correlation
- Measures and logs request duration
- Identifies slow requests (>5 seconds) with warnings
- Uses appropriate log levels based on response status codes:
  - Information: 2xx, 3xx
  - Warning: 4xx
  - Error: 5xx
- Excludes sensitive headers (Authorization, Cookie, etc.) from logs
- Skips WebSocket requests (handled separately)

**Example Log Output:**
```
Request started - abc123 POST /api/v1/transcribe/batch - ContentType: multipart/form-data, ContentLength: 1048576
Request abc123 includes file: audio.mp3, Size: 1048576 bytes, ContentType: audio/mpeg
Request completed - abc123 POST /api/v1/transcribe/batch - Status: 200, Duration: 1234ms, ContentType: application/json
```

### 2. Application Startup Logging

**File:** `Program.cs`

**Features:**
- Logs application startup sequence
- Logs configuration loading and validation
- Logs all configuration values (model size, max workers, queue size, file size limit)
- Logs service registration
- Logs middleware registration
- Logs controller mapping
- Logs model preloading status
- Logs application readiness

**Example Log Output:**
```
Starting ASP.NET Core Transcription API...
Configuration loaded - ModelSize: medium, MaxWorkers: 4, MaxQueueSize: 100, MaxFileSizeMB: 500
Configuration validation passed
Registering services...
Services registered successfully
Application built successfully
Running in Development environment
Request/response logging middleware enabled
WebSocket support enabled
Global exception handler enabled
Controllers mapped
Starting background model preload...
Preloading Whisper model in background...
Application startup complete - ready to accept requests
```

### 3. Job Lifecycle Logging

**File:** `Services/JobManager.cs`

**Features:**
- Logs job creation with job ID, status, and timestamp
- Logs all job status transitions with old and new status
- Logs job completion with duration calculation
- Logs transcription results (length)
- Logs error messages for failed jobs
- Logs processing start with audio file path
- Logs audio conversion start and completion with duration
- Logs transcription start and completion with duration and result length
- Logs total processing duration
- Logs cleanup operations (file deletion)
- Logs cleanup failures as warnings

**Job Lifecycle Events:**
- `CREATED` - Job created with pending status
- `STATUS_CHANGE` - Job status updated
- `PROCESSING_STARTED` - Background processing started
- `AUDIO_CONVERSION_STARTED` - Audio conversion initiated
- `AUDIO_CONVERSION_COMPLETED` - Audio conversion finished
- `TRANSCRIPTION_STARTED` - Transcription initiated
- `TRANSCRIPTION_COMPLETED` - Transcription finished
- `COMPLETED` - Job completed successfully
- `FAILED` - Job failed with error
- `PROCESSING_COMPLETED` - Total processing finished
- `PROCESSING_FAILED` - Processing failed
- `TRANSCRIPTION_SET` - Transcription result stored
- `ERROR_SET` - Error message stored
- `CLEANUP_STARTED` - File cleanup initiated
- `CLEANUP_FILE_DELETED` - File deleted
- `CLEANUP_COMPLETED` - Cleanup finished
- `CLEANUP_FAILED` - Cleanup failed

**Example Log Output:**
```
Job lifecycle: CREATED - JobId: abc-123, Status: Pending, CreatedAt: 2024-01-15T10:30:00Z
Job lifecycle: PROCESSING_STARTED - JobId: abc-123, AudioFile: /tmp/upload_xyz.mp3
Job lifecycle: STATUS_CHANGE - JobId: abc-123, OldStatus: Pending, NewStatus: Processing
Job lifecycle: AUDIO_CONVERSION_STARTED - JobId: abc-123, InputFile: /tmp/upload_xyz.mp3
Job lifecycle: AUDIO_CONVERSION_COMPLETED - JobId: abc-123, OutputFile: /tmp/converted_xyz.wav, Duration: 1234ms
Job lifecycle: TRANSCRIPTION_STARTED - JobId: abc-123, ConvertedFile: /tmp/converted_xyz.wav
Job lifecycle: TRANSCRIPTION_COMPLETED - JobId: abc-123, Duration: 5678ms, ResultLength: 150 characters
Job lifecycle: COMPLETED - JobId: abc-123, OldStatus: Processing, Duration: 6912ms, CompletedAt: 2024-01-15T10:30:07Z
Job lifecycle: TRANSCRIPTION_SET - JobId: abc-123, TranscriptionLength: 150 characters
Job lifecycle: PROCESSING_COMPLETED - JobId: abc-123, TotalDuration: 6912ms
Job lifecycle: CLEANUP_STARTED - JobId: abc-123
Job lifecycle: CLEANUP_FILE_DELETED - JobId: abc-123, File: /tmp/upload_xyz.mp3
Job lifecycle: CLEANUP_FILE_DELETED - JobId: abc-123, File: /tmp/converted_xyz.wav
Job lifecycle: CLEANUP_COMPLETED - JobId: abc-123
```

### 4. Audio Processing Logging

**File:** `Services/AudioProcessor.cs`

**Features:**
- Logs file validation (format, size, MIME type)
- Logs upload save operations with duration
- Logs audio conversion with detailed steps:
  - Input file information (path, size)
  - Output file configuration (format, sample rate, channels)
  - FFmpeg execution start and completion
  - Conversion duration
  - Output file information (path, size)
  - Size change percentage
- Logs all errors with context

**Audio Processing Events:**
- `UPLOAD_SAVE_STARTED` - File upload save initiated
- `UPLOAD_SAVE_COMPLETED` - File upload save finished
- `UPLOAD_SAVE_FAILED` - File upload save failed
- `CONVERSION_STARTED` - Audio conversion initiated
- `CONVERSION_CONFIG` - Conversion configuration
- `FFMPEG_EXECUTION_STARTED` - FFmpeg execution started
- `FFMPEG_EXECUTION_COMPLETED` - FFmpeg execution finished
- `CONVERSION_COMPLETED` - Audio conversion finished
- `CONVERSION_FAILED` - Audio conversion failed

**Example Log Output:**
```
File validation successful: audio.mp3, Size: 1048576 bytes, Type: audio/mpeg
Audio processing: UPLOAD_SAVE_STARTED - FileName: audio.mp3, Size: 1048576 bytes, TargetPath: /tmp/upload_xyz.mp3
Audio processing: UPLOAD_SAVE_COMPLETED - FilePath: /tmp/upload_xyz.mp3, Size: 1048576 bytes, Duration: 123ms
Audio processing: CONVERSION_STARTED - InputFile: /tmp/upload_xyz.mp3, Size: 1048576 bytes
Audio processing: CONVERSION_CONFIG - OutputFile: /tmp/converted_xyz.wav, TargetFormat: 16kHz mono WAV with normalization
Audio processing: FFMPEG_EXECUTION_STARTED - InputFile: /tmp/upload_xyz.mp3
Audio processing: FFMPEG_EXECUTION_COMPLETED - Duration: 1234ms
Audio processing: CONVERSION_COMPLETED - OutputFile: /tmp/converted_xyz.wav, Size: 512000 bytes, Duration: 1234ms, SizeChange: -51.17%
```

### 5. Model Loading Logging

**File:** `Services/WhisperModelService.cs`

**Features:**
- Logs model loading start with model size
- Logs model type parsing
- Logs model download (if not cached) with duration and file size
- Logs cached model usage with file size
- Logs processor creation with duration
- Logs model loading completion with total duration
- Logs model loading failures with error details
- Logs when model is already loaded

**Model Loading Events:**
- `ALREADY_LOADED` - Model already loaded, skipping
- `STARTED` - Model loading initiated
- `PARSED_MODEL_TYPE` - Model type parsed
- `DOWNLOAD_STARTED` - Model download initiated
- `DOWNLOAD_COMPLETED` - Model download finished
- `USING_CACHED` - Using cached model file
- `PROCESSOR_CREATION_STARTED` - Processor creation initiated
- `PROCESSOR_CREATION_COMPLETED` - Processor creation finished
- `COMPLETED` - Model loading finished
- `FAILED` - Model loading failed

**Example Log Output:**
```
Model loading: STARTED - ModelSize: medium. This may take several minutes on first run...
Model loading: PARSED_MODEL_TYPE - GgmlType: Medium
Model loading: DOWNLOAD_STARTED - ModelFile: ggml-medium.bin not found in cache, downloading...
Model loading: DOWNLOAD_COMPLETED - ModelFile: ggml-medium.bin, Size: 1536000000 bytes, Duration: 45678ms
Model loading: PROCESSOR_CREATION_STARTED - Creating WhisperProcessor
Model loading: PROCESSOR_CREATION_COMPLETED - Duration: 2345ms
Model loading: COMPLETED - ModelSize: medium, TotalDuration: 48023ms, IsLoaded: True
```

### 6. Transcription Logging

**File:** `Services/WhisperModelService.cs`

**Features:**
- Logs transcription start with audio file path and size
- Logs each transcription segment processed
- Logs transcription completion with duration, segment count, and result length
- Logs transcription cancellation
- Logs transcription failures with error details
- Logs when model needs to be loaded before transcription

**Transcription Events:**
- `MODEL_NOT_LOADED` - Model not loaded, loading now
- `STARTED` - Transcription initiated
- `SEGMENT_PROCESSED` - Transcription segment processed
- `COMPLETED` - Transcription finished
- `CANCELLED` - Transcription cancelled
- `FAILED` - Transcription failed

**Example Log Output:**
```
Model transcription: STARTED - AudioFile: /tmp/converted_xyz.wav, Size: 512000 bytes
Model transcription: SEGMENT_PROCESSED - AudioFile: /tmp/converted_xyz.wav, SegmentNumber: 1, SegmentText: Hello world
Model transcription: SEGMENT_PROCESSED - AudioFile: /tmp/converted_xyz.wav, SegmentNumber: 2, SegmentText: This is a test
Model transcription: COMPLETED - AudioFile: /tmp/converted_xyz.wav, Duration: 5678ms, Segments: 2, ResultLength: 150 characters
```

### 7. Controller Logging

**Files:** 
- `Controllers/HealthController.cs`
- `Controllers/TranscriptionController.cs`
- `Controllers/StreamingTranscriptionController.cs`

**Features:**
- Logs all endpoint requests
- Logs endpoint responses with results
- Logs validation failures
- Logs capacity checks
- Logs WebSocket connections and disconnections
- Logs streaming buffer operations
- Logs partial and final transcription results

**Example Log Output:**
```
Health check requested
Health check completed - Status: healthy, ModelLoaded: True, ModelSize: medium
Capacity check requested
Capacity check completed - Active: 2, Queued: 5, Available: 93, AtCapacity: False
Received batch transcription request for file: audio.mp3
Audio file validation passed for: audio.mp3
Created job abc-123 for file: audio.mp3
Started background processing for job abc-123
Job status query for: abc-123
Job abc-123 status: Completed
WebSocket connection established
Received 4096 bytes, buffer size: 102400
Buffer reached minimum chunk size, transcribing 102400 bytes
Sent partial transcription: Hello world
WebSocket close message received
Sent final transcription: This is a test
WebSocket connection closed
```

### 8. Error Logging

**File:** `Middleware/GlobalExceptionHandler.cs`

**Features:**
- Logs all unhandled exceptions with full stack traces
- Uses appropriate log levels:
  - Warning: 4xx client errors
  - Error: 5xx server errors
- Includes request context (method, path, status code)
- Includes exception type
- Maps exceptions to appropriate error codes and messages

**Example Log Output:**
```
Client error occurred. Type: InvalidAudioFormatException, Method: POST, Path: /api/v1/transcribe/batch, StatusCode: 415
Server error occurred. Type: InvalidOperationException, Method: POST, Path: /api/v1/transcribe/batch, StatusCode: 500
```

## Structured Logging Benefits

### 1. Correlation
- Request IDs link all operations for a single request
- Job IDs link all operations for a single transcription job
- File paths link upload, conversion, and transcription operations

### 2. Performance Monitoring
- Duration measurements for all major operations
- Slow request detection
- Size change tracking for audio conversion
- Segment processing visibility

### 3. Debugging
- Detailed step-by-step operation logging
- Error context with file paths and parameters
- Stack traces for all exceptions
- Configuration visibility

### 4. Operational Visibility
- Job lifecycle tracking
- Capacity monitoring
- Model loading status
- File cleanup verification

### 5. Compliance
- Request/response audit trail
- File upload tracking
- Processing duration records
- Error documentation

## Log Levels

The application uses standard .NET log levels:

- **Debug**: Detailed diagnostic information (segment processing, file operations)
- **Information**: General informational messages (requests, job lifecycle, completions)
- **Warning**: Potentially harmful situations (slow requests, cleanup failures, capacity issues)
- **Error**: Error events (exceptions, failures)

## Configuration

Logging can be configured in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "TranscriptionApi": "Information"
    }
  }
}
```

For production, consider:
- Setting `Default` to `Warning` or `Error` to reduce log volume
- Using structured logging sinks (Serilog, NLog) for better log management
- Sending logs to centralized logging systems (ELK, Splunk, Application Insights)

## Best Practices Implemented

1. **Structured Logging**: All logs use structured parameters for easy querying
2. **Consistent Naming**: Event names follow a consistent pattern (COMPONENT: EVENT)
3. **Context Inclusion**: All logs include relevant context (IDs, file names, sizes, durations)
4. **Appropriate Levels**: Log levels match the severity of events
5. **Performance Awareness**: Debug logs for high-frequency events, Information for important events
6. **Error Details**: All errors include exception details and stack traces
7. **Security**: Sensitive headers excluded from request logs
8. **Correlation**: Request IDs and Job IDs enable end-to-end tracing

## Monitoring Recommendations

Based on the logging implementation, monitor:

1. **Request Duration**: Alert on requests >10 seconds
2. **Error Rate**: Alert on error rate >5%
3. **Job Failure Rate**: Alert on job failure rate >10%
4. **Model Loading**: Alert on model loading failures
5. **Capacity**: Alert when at capacity for >5 minutes
6. **Cleanup Failures**: Alert on repeated cleanup failures
7. **Slow Transcriptions**: Alert on transcriptions >30 seconds

## Example Full Request Flow

```
[INFO] Request started - req-123 POST /api/v1/transcribe/batch - ContentType: multipart/form-data, ContentLength: 1048576
[INFO] Request req-123 includes file: audio.mp3, Size: 1048576 bytes, ContentType: audio/mpeg
[INFO] Received batch transcription request for file: audio.mp3
[DEBUG] Audio file validation passed for: audio.mp3
[INFO] Audio processing: UPLOAD_SAVE_STARTED - FileName: audio.mp3, Size: 1048576 bytes
[INFO] Audio processing: UPLOAD_SAVE_COMPLETED - FilePath: /tmp/upload_xyz.mp3, Duration: 123ms
[INFO] Job lifecycle: CREATED - JobId: job-456, Status: Pending
[INFO] Created job job-456 for file: audio.mp3
[DEBUG] Started background processing for job job-456
[INFO] Request completed - req-123 POST /api/v1/transcribe/batch - Status: 200, Duration: 234ms
[INFO] Job lifecycle: PROCESSING_STARTED - JobId: job-456, AudioFile: /tmp/upload_xyz.mp3
[INFO] Job lifecycle: STATUS_CHANGE - JobId: job-456, OldStatus: Pending, NewStatus: Processing
[INFO] Audio processing: CONVERSION_STARTED - InputFile: /tmp/upload_xyz.mp3, Size: 1048576 bytes
[DEBUG] Audio processing: CONVERSION_CONFIG - OutputFile: /tmp/converted_xyz.wav
[DEBUG] Audio processing: FFMPEG_EXECUTION_STARTED - InputFile: /tmp/upload_xyz.mp3
[DEBUG] Audio processing: FFMPEG_EXECUTION_COMPLETED - Duration: 1234ms
[INFO] Audio processing: CONVERSION_COMPLETED - OutputFile: /tmp/converted_xyz.wav, Duration: 1234ms
[INFO] Job lifecycle: AUDIO_CONVERSION_COMPLETED - JobId: job-456, Duration: 1234ms
[DEBUG] Job lifecycle: TRANSCRIPTION_STARTED - JobId: job-456
[INFO] Model transcription: STARTED - AudioFile: /tmp/converted_xyz.wav, Size: 512000 bytes
[DEBUG] Model transcription: SEGMENT_PROCESSED - SegmentNumber: 1, SegmentText: Hello world
[INFO] Model transcription: COMPLETED - Duration: 5678ms, Segments: 1, ResultLength: 11 characters
[INFO] Job lifecycle: TRANSCRIPTION_COMPLETED - JobId: job-456, Duration: 5678ms, ResultLength: 11 characters
[INFO] Job lifecycle: COMPLETED - JobId: job-456, Duration: 6912ms
[INFO] Job lifecycle: TRANSCRIPTION_SET - JobId: job-456, TranscriptionLength: 11 characters
[INFO] Job lifecycle: PROCESSING_COMPLETED - JobId: job-456, TotalDuration: 6912ms
[DEBUG] Job lifecycle: CLEANUP_STARTED - JobId: job-456
[DEBUG] Job lifecycle: CLEANUP_FILE_DELETED - JobId: job-456, File: /tmp/upload_xyz.mp3
[DEBUG] Job lifecycle: CLEANUP_FILE_DELETED - JobId: job-456, File: /tmp/converted_xyz.wav
[DEBUG] Job lifecycle: CLEANUP_COMPLETED - JobId: job-456
```

## Conclusion

The logging implementation provides comprehensive visibility into all aspects of the transcription API, enabling effective monitoring, debugging, and operational support. All requirements from task 14.1 have been fully implemented with structured logging and rich context throughout the application.
