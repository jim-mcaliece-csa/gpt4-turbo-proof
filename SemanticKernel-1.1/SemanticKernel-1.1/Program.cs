using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Text.Json;
using Kernel = Microsoft.SemanticKernel.Kernel;
using Newtonsoft.Json;
using System.Text;

namespace SemanticKernel_1._1
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
           

            // Configure AI service credentials used by the kernel
            string localSettings = File.ReadAllText(@"..\..\..\local.settings.json");

            // Deserialize the JSON content into a configuration object
           
            AppSettings settings = JsonConvert.DeserializeObject<AppSettings>(localSettings);

            var model = settings.Model;
            var azureEndpoint = settings.AzureEndpoint;
            var apiKey = settings.ApiKey;

            var builder = Kernel.CreateBuilder();

            builder.AddAzureOpenAIChatCompletion(model, azureEndpoint, apiKey);
            

            var kernel = builder.Build();

            const string skPrompt = @"
                    ChatBot can have a conversation with you about any topic.
                    It can give explicit instructions or say 'I don't know' if it does not have an answer.

                    {{$history}}
                    User: {{$userInput}}
                    ChatBot:";

            var executionSettings = new OpenAIPromptExecutionSettings
            {
                MaxTokens = 2000,
                Temperature = 0.7,
                TopP = 0.5
            };

            var chatFunction = kernel.CreateFunctionFromPrompt(skPrompt, executionSettings);

            var history = "";
            var arguments = new KernelArguments()
            {
                ["history"] = history
            };

            var userInput = "Hi, I'm looking for book suggestions";
            arguments["userInput"] = userInput;

            StringBuilder bot_answer = new StringBuilder(); // await chatFunction.InvokeAsync(kernel, arguments);

            await foreach (var s in chatFunction.InvokeStreamingAsync<string>(kernel, arguments))
            {
                // I would expect to start hitting this point soon after the kernel is launched. However it takes 
                // as long as without streaming to get here.
                Console.Write($"{s}");
                bot_answer.AppendLine(s);
            }

            history += $"\nUser: {userInput}\nAI: {bot_answer.ToString()}\n";
            arguments["history"] = history;

            //Console.WriteLine(history);

            Func<string, Task> Chat = async (string input) => {
                // Save new message in the arguments
                arguments["userInput"] = input;

                // Process the user message and get an answer
                StringBuilder answer = new StringBuilder();// await chatFunction.InvokeAsync(kernel, arguments);

                await foreach (var s in chatFunction.InvokeStreamingAsync<string>(kernel, arguments))
                {
                    // I would expect to start hitting this point soon after the kernel is launched. However it takes 
                    // as long as without streaming to get here.
                    Console.Write($"{s}");
                    answer.AppendLine(s);
                }

                // Append the new interaction to the chat history
                var result = $"\nUser: {input}\nAI: {answer.ToString()}\n";
                history += result;

                arguments["history"] = history;

                // Show the response
                //Console.WriteLine(result);
            };

            await Chat("I would like a non-fiction book suggestion about Greece history. Please only list one book.");
            await Chat("that sounds interesting, what are some of the topics I will learn about?");
            await Chat("Which topic from the ones you listed do you think most people find interesting?");
            await Chat("could you list some more books I could read about the topic(s) you mentioned?");
            Console.WriteLine(history);
        }
    }
    public class AppSettings
    {
        [JsonProperty("apiKey")]
        public string ApiKey { get; set; }
        [JsonProperty("model")]
        public string Model { get; set; }
        [JsonProperty("azureEndpoint")]
        public string AzureEndpoint { get; set; }
        // Add other settings as needed
    }
}