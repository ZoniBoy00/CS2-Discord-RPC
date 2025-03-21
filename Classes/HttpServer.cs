using System;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RichPresenceApp.Classes
{
    public class HttpServer
    {
        // HTTP listener
        private HttpListener? _listener;

        // Cancellation token source
        private CancellationTokenSource? _cancellationTokenSource;

        // Game state monitor
        private readonly GameStateMonitor _gameStateMonitor;

        // Constructor
        public HttpServer(GameStateMonitor gameStateMonitor)
        {
            _gameStateMonitor = gameStateMonitor ?? throw new ArgumentNullException(nameof(gameStateMonitor));
        }

        // Start HTTP server
        public void Start()
        {
            try
            {
                if (Config.Current == null)
                {
                    ConsoleManager.WriteLine("Configuration not loaded, cannot start HTTP server", ConsoleColor.Red, true);
                    return;
                }

                // Create HTTP listener
                _listener = new HttpListener();

                // Add prefix
                string prefix = $"http://{Config.Current.Host}:{Config.Current.HttpPort}/";
                _listener.Prefixes.Add(prefix);

                // Start listener
                _listener.Start();

                ConsoleManager.WriteLine($"HTTP server started at {prefix}", ConsoleColor.Green, true);

                // Create cancellation token source
                _cancellationTokenSource = new CancellationTokenSource();

                // Start listening for requests
                Task.Run(() => ListenAsync(_cancellationTokenSource.Token));
            }
            catch (Exception ex)
            {
                ConsoleManager.WriteLine($"Error starting HTTP server: {ex.Message}", ConsoleColor.Red, true);
            }
        }

        // Listen for HTTP requests
        private async Task ListenAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (_listener == null)
                {
                    ConsoleManager.WriteLine("HTTP listener is null, cannot listen for requests", ConsoleColor.Red, true);
                    return;
                }

                while (!cancellationToken.IsCancellationRequested && _listener.IsListening)
                {
                    try
                    {
                        // Get HTTP context
                        HttpListenerContext context = await _listener.GetContextAsync();

                        // Process request in a separate task
                        _ = Task.Run(() => ProcessRequestAsync(context), cancellationToken);
                    }
                    catch (ObjectDisposedException)
                    {
                        // Listener was disposed, exit the loop
                        break;
                    }
                    catch (HttpListenerException ex)
                    {
                        ConsoleManager.WriteLine($"HTTP listener error: {ex.Message}", ConsoleColor.Red, true);
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Ignore cancellation exceptions
            }
            catch (Exception ex)
            {
                ConsoleManager.WriteLine($"Error listening for HTTP requests: {ex.Message}", ConsoleColor.Red, true);
            }
        }

        // Process HTTP request
        private async Task ProcessRequestAsync(HttpListenerContext context)
        {
            try
            {
                // Get request and response
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;

                // Set response headers
                response.ContentType = "application/json";
                response.Headers.Add("Access-Control-Allow-Origin", "*");

                // Handle request based on method and path
                if (request.HttpMethod == "POST" && request.Url?.AbsolutePath == "/")
                {
                    // Read request body
                    using var reader = new System.IO.StreamReader(request.InputStream, request.ContentEncoding);
                    string body = await reader.ReadToEndAsync();

                    // Log the received JSON (will only show in console if debug mode is enabled)
                    ConsoleManager.WriteLine($"Received game state JSON: {body}", ConsoleColor.Cyan);

                    // Process game state update
                    ProcessGameStateUpdate(body);

                    // Send success response
                    SendJsonResponse(response, new { success = true });
                }
                else
                {
                    // Send error response for unsupported requests
                    response.StatusCode = 404;
                    SendJsonResponse(response, new { error = "Not found" });
                }
            }
            catch (Exception ex)
            {
                ConsoleManager.WriteLine($"Error processing HTTP request: {ex.Message}", ConsoleColor.Red, true);

                try
                {
                    // Send error response
                    context.Response.StatusCode = 500;
                    SendJsonResponse(context.Response, new { error = "Internal server error" });
                }
                catch
                {
                    // Ignore errors when sending error response
                }
            }
        }

        // Process game state update
        private void ProcessGameStateUpdate(string json)
        {
            try
            {
                if (string.IsNullOrEmpty(json))
                {
                    ConsoleManager.WriteLine("Received empty game state JSON", ConsoleColor.Yellow, true);
                    return;
                }

                // Parse game state with more flexible options
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                };

                try
                {
                    var gameState = JsonSerializer.Deserialize<GameState>(json, options);

                    // Process the raw data to extract nested properties
                    gameState?.ProcessRawData();

                    // Update game state
                    if (gameState != null)
                    {
                        _gameStateMonitor.UpdateGameState(gameState);
                    }
                    else
                    {
                        ConsoleManager.WriteLine("Failed to deserialize game state JSON", ConsoleColor.Red, true);
                    }
                }
                catch (JsonException ex)
                {
                    ConsoleManager.WriteLine($"Error parsing game state JSON: {ex.Message}", ConsoleColor.Red, true);
                }
            }
            catch (Exception ex)
            {
                ConsoleManager.WriteLine($"Error processing game state update: {ex.Message}", ConsoleColor.Red, true);
            }
        }

        // Send JSON response
        private void SendJsonResponse(HttpListenerResponse response, object data)
        {
            try
            {
                // Serialize data
                string json = JsonSerializer.Serialize(data);
                byte[] buffer = Encoding.UTF8.GetBytes(json);

                // Set content length
                response.ContentLength64 = buffer.Length;

                // Write response
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.OutputStream.Close();
            }
            catch (Exception ex)
            {
                ConsoleManager.WriteLine($"Error sending JSON response: {ex.Message}", ConsoleColor.Red, true);
            }
        }

        // Toggle debug mode
        public void ToggleDebugMode()
        {
            ConsoleManager.ToggleDebugMode();
        }

        // Stop HTTP server
        public void Stop()
        {
            try
            {
                // Cancel listening
                if (_cancellationTokenSource != null)
                {
                    _cancellationTokenSource.Cancel();
                    _cancellationTokenSource.Dispose();
                    _cancellationTokenSource = null;
                }

                // Stop listener
                if (_listener != null)
                {
                    if (_listener.IsListening)
                    {
                        _listener.Stop();
                    }
                    _listener.Close();
                    _listener = null;
                }

                ConsoleManager.WriteLine("HTTP server stopped", ConsoleColor.Yellow, true);
            }
            catch (Exception ex)
            {
                ConsoleManager.WriteLine($"Error stopping HTTP server: {ex.Message}", ConsoleColor.Red, true);
            }
        }
    }
}