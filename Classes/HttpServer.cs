using System;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Buffers;

namespace RichPresenceApp.Classes
{
    public class HttpServer : IDisposable
    {
        // HTTP listener
        private HttpListener? _listener;

        // Cancellation token source
        private CancellationTokenSource? _cancellationTokenSource;

        // Game state monitor
        private readonly GameStateMonitor _gameStateMonitor;

        // JSON options - create once and reuse
        private readonly JsonSerializerOptions _jsonOptions;

        // Buffer size for reading request bodies
        private const int BUFFER_SIZE = 4096;

        // Constructor
        public HttpServer(GameStateMonitor gameStateMonitor)
        {
            _gameStateMonitor = gameStateMonitor ?? throw new ArgumentNullException(nameof(gameStateMonitor));

            // Initialize JSON options once
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };
        }

        // Start HTTP server
        public void Start()
        {
            try
            {
                if (Config.Current == null)
                {
                    ConsoleManager.LogError("Configuration not loaded, cannot start HTTP server");
                    return;
                }

                // Stop existing server if running
                Stop();

                // Create HTTP listener
                _listener = new HttpListener();

                // Add prefix
                string prefix = $"http://{Config.Current.Host}:{Config.Current.HttpPort}/";
                _listener.Prefixes.Add(prefix);

                // Start listener
                _listener.Start();

                ConsoleManager.LogImportant($"HTTP server started at {prefix}");

                // Create cancellation token source
                _cancellationTokenSource = new CancellationTokenSource();

                // Start listening for requests
                _ = ListenAsync(_cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                ConsoleManager.LogError("Error starting HTTP server", ex);
            }
        }

        // Listen for HTTP requests
        private async Task ListenAsync(CancellationToken cancellationToken)
        {
            if (_listener == null)
            {
                ConsoleManager.LogError("HTTP listener is null, cannot listen for requests");
                return;
            }

            try
            {
                while (!cancellationToken.IsCancellationRequested && _listener.IsListening)
                {
                    // Get HTTP context with cancellation support
                    HttpListenerContext context;

                    try
                    {
                        // Use GetContextAsync with cancellation token via TaskCompletionSource
                        var tcs = new TaskCompletionSource<HttpListenerContext>();

                        using var registration = cancellationToken.Register(() =>
                            tcs.TrySetCanceled(cancellationToken));

                        var getContextTask = _listener.GetContextAsync();
                        var completedTask = await Task.WhenAny(getContextTask, tcs.Task);

                        if (completedTask == tcs.Task)
                        {
                            // Cancellation was requested
                            await tcs.Task; // This will throw OperationCanceledException
                            break;
                        }

                        context = await getContextTask;
                    }
                    catch (ObjectDisposedException)
                    {
                        // Listener was disposed, exit the loop
                        break;
                    }
                    catch (HttpListenerException ex)
                    {
                        ConsoleManager.LogError("HTTP listener error", ex);
                        break;
                    }

                    // Process request in a separate task
                    _ = ProcessRequestAsync(context);
                }
            }
            catch (OperationCanceledException)
            {
                // Ignore cancellation exceptions
            }
            catch (Exception ex)
            {
                ConsoleManager.LogError("Error listening for HTTP requests", ex);
            }
        }

        // Process HTTP request - optimized to use buffer pooling
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
                    // Read request body efficiently using a buffer from the pool
                    string body;
                    byte[] buffer = ArrayPool<byte>.Shared.Rent(BUFFER_SIZE);

                    try
                    {
                        using (var ms = new System.IO.MemoryStream())
                        {
                            int bytesRead;
                            while ((bytesRead = await request.InputStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                await ms.WriteAsync(buffer, 0, bytesRead);
                            }

                            body = Encoding.UTF8.GetString(ms.ToArray());
                        }
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(buffer);
                    }

                    // Log the received JSON (will only show in console if debug mode is enabled)
                    ConsoleManager.LogDebug($"Received game state JSON: {body}");

                    // Process game state update
                    ProcessGameStateUpdate(body);

                    // Send success response
                    await SendJsonResponseAsync(response, new { success = true });
                }
                else
                {
                    // Send error response for unsupported requests
                    response.StatusCode = 404;
                    await SendJsonResponseAsync(response, new { error = "Not found" });
                }
            }
            catch (Exception ex)
            {
                ConsoleManager.LogError("Error processing HTTP request", ex);

                try
                {
                    // Send error response
                    context.Response.StatusCode = 500;
                    await SendJsonResponseAsync(context.Response, new { error = "Internal server error" });
                }
                catch
                {
                    // Ignore errors when sending error response
                }
            }
            finally
            {
                // Always close the response
                try
                {
                    context.Response.Close();
                }
                catch
                {
                    // Ignore errors when closing response
                }
            }
        }

        // Process game state update - optimized
        private void ProcessGameStateUpdate(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                ConsoleManager.LogError("Received empty game state JSON");
                return;
            }

            try
            {
                // Parse game state with reused options
                var gameState = JsonSerializer.Deserialize<GameState>(json, _jsonOptions);

                // Process the raw data to extract nested properties
                gameState?.ProcessRawData();

                // Update game state
                if (gameState != null)
                {
                    _gameStateMonitor.UpdateGameState(gameState);
                    ConsoleManager.LogDebug("Game state updated successfully");
                }
                else
                {
                    ConsoleManager.LogError("Failed to deserialize game state JSON");
                }
            }
            catch (JsonException ex)
            {
                ConsoleManager.LogError("Error parsing game state JSON", ex);
            }
            catch (Exception ex)
            {
                ConsoleManager.LogError("Error processing game state update", ex);
            }
        }

        // Send JSON response asynchronously - optimized to use buffer pooling
        private async Task SendJsonResponseAsync(HttpListenerResponse response, object data)
        {
            try
            {
                // Serialize data
                string json = JsonSerializer.Serialize(data, _jsonOptions);
                byte[] buffer = Encoding.UTF8.GetBytes(json);

                // Set content length
                response.ContentLength64 = buffer.Length;

                // Write response asynchronously
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            }
            catch (Exception ex)
            {
                ConsoleManager.LogError("Error sending JSON response", ex);
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
                    if (!_cancellationTokenSource.IsCancellationRequested)
                    {
                        _cancellationTokenSource.Cancel();
                    }
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

                ConsoleManager.LogImportant("HTTP server stopped");
            }
            catch (Exception ex)
            {
                ConsoleManager.LogError("Error stopping HTTP server", ex);
            }
        }

        // Implement IDisposable
        public void Dispose()
        {
            Stop();
            GC.SuppressFinalize(this);
        }
    }
}

