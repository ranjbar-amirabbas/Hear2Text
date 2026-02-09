# Implementation Plan: ASP.NET Core Transcription API

## Overview

This implementation plan breaks down the ASP.NET Core transcription API into discrete coding tasks. The approach follows a bottom-up strategy: first establishing core models and configuration, then building services, and finally wiring everything together through controllers. Each task builds incrementally on previous work to ensure continuous integration.

## Tasks

- [x] 1. Set up project structure and configuration
  - Create ASP.NET Core Web API project in `dotnet/TranscriptionApi/`
  - Add NuGet packages: FFMpegCore, Whisper.net, Whisper.net.Runtime
  - Create folder structure: Controllers/, Services/, Models/, Exceptions/, Middleware/
  - Create appsettings.json with Transcription configuration section
  - _Requirements: 5.1, 5.2, 3.5, 7_

- [ ] 2. Implement core models and DTOs
  - [x] 2.1 Create TranscriptionJob model and JobStatus enum
    - Define TranscriptionJob class with JobId, Status, Transcription, Error, CreatedAt, CompletedAt
    - Define JobStatus enum: Pending, Processing, Completed, Failed
    - _Requirements: 3.3_
  
  - [x] 2.2 Create AppConfiguration model
    - Define all configuration properties with defaults
    - WhisperModelSize, MaxConcurrentWorkers, MaxQueueSize, MaxFileSizeMB, etc.
    - _Requirements: 3.5, 7_
  
  - [x] 2.3 Create response DTOs
    - Define HealthResponse, CapacityResponse, BatchTranscriptionResponse records
    - Define JobStatusResponse, StreamingMessage, ErrorResponse, ErrorDetail records
    - _Requirements: 6.1, 6.2_
  
  - [x] 2.4 Create custom exception classes
    - InvalidAudioFormatException, FileTooLargeException
    - ServiceAtCapacityException, JobNotFoundException
    - _Requirements: 3.6_

- [ ] 3. Implement configuration validation
  - [x] 3.1 Create AppConfigurationValidator class
    - Validate MaxConcurrentWorkers >= 1
    - Validate MaxQueueSize >= 1
    - Validate MaxFileSizeMB >= 1
    - Validate WhisperModelSize is in allowed list
    - _Requirements: 3.5_
  
  - [ ]* 3.2 Write property test for configuration validation
    - **Property 23: Configuration validation on startup**
    - **Validates: Requirements 3.5**

- [ ] 4. Implement AudioProcessor service
  - [x] 4.1 Create IAudioProcessor interface and AudioProcessor implementation
    - Implement IsValidAudioFile() - check extension, size, MIME type
    - Implement SaveUploadedFileAsync() - save to temp directory
    - Implement ConvertToWhisperFormatAsync() - use FFMpegCore for 16kHz mono WAV
    - Add audio normalization during conversion
    - _Requirements: 3.1_
  
  - [ ]* 4.2 Write property tests for audio validation
    - **Property 4: Supported audio format acceptance**
    - **Property 5: File size validation**
    - **Property 6: Unsupported format rejection**
    - **Validates: Requirements 2.3.2, 2.3.4, 2.3.5, 2.3.6**
  
  - [ ]* 4.3 Write property test for audio conversion
    - **Property 19: Audio conversion produces valid Whisper format**
    - **Validates: Requirements 3.1**

- [ ] 5. Implement WhisperModelService
  - [x] 5.1 Create IWhisperModelService interface and WhisperModelService implementation
    - Implement LoadModelAsync() with thread-safe loading using SemaphoreSlim
    - Implement TranscribeAsync() for audio file transcription
    - Implement IsLoaded and ModelSize properties
    - Implement IDisposable for resource cleanup
    - _Requirements: 3.2, 5.1_
  
  - [ ]* 5.2 Write unit tests for model loading
    - Test model loads successfully
    - Test thread-safe loading (multiple concurrent calls)
    - Test transcription with loaded model
    - _Requirements: 3.2_

- [ ] 6. Implement JobManager service
  - [x] 6.1 Create IJobManager interface and JobManager implementation
    - Use ConcurrentDictionary for thread-safe job storage
    - Implement CreateJob() - generate unique ID, create pending job
    - Implement GetJob() - retrieve job by ID
    - Implement UpdateJobStatus() - thread-safe status updates
    - Implement GetActiveJobCount() and GetQueuedJobCount()
    - Implement IsAtCapacity() - check against limits
    - _Requirements: 3.3_
  
  - [x] 6.2 Implement StartJobProcessingAsync()
    - Update status to Processing
    - Call TranscriptionService
    - Update with result or error
    - Clean up audio files
    - _Requirements: 3.2, 3.3_
  
  - [x] 6.3 Implement automatic job cleanup with Timer
    - Create background timer (runs every hour)
    - Remove jobs older than JobCleanupMaxAgeHours
    - _Requirements: 3.3_
  
  - [ ]* 6.4 Write property tests for job management
    - **Property 9: Job status validity**
    - **Property 12: Job state transitions are valid**
    - **Property 22: Thread-safe job updates**
    - **Validates: Requirements 2.4.2, 3.3**

