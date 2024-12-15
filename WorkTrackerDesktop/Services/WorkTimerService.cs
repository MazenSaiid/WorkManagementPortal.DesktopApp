using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Timers;
using WorkTrackerDesktop.Dtos;
using WorkTrackerDesktop.Responses;

namespace WorkTrackerDesktop.Services
{
    public class WorkTimerService
    {
        private readonly HttpClient _httpClient;
        private readonly string _workSessionUrl;
        public WorkTimerService(IConfiguration config)
        {
            // Set up Serilog to log to both console and file
            string logFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "YourApp", "logs", "log.txt");

            // Create directory if it doesn't exist
            Directory.CreateDirectory(Path.GetDirectoryName(logFilePath));

            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()  // Output logs to console
                .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day) // Log to file
                .CreateLogger();

            _httpClient = new HttpClient();
            _workSessionUrl = config["ApiBaseUrl"] ?? "https://localhost:7119/api/";
            
        }

        public async Task<WorkTrackingResponse> StartAsync()
        {
            try
            {
                var userId = WorkSessionService.Instance.UserId = UserSessionService.Instance.UserId;

                var response = await _httpClient.PostAsJsonAsync(_workSessionUrl + "WorkTrackings/ClockIn", userId);
                if (response.IsSuccessStatusCode)
                {
                    // Read the response content as a string (raw JSON)
                    var contentAsString = await response.Content.ReadAsStringAsync();

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
                        // Handle case where the deserialization does not return a valid object
                        return new WorkTrackingResponse
                        {
                            Success = false,
                            Message = "Invalid response format.",
                            Token = null,
                            WorkTrackingLog = null,
                        };
                    }
                }
                else
                {
                    return new WorkTrackingResponse
                    {
                        Success = false,
                        Message = "Failed to start work session.",
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
                    Message = "An error occurred while starting the work session.",
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

         

                var response = await _httpClient.PostAsJsonAsync(_workSessionUrl + "WorkTrackings/StartPause", data);
                if (response.IsSuccessStatusCode)
                {
                    // Read the response content as a string (raw JSON)
                    var contentAsString = await response.Content.ReadAsStringAsync();

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
                        // Handle case where the deserialization does not return a valid object
                        return new PauseTrackingResponse
                        {
                            Success = false,
                            Message = "Invalid response format.",
                            Token = null,
                            PauseTrackingLog = null,
                        };
                    }
                    
                }
                else
                {
                    return new PauseTrackingResponse
                    {
                        Success = false,
                        Message = "Failed to pause work session.",
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
                    Message = "An error occurred while pausing the work session.",
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

                var response = await _httpClient.PostAsJsonAsync(_workSessionUrl + "WorkTrackings/EndPause", workLogId);
                if (response.IsSuccessStatusCode)
                {
                    // Read the response content as a string (raw JSON)
                    var contentAsString = await response.Content.ReadAsStringAsync();

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
                        // Handle case where the deserialization does not return a valid object
                        return new PauseTrackingResponse
                        {
                            Success = false,
                            Message = "Invalid response format.",
                            Token = null,
                            PauseTrackingLog = null,
                        };
                    }

                }
                else
                {
                    return new PauseTrackingResponse
                    {
                        Success = false,
                        Message = "Failed to resume work session.",
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
                    Message = "An error occurred while resuming the work session.",
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

                var response = await _httpClient.PostAsJsonAsync(_workSessionUrl + "WorkTrackings/ClockOut", workLogId);
                if (response.IsSuccessStatusCode)
                {
                    // Read the response content as a string (raw JSON)
                    var contentAsString = await response.Content.ReadAsStringAsync();

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
                        // Handle case where the deserialization does not return a valid object
                        return new WorkTrackingResponse
                        {
                            Success = false,
                            Message = "Invalid response format.",
                            Token = null,
                            WorkTrackingLog = null,
                        };
                    }
                }
                else
                {
                    return new WorkTrackingResponse
                    {
                        Success = false,
                        Message = "Failed to stop work session.",
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
                    Message = "An error occurred while stopping the work session.",
                    Token = null,
                    WorkTrackingLog = null
                };
            }
        }
    }
}
