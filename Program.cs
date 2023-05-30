public class Program
{
    private static async Task Main(string[] args)
    {
        
        // Setting file name
        string settingsFile = "Settings.user";

        // Check that the file exists
        if (!File.Exists(settingsFile)) 
        {
            Console.WriteLine("\n>>> ERROR: Please create Settings.user.txt file and enter all required information.");
            return;
        }

        // Read configuration parameters from settings file
        var settings = File.ReadAllLines(settingsFile)
                    .Select(l => l.Split(new[] { '=' }))
                    .ToDictionary(s => s[0].Trim(), s => s[1].Trim());    

        // Check that ALL required values are present in the settings file
        if (settings["APIKEY"] == "" || settings["USERNAME"] == "" || settings["PASSWORD"] == "")
        {
            Console.WriteLine("\n>>> ERROR: Please provide all required information in the Settings.user.txt file.");
            return;
        }

        // Get OpenAI to generate quiz and store the JSON return into array along with the status code
        string[] generatedQuiz = await Openai.GenerateQuiz(
        settings["APIKEY"], 
        settings["APIURL"], 
        settings["PROMPT"], 
        settings["MODEL"]);

        // Check that there is a response and the status code returned OK
        if (generatedQuiz[0] == null || generatedQuiz[0] == "" || generatedQuiz[1] != "OK")
        {
            Console.WriteLine($"\n>>> ERROR: A problem has occurred while generating or parsing the quiz response.");
            Console.WriteLine($">>> Status Code: {generatedQuiz[1]}\nGenerated Quiz: {generatedQuiz[0]}");
            return;
        }
            
        Console.WriteLine($"-> Parsed JSON output from Open AI:\n{generatedQuiz[0]}");

        // Create Connection String for NPGSQL
        string connectionString = $"Host={settings["HOST"]};Username={settings["USERNAME"]};Password={settings["PASSWORD"]};Database={settings["DATABASE"]}"; 
        // Parse the JSON output and add it to Postgres
        Quiz.AddQuiz(generatedQuiz[0], generatedQuiz[2], connectionString);
    }
}