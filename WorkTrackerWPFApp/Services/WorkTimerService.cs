using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Serilog;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using WorkTrackerDesktopWPFApp.Dtos;
using WorkTrackerDesktopWPFApp.Responses;
using WorkTrackerWPFApp.Services;
using WorkTrackerWPFApp.Services.Static;

namespace WorkTrackerDesktopWPFApp.Services
{
    public class WorkTimerService
    {
        private readonly HttpClient _httpClient;
        private readonly string _workSessionUrl;
        private readonly string _username;

        // Paths
        private readonly string _logFilePath;
        public WorkTimerService(IConfiguration config)
        {
            _logFilePath = PathHelperService.GetBaseDirectoryLogFilePath(config);
            // Configure Serilog
            SetUpLogging();

            _httpClient = new HttpClient();
            _workSessionUrl = config["ApiBaseUrl"];
            
        }
        private void SetUpLogging()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console() // Logs to the console
                .WriteTo.File(Path.Combine(_logFilePath, $"WorkTrackingLogs_{DateTime.Now:yyyy-MM-dd}.txt"), rollingInterval: RollingInterval.Day) // Logs to a daily rotating file
                .CreateLogger();
        }
        public async Task<WorkTrackingResponse> StartAsync()
        {
            try
            {
                // Log using the injected logger
                Log.Information("Starting work session.");
                var userId = WorkSessionService.Instance.UserId = UserSessionService.Instance.UserId;

                var response = await _httpClient.PostAsJsonAsync(_workSessionUrl + "WorkTrackings/ClockIn", userId);
                // Read the response content as a string (raw JSON)
                var contentAsString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    // Deserialize the raw JSON string into a strongly-typed object (response structure)
                    var workLogResponseDto = JsonConvert.DeserializeObject<WorkTrackingResponse>(contentAsString);
                    // Check if the response contains the expected work tracking log data
                    if (workLogResponseDto != null)
                    {
                        return new WorkTrackingResponse
                        {
                            Success = workLogResponseDto.Success,
                            Message = workLogResponseDto.Message,
                            Token = workLogResponseDto.Token,
                            WorkTrackingLog = workLogResponseDto.WorkTrackingLog
                        };
                    }
                    else
                    {
                        Log.Error($"Invalid response from server \n {workLogResponseDto.Message}");
                        // Handle case where the deserialization does not return a valid object
                        return new WorkTrackingResponse
                        {
                            Success = false,
                            Message = workLogResponseDto.Message,
                            Token = null,
                            WorkTrackingLog = null,
                        };
                        
                    }
                }
                else
                {
                    Log.Error("Failed to start work session");
                    return new WorkTrackingResponse
                    {
                        Success = false,
                        Message = contentAsString,
                        Token = null,
                        WorkTrackingLog = null,
                    };
                }
            }
            catch (Exception ex)
            {
                // Log the error using the injected logger
                Log.Error(ex, "Error starting work session.");

                return new WorkTrackingResponse
                {
                    Success = false,
                    Message = $"An error occurred while starting the work session.\n {ex.Message}",
                    Token = null,
                    WorkTrackingLog = null
                };
            }
        }


        public async Task<PauseTrackingResponse> PauseAsync(int pauseType)
        {
            try
            {
                var data = new StartPauseDto
                {
                    WorkLogId = WorkSessionService.Instance.WorkLogId,
                    PauseType = pauseType
                };


                // Log using the injected logger
                Log.Information("Pausing work session.");

                var response = await _httpClient.PostAsJsonAsync(_workSessionUrl + "WorkTrackings/StartPause", data);
                var contentAsString = await response.Content.ReadAsStringAsync();

                
                if (response.IsSuccessStatusCode)
                {
                    // Deserialize the raw JSON string into a strongly-typed LoginResponse object using JsonConvert
                    var workLogResponseDto = JsonConvert.DeserializeObject<PauseTrackingResponse>(contentAsString);
                    // Check if the response contains the expected work tracking log data
                    if (workLogResponseDto != null)
                    {
                        return new PauseTrackingResponse
                        {
                            Success = workLogResponseDto.Success,
                            Token = workLogResponseDto.Token,
                            Message = workLogResponseDto.Message,
                            PauseTrackingLog = workLogResponseDto.PauseTrackingLog
                        };
                    }
                    else
                    {
                        Log.Error($"Invalid response from server \n {workLogResponseDto.Message}");
                        // Handle case where the deserialization does not return a valid object
                        return new PauseTrackingResponse
                        {
                            Success = false,
                            Message = $"Invalid response \n {workLogResponseDto.Message}",
                            Token = null,
                            PauseTrackingLog = null,
                        };
                    }
                    
                }
                else
                {
                    Log.Error($"Failed to pause work session. \n{contentAsString}");
                    return new PauseTrackingResponse
                    {
                        Success = false,
                        Message = $"{contentAsString}",
                        Token = null,
                        PauseTrackingLog = null,
                    };

                }
            }
            catch (Exception ex)
            {
                // Log the error using the injected logger
                Log.Error(ex, "Error pausing work session.");
                return new PauseTrackingResponse
                {
                    Success = false,
                    Message = $"An error occurred while pausing the work session. \n {ex.Message}",
                    Token = null,
                    PauseTrackingLog = null
                };
            }
        }

        public async Task<PauseTrackingResponse> ResumeAsync()
        {
            try
            {
                var workLogId = WorkSessionService.Instance.WorkLogId;
                // Log using the injected logger
                Log.Information("Resuming work session.");
                var response = await _httpClient.PostAsJsonAsync(_workSessionUrl + "WorkTrackings/EndPause", workLogId);
                // Read the response content as a string (raw JSON)
                var contentAsString = await response.Content.ReadAsStringAsync();

                
                if (response.IsSuccessStatusCode)
                {
                    // Deserialize the raw JSON string into a strongly-typed LoginResponse object using JsonConvert
                    var workLogResponseDto = JsonConvert.DeserializeObject<PauseTrackingResponse>(contentAsString);
                    if (workLogResponseDto != null)
                    {
                        return new PauseTrackingResponse
                        {
                            Success = workLogResponseDto.Success,
                            Token = workLogResponseDto.Token,
                            Message = workLogResponseDto.Message,
                            PauseTrackingLog = workLogResponseDto.PauseTrackingLog
                        };
                    }
                    else
                    {
                        Log.Error($"Invalid response from server \n {workLogResponseDto.Message}");

                        // Handle case where the deserialization does not return a valid object
                        return new PauseTrackingResponse
                        {
                            Success = workLogResponseDto.Success,
                            Message = $"Invalid response from server \n {workLogResponseDto.Message}",
                            Token = null,
                            PauseTrackingLog = null,
                        };
                    }

                }
                else
                {
                    Log.Error("Failed to resume work session.");
                    return new PauseTrackingResponse
                    {
                        Success = false,
                        Message = $"{contentAsString}",
                        Token = null,
                        PauseTrackingLog = null,
                    };

                }
            }
            catch (Exception ex)
            {
                // Log the error using the injected logger
                Log.Error(ex, "Error resuming work session.");
                return new PauseTrackingResponse
                {
                    Success = false,
                    Message = $"An error occurred while resuming the work session. \n {ex.Message}",
                    Token = null,
                    PauseTrackingLog = null
                };
            }
        }

        public async Task<WorkTrackingResponse> StopAsync()
        {
            try
            {
                var workLogId = WorkSessionService.Instance.WorkLogId;
                // Log using the injected logger
                Log.Information("Stopping work session.");
                var response = await _httpClient.PostAsJsonAsync(_workSessionUrl + "WorkTrackings/ClockOut", workLogId);
                // Read the response content as a string (raw JSON)
                var contentAsString = await response.Content.ReadAsStringAsync();

                
                if (response.IsSuccessStatusCode)
                {
                    // Deserialize the raw JSON string into a strongly-typed LoginResponse object using JsonConvert
                    var workLogResponseDto = JsonConvert.DeserializeObject<WorkTrackingResponse>(contentAsString);
                    if (workLogResponseDto != null)
                    {
                        return new WorkTrackingResponse
                        {
                            Success = workLogResponseDto.Success,
                            Message = workLogResponseDto.Message,
                            Token = workLogResponseDto.Token,
                            WorkTrackingLog = workLogResponseDto.WorkTrackingLog
                        };
                    }
                    else
                    {
                        Log.Error($"Invalid response from server \n {workLogResponseDto.Message}");
                        // Handle case where the deserialization does not return a valid object
                        return new WorkTrackingResponse
                        {
                            Success = false,
                            Message = $"Invalid response format. \n {workLogResponseDto.Message}",
                            Token = null,
                            WorkTrackingLog = null,
                        };
                    }
                }
                else
                {
                    Log.Error("Failed to stop work session.");
                    return new WorkTrackingResponse
                    {
                        Success = false,
                        Message = $"{contentAsString}",
                        Token = null,
                        WorkTrackingLog = null,
                    };
                }
            }
            catch (Exception ex)
            {
                // Log the error using the injected logger
                Log.Error(ex, "Error stopping work session.");

                return new WorkTrackingResponse
                {
                    Success = false,
                    Message = $"An error occurred while stopping the work session. \n {ex.Message}",
                    Token = null,
                    WorkTrackingLog = null
                };
            }
        }
    }
}
