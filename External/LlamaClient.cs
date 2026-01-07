using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using LocalRAGChatbotUI.Services;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;


namespace LocalRAGChatbotUI.Services
{
    public class LlamaClient
    {
        private readonly HttpClient _httpClient = new HttpClient();
        private readonly RagClient _RagClient = new();

        public async IAsyncEnumerable<string> StreamCompletion(string prompt , bool rag_on = false)
        {
            string preprompt = "You are TutorLM ,an AI Powered Helpful Assistant Developed by `srAIlaabs`, who helps students understand Concepts clearly . Do not Make up Facts , only answer questions which you are confident enough if not answer ***Sorry im not sure if the question is relevant for my purpose***";
            if(rag_on)
            {
                var rag_context = await _RagClient.QueryAsync(prompt,3);
                Debug.Write(rag_context);
                preprompt += $"\n\nUse the following context to answer the question:\n{rag_context}\n\n";
            }
            

            var payload = new
            {
                messages = new[]
                {
            new { role = "user", content = preprompt+prompt }
        },
                temperature = 0.7,
                stream = true,
                top_p = 0.9,
                n_predict = 512
            };

            var jsoni = JsonSerializer.Serialize(payload);
            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost:8080/chat/completions")
            {
                Content = new StringContent(jsoni, Encoding.UTF8, "application/json")
            };

            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead 
            );

            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;

                if (line.StartsWith("data:"))
                    line = line.Substring("data:".Length).Trim();

                if (line == "[DONE]" || line.StartsWith(":"))
                    continue;

                try
                {
                    using var jsonLine = JsonDocument.Parse(line);

                    if (jsonLine.RootElement.TryGetProperty("choices", out var choices))
                    {
                        var delta = choices[0].GetProperty("delta");
                        if (delta.TryGetProperty("content", out var contentProp))
                            yield return contentProp.GetString() ?? "";
                    }
                }
                finally 
                {
                    
                }
            }
        }

    }
}

