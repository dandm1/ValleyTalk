using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

namespace ValleyTalk.Platform
{
    /// <summary>
    /// Helper class for Android-compatible network operations
    /// </summary>
    public static class NetworkHelper
    {
        private static readonly HttpClient _httpClient;
        
        static NetworkHelper()
        {
            var handler = new HttpClientHandler();
            
            // Android-specific configuration
            if (AndroidHelper.IsAndroid)
            {
                // Increase timeout for mobile networks
                _httpClient = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(30)
                };
                
                // Add mobile-friendly headers
                _httpClient.DefaultRequestHeaders.Add("User-Agent", 
                    "ValleyTalk/1.0 (Android; Stardew Valley Mod)");
            }
            else
            {
                _httpClient = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(10)
                };
            }
        }

        /// <summary>
        /// Makes an HTTP request with Android-compatible settings
        /// </summary>
        public static async Task<string> MakeRequestAsync(string url, string content = null, CancellationToken cancellationToken = default, string authToken = null)
        {
            try
            {
                HttpResponseMessage response;
                
                if (string.IsNullOrEmpty(content))
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, url);
                    if (!string.IsNullOrEmpty(authToken))
                        request.Headers.Add("Authorization", $"Bearer {authToken}");
                    response = await _httpClient.SendAsync(request, cancellationToken);
                }
                else
                {
                    var stringContent = new StringContent(content, System.Text.Encoding.UTF8, "application/json");
                    var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = stringContent };
                    if (!string.IsNullOrEmpty(authToken))
                        request.Headers.Add("Authorization", $"Bearer {authToken}");
                    response = await _httpClient.SendAsync(request, cancellationToken);
                }

                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException("Request was cancelled");
            }
            catch (TaskCanceledException)
            {
                throw new TimeoutException("Request timed out");
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException($"Network request failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Makes an HTTP request with custom headers for specific LLM providers
        /// </summary>
        public static async Task<string> MakeRequestWithCustomHeadersAsync(string url, string content, Dictionary<string, string> headers, CancellationToken cancellationToken = default)
        {
            try
            {
                var stringContent = new StringContent(content, System.Text.Encoding.UTF8, "application/json");
                var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = stringContent };
                
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        request.Headers.Add(header.Key, header.Value);
                    }
                }

                var response = await _httpClient.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException("Request was cancelled");
            }
            catch (TaskCanceledException)
            {
                throw new TimeoutException("Request timed out");
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException($"Network request failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Checks if network is available (basic check for Android)
        /// </summary>
        public static bool IsNetworkAvailable()
        {
            try
            {
                // Basic connectivity test
                return System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Disposes the HTTP client
        /// </summary>
        public static void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
