using Azure.AI.OpenAI;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

// See https://aka.ms/new-console-template for more information
class Program
{
    private static string _endpoint;
    private static string _key;
    private static string _deploymentOrModelName4;
    private static string _deploymentOrModelName35;

    private static readonly string userPrompt = "Analyze the following SQL stored procedure and provide a detailed breakdown of its business logic, including operations performed, data manipulations, and any conditional logic used. Identify the main variables and their roles in the procedure.Analyze the following SQL stored procedure and provide a detailed breakdown of its business logic, including operations performed, data manipulations, and any conditional logic used. Identify the main variables and their roles in the procedure.";
    private static readonly string systemPrompt = "As an intelligent assistant specialized in software development, I am equipped to handle a range of tasks involving code analysis, translation, and generation.My capabilities include understanding and interpreting SQL stored procedures, translating business logic into different programming languages, particularly C#, and constructing well-designed software components. I am here to assist in converting complex database operations and logic into efficient and robust C# code, adhering to best practices in software development. Feel free to provide specific instructions or requirements for your coding tasks, and I will adapt my analysis and output accordingly.";

    static async Task Main(string[] args)
    {

        var builder = new ConfigurationBuilder()
           .AddUserSecrets<Program>();

        var configuration = builder.Build();

        _endpoint = configuration["OpenAI:Endpoint"];
        _key = configuration["OpenAI:Key"];
        _deploymentOrModelName4 = configuration["OpenAI:DeploymentOrModelName4"];
        _deploymentOrModelName35 = configuration["OpenAI:DeploymentOrModelName35"];

        string directoryPath = @"C:\batch"; // Replace with your directory path\
        string completionResponse = string.Empty;   

        foreach (var file in Directory.EnumerateFiles(directoryPath, "*.sql"))
        {
            var contents = string.Empty;
            
            try
            {
                contents = File.ReadAllText(file);
                Console.WriteLine($"Content of {file}:\n{contents}\n");
            }
            catch (Exception ex)
            {
                var message = $" Exception thrown reading file from directory: {ex.Message}";
                throw;
            }
            
            // Parse out unnecessary characters
            //string pattern = @"/\*.*?\*/\r\nSET ANSI_NULLS ON\r\nGO\r\n\r\nSET QUOTED_IDENTIFIER ON\r\nGO\r\n\r\n|GO\r\n\r\n$";
            // Remove comments, SET ANSI_NULLS ON, GO, SET QUOTED_IDENTIFIER ON, \r, \n
            string pattern = @"(/\*.*?\*/|SET ANSI_NULLS ON|GO|SET QUOTED_IDENTIFIER ON|\r|\n)+";

            string scrubbed1 = Regex.Replace(contents.ToString(), pattern, "", RegexOptions.Singleline);

            // Remove tabs and quotes
            var scrubbed2 = scrubbed1.Replace("\t", "").Replace("\\\"", "");

            try
            {
                OpenAIClient client = new(
                  new Uri(_endpoint),
                  new Azure.AzureKeyCredential(_key));

                var summaryCompletion = await client.GetChatCompletionsAsync(new ChatCompletionsOptions
                {
                    DeploymentName = _deploymentOrModelName4,
                    Temperature = 0.0f,
                    MaxTokens = 1000,
                    //FrequencyPenalty = _frequencyPenalty,
                    //PresencePenalty = _PresencePenalty,
                    //NucleusSamplingFactor = _nucleus,
                    Messages = {
                        new ChatRequestSystemMessage(systemPrompt),
                        new ChatRequestSystemMessage(userPrompt),
                        new ChatRequestUserMessage(scrubbed2)
                    }
                });

                completionResponse = summaryCompletion.Value.Choices.FirstOrDefault()?.Message?.Content;
            }

            catch (Exception ex)
            {
                var message = $" Exception thrown conversing with LLM: {ex.Message}";   
                throw;
            }

            try
            {
                // Perform your action here
                string newDirectoryPath = @"C:\completions";
                string newFilePath = Path.Combine(newDirectoryPath, Path.GetFileName(file) + ".txt");
                File.WriteAllText(newFilePath, completionResponse);
            }
            catch (Exception ex)
            {
                var message = $" Exception thrown writing file to directory: {ex.Message}";
                throw;
            }
        }
    }
}
