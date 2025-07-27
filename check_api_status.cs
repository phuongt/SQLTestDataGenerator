using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System.Text.Json;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== KIỂM TRA TRẠNG THÁI API GEMINI ===");
        
        // API key từ config file test
        string apiKey = "AIzaSyCsOzujfOGEBwBvbCdPsKw8Cf16bb0iTJM";
        
        Console.WriteLine($"API Key: {apiKey.Substring(0, 10)}...");
        
        await TestGeminiAPI(apiKey);
    }
    
    static async Task TestGeminiAPI(string apiKey)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);
            
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = "Hello, can you respond with just 'API is working'?" }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.1,
                    maxOutputTokens = 50
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={apiKey}";
            
            Console.WriteLine("🔄 Đang gọi Gemini API...");
            var response = await httpClient.PostAsync(url, content);
            var responseText = await response.Content.ReadAsStringAsync();
            
            Console.WriteLine($"📊 Status Code: {response.StatusCode}");
            
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("✅ API hoạt động bình thường!");
                Console.WriteLine($"📝 Response: {responseText.Substring(0, Math.Min(200, responseText.Length))}...");
            }
            else
            {
                Console.WriteLine("❌ API có vấn đề!");
                Console.WriteLine($"🔍 Error Response: {responseText}");
                
                if (responseText.Contains("quota", StringComparison.OrdinalIgnoreCase) || 
                    responseText.Contains("limit", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("🚫 CÓ THỂ ĐÃ HẾT QUOTA API!");
                }
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"❌ Lỗi kết nối: {ex.Message}");
        }
        catch (TaskCanceledException ex)
        {
            Console.WriteLine($"⏰ Timeout: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Lỗi khác: {ex.Message}");
        }
    }
} 