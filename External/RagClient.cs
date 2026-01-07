using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LocalRAGChatbotUI.Services
{
    internal class RagClient
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "http://localhost:8081";

        public RagClient()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(60)
            };
        }
        public async Task<bool> AddDocumentsAsync(
            string document
        )
        {
            var payload = new
            {
                path = document,
            };

            var json = JsonSerializer.Serialize(payload);

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{BaseUrl}/add-docs"
            )
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            using var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }


        public async Task<string> QueryAsync(string query, int topK = 5)
        {
            var payload = new
            {
                query = query,
                top_k = topK
            };

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{BaseUrl}/query"
            )
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json")
            };

            using var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("results", out var results))
                return string.Empty;

            var sb = new StringBuilder();

            foreach (var item in results.EnumerateArray())
            {
                if (item.TryGetProperty("text", out var text))
                {
                    sb.AppendLine(text.GetString());
                    sb.AppendLine(); 
                }
            }

            return sb.ToString().Trim();
        }

        public async Task<bool> Initialize()
        {
            try
            {
                var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{BaseUrl}/init"
            );
                Debug.Write("Initialising RAG");
                using var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();


                using var doc = JsonDocument.Parse(json);

                if (!doc.RootElement.TryGetProperty("status", out var results))
                    return false;
                Debug.Write(results.GetString());
                return results.GetString() == "success";
            }
            catch
            {
                return false;
            }
        }
    }
}