- [ ] 7. Implement TranscriptionService
  - [x] 7.1 Create ITranscriptionService interface and TranscriptionService implementation
    - Initialize SemaphoreSlim with MaxConcurrentWorkers
    - Implement TranscribeAsync() for batch transcription
    - Implement TranscribeStreamAsync() for streaming transcription
    - Coordinate AudioProcessor and WhisperModelService
    - Ensure proper resource cleanup in finally blocks
    - _Requirements: 3.2_
  
  - [ ]* 7.2 Write property test for worker limit enforcement
    - **Property 21: Worker limit enforcement**
    - **Validates: Requirements 3.2**
  
  - [ ]* 7.3 Write property test for temporary file cleanup
    - **Property 20: Temporary file cleanup**
    - **Validates: Requirements 4.1**

#test later-------------------------------#
- [ ]* 8. Checkpoint - Ensure core services work
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 9. Implement GlobalExceptionHandler middleware
  - [x] 9.1 Create GlobalExceptionHandler implementing IExceptionHandler
    - Map exceptions to HTTP status codes
    - Format error responses using ErrorResponse DTO
    - Log all exceptions with context
    - _Requirements: 3.6_
  
  - [ ]* 9.2 Write property tests for error handling
    - **Property 25: Error response format consistency**
    - **Property 26: Appropriate HTTP status codes**
    - **Validates: Requirements 3.6**

- [ ] 10. Implement HealthController
  - [x] 10.1 Create HealthController with health and capacity endpoints
    - Inject IWhisperModelService and IJobManager
    - Implement GET /api/v1/health - return HealthResponse
    - Implement GET /api/v1/capacity - return CapacityResponse with 503 if at capacity
    - _Requirements: 2.1, 2.2_
  
  - [ ]* 10.2 Write property tests for health endpoints
    - **Property 1: Health endpoint response completeness**
    - **Property 2: Capacity endpoint response completeness**
    - **Property 8: Available capacity calculation**
    - **Validates: Requirements 2.1.2, 2.2.2**

- [ ] 11. Implement TranscriptionController for batch operations
  - [x] 11.1 Create TranscriptionController with batch endpoints
    - Inject ITranscriptionService, IJobManager, IAudioProcessor
    - Implement POST /api/v1/transcribe/batch
      - Validate audio file (format and size)
      - Check capacity, return 503 if at capacity
      - Save uploaded file
      - Create job
      - Start background processing
      - Return BatchTranscriptionResponse
    - Implement GET /api/v1/transcribe/batch/{jobId}
      - Look up job, return 404 if not found
      - Return JobStatusResponse
    - _Requirements: 2.3, 2.4_
  
  - [ ]* 11.2 Write property tests for batch transcription
    - **Property 3: Batch upload response completeness**
    - **Property 7: Capacity limit enforcement**
    - **Property 10: Completed job has transcription**
    - **Property 11: Failed job has error message**
    - **Property 13: Valid job ID returns job**
    - **Property 14: Invalid job ID returns 404**
    - **Validates: Requirements 2.3.3, 2.3.7, 2.4.1, 2.4.3, 2.4.4, 2.4.5**

- [ ] 12. Implement StreamingTranscriptionController for WebSocket
  - [x] 12.1 Create StreamingTranscriptionController with WebSocket endpoint
    - Inject ITranscriptionService
    - Implement WebSocket handler at /api/v1/transcribe/stream
    - Initialize audio buffer
    - Receive binary chunks and append to buffer
    - When buffer >= StreamMinChunkSize:
      - Transcribe buffer
      - Send partial result as JSON
      - Clear buffer
    - On disconnect with remaining buffer:
      - Transcribe remaining data
      - Send final result
    - Handle errors and send error messages
    - Enforce StreamMaxBufferSize limit
    - _Requirements: 2.5, 3.4_
  
  - [ ]* 12.2 Write property tests for streaming transcription
    - **Property 15: Streaming accepts binary data**
    - **Property 16: Partial results on buffer threshold**
    - **Property 17: Final result on disconnect**
    - **Property 18: Streaming error handling**
    - **Validates: Requirements 2.5.2, 2.5.3, 2.5.4, 2.5.5**

- [ ] 13. Configure dependency injection and startup
  - [x] 13.1 Update Program.cs with service registration
    - Load and validate AppConfiguration from appsettings
    - Register AppConfiguration as singleton
    - Register WhisperModelService as singleton
    - Register JobManager as singleton
    - Register TranscriptionService as scoped
    - Register AudioProcessor as scoped
    - Add controllers, exception handler, logging
    - Configure WebSockets middleware
    - Optionally warm up model on startup
    - _Requirements: 4.3, 5.1_
  
  - [ ]* 13.2 Write property test for environment variable overrides
    - **Property 24: Environment variable overrides**
    - **Validates: Requirements 3.5**

- [ ] 14. Add structured logging throughout
  - [x] 14.1 Add logging to all services and controllers
    - Log request/response for all endpoints
    - Log job lifecycle events (created, processing, completed, failed)
    - Log all errors with stack traces
    - Log audio processing steps
    - Log model loading
    - Use structured logging with context (job IDs, file names, etc.)
    - _Requirements: 3.7_

- [x] 15. Final checkpoint - Integration verification
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional property-based tests (per requirements 5.3, no testing code required)
- Each task references specific requirements for traceability
- The implementation follows a bottom-up approach: models → services → controllers
- Checkpoints ensure incremental validation at key milestones
- All services use dependency injection for testability and maintainability
- Thread safety is ensured through concurrent collections and semaphores
- Resource cleanup is handled in finally blocks and IDisposable implementations
