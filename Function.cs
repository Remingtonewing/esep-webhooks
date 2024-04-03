using System.Text;
using Amazon.Lambda.Core;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace EsepWebhook;

public class Function
{
    private static readonly HttpClient client = new HttpClient();

    /// <summary>
    /// A simple function that takes a string and does a ToUpper
    /// </summary>
    /// <param name="input"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task<string> FunctionHandler(string input, ILambdaContext context)
    {
        try
        {
            var json = JsonConvert.DeserializeObject<dynamic>(input);
            
            string payload = JsonConvert.SerializeObject(new
            {
                text = $"Issue Created: {json?.issue?.html_url}"
            });
            
            var slackUrl = Environment.GetEnvironmentVariable("SLACK_URL");
            if (string.IsNullOrEmpty(slackUrl))
            {
                throw new InvalidOperationException("Slack URL is not configured in environment variables.");
            }

            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(slackUrl, content);
            
            if (!response.IsSuccessStatusCode)
            {
                context.Logger.LogLine($"Failed to send message to Slack. Status code: {response.StatusCode}");
                return $"Error: {response.ReasonPhrase}";
            }

            return await response.Content.ReadAsStringAsync();
        }
        catch (JsonException jsonEx)
        {
            context.Logger.LogLine($"JSON Error: {jsonEx.Message}");
            throw; // Rethrow the exception, it will automatically be logged in CloudWatch by Lambda
        }
        catch (HttpRequestException httpEx)
        {
            context.Logger.LogLine($"HTTP Request Error: {httpEx.Message}");
            throw;
        }
        catch (Exception ex)
        {
            context.Logger.LogLine($"Error: {ex.Message}");
            throw;
        }
    }
}
