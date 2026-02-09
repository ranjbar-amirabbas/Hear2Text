namespace TranscriptionApi.Controllers;

using Microsoft.AspNetCore.Mvc;
using TranscriptionApi.Models;
using TranscriptionApi.Services;

/// <summary>
/// Controller for health and capacity monitoring endpoints.
/// Provides information about service status, model state, and current load.
/// </summary>
[ApiController]
[Route("api/v1")]
public class HealthController : ControllerBase
{
    private readonly IWhisperModelService _modelService;
    private readonly IJobManager _jobManager;
    private readonly AppConfiguration _config;
    private readonly ILogger<HealthController> _logger;

    /// <summary>
    /// Initializes a new instance of the HealthController.
    /// </summary>
    /// <param name="modelService">Service for Whisper model management.</param>
    /// <param name="jobManager">Service for job management.</param>
    /// <param name="config">Application configuration.</param>
    /// <param name="logger">Logger instance.</param>
    public HealthController(
        IWhisperModelService modelService,
        IJobManager jobManager,
        AppConfiguration config,
        ILogger<HealthController> logger)
    {
        _modelService = modelService;
        _jobManager = jobManager;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Health check endpoint.
    /// Returns service status, model loaded state, and model size.
    /// </summary>
    /// <returns>Health information including model status.</returns>
    /// <response code="200">Service is healthy and responding.</response>
    [HttpGet("health")]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    public ActionResult<HealthResponse> GetHealth()
    {
        _logger.LogInformation("Health check requested");

        var response = new HealthResponse(
            Status: "healthy",
            ModelLoaded: _modelService.IsLoaded,
            ModelSize: _modelService.ModelSize
        );

        _logger.LogInformation(
            "Health check completed - Status: {Status}, ModelLoaded: {ModelLoaded}, ModelSize: {ModelSize}",
            response.Status,
            response.ModelLoaded,
            response.ModelSize
        );

        return Ok(response);
    }

    /// <summary>
    /// Capacity check endpoint.
    /// Returns current load information including active jobs, queued jobs, and available capacity.
    /// Returns 503 Service Unavailable when the service is at capacity.
    /// </summary>
    /// <returns>Capacity information including job counts and availability.</returns>
    /// <response code="200">Capacity information retrieved successfully.</response>
    /// <response code="503">Service is at capacity and cannot accept new jobs.</response>
    [HttpGet("capacity")]
    [ProducesResponseType(typeof(CapacityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(CapacityResponse), StatusCodes.Status503ServiceUnavailable)]
    public ActionResult<CapacityResponse> GetCapacity()
    {
        _logger.LogInformation("Capacity check requested");

        var activeJobs = _jobManager.GetActiveJobCount();
        var queuedJobs = _jobManager.GetQueuedJobCount();
        var maxWorkers = _config.MaxConcurrentWorkers;
        var maxQueueSize = _config.MaxQueueSize;
        var availableCapacity = maxQueueSize - (activeJobs + queuedJobs);
        var atCapacity = _jobManager.IsAtCapacity();

        var response = new CapacityResponse(
            ActiveJobs: activeJobs,
            QueuedJobs: queuedJobs,
            MaxWorkers: maxWorkers,
            MaxQueueSize: maxQueueSize,
            AvailableCapacity: availableCapacity,
            AtCapacity: atCapacity
        );

        _logger.LogInformation(
            "Capacity check completed - Active: {ActiveJobs}, Queued: {QueuedJobs}, Available: {AvailableCapacity}, AtCapacity: {AtCapacity}",
            response.ActiveJobs,
            response.QueuedJobs,
            response.AvailableCapacity,
            response.AtCapacity
        );

        // Return 503 if at capacity
        if (atCapacity)
        {
            _logger.LogWarning("Service is at capacity - returning 503");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, response);
        }

        return Ok(response);
    }
}
