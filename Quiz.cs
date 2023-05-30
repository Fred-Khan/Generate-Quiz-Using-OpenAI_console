using System.Text.Json;
using Npgsql;

public class Quiz
{
    public static void AddQuiz(string getQuestionsJSON, string prompt_Text, string connectionString)
    {

        // Create the required tables in case it does not exist
        Quiz.CreateTablesIfNotExists(connectionString);

        // Declare the document variable
        JsonDocument? document = null;

        try
        {
            // Parse the JSON string into a JsonDocument object
            document = JsonDocument.Parse(getQuestionsJSON);
        }
        catch (JsonException ex)
        {
            // Handle the JSON parsing exception
            Console.WriteLine($"\nERROR: An error occurred during JsonDocument parsing: {ex.Message}\nExecution halted.\nPlease re-run and try again.");
            return;
        }

        // Establish a connection to the database
        using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
        {
            // Open the database connection
            connection.Open();

            // Check if the document is null before proceeding
            if (document is null)
            {
                Console.WriteLine("\nERROR: Failed to generate the quiz. The JSON document is null.");
                connection.Close();
                return;
            }

            // Declare variable to hold the prompt ID generated from inserting this prompt
            int prompt_ID = 0;

            // Insert Prompt Text into quiz_prompt table and retrieve the ID generated
            using (NpgsqlCommand command = new NpgsqlCommand("INSERT INTO quiz_prompts (prompt_text) VALUES (@promptText) RETURNING id", connection))
            {
                // Check if the prompt text is not null
                if (prompt_Text is not null)
                {
                    command.Parameters.AddWithValue("@promptText", NpgsqlTypes.NpgsqlDbType.Text, prompt_Text);
                    // Execute the command and retrieve the generated ID
                    object result = command.ExecuteScalar()!;
                    prompt_ID = (int)result;
                    Console.WriteLine($"Generated ID for this prompt: {prompt_ID}");
                }
            }



            // Iterate through each property in the JSON document and insert into tables
            foreach (JsonProperty property in document.RootElement.EnumerateObject())
            {
                // Declare variable to hold the question ID generated from inserting this question
                int question_ID = 0;

                // Insert Questions into questions table and retrieve the ID generated
                using (NpgsqlCommand command = new NpgsqlCommand("INSERT INTO quiz_questions (question_text, prompt_id) VALUES (@questionText, @promptID) RETURNING id", connection))
                {
                    // Get Question value from JSON
                    JsonElement question = property.Value.GetProperty("Question");

                    // Check if the question value is not null
                    if (question.ValueKind != JsonValueKind.Null)
                    {
                        string questionText = question.GetString() ?? string.Empty;
                        string question_text = questionText.Replace("'", "\"");
                        command.Parameters.AddWithValue("@questionText", NpgsqlTypes.NpgsqlDbType.Text, question_text);
                        command.Parameters.AddWithValue("@promptID", NpgsqlTypes.NpgsqlDbType.Integer, prompt_ID);
                        // Execute the command and retrieve the generated ID
                        object result = command.ExecuteScalar()!;
                        question_ID = (int)result;
                        Console.WriteLine($"Generated ID for this question: {question_ID}");
                        Console.WriteLine($"Question: {question_text}");
                    }
                }



                // Insert Options into options table
                using (NpgsqlCommand command = new NpgsqlCommand("INSERT INTO quiz_options (id, option_name, option_text) VALUES (@id, @optionName, @optionText)", connection))
                {
                    // Get the value of the "Options" property from the JSON
                    JsonElement options = property.Value.GetProperty("Options");
                    Console.WriteLine("Options:");

                    // Iterate through options and insert them into the options table
                    foreach (JsonProperty option in options.EnumerateObject())
                    {
                        string option_name = option.Name ?? "";
                        string option_text = option.Value.GetString()?.Replace("'", "\"") ?? "";
                        // Set the parameter values for the option ID, option name, and option text
                        command.Parameters.AddWithValue("@id", NpgsqlTypes.NpgsqlDbType.Integer, question_ID);
                        command.Parameters.AddWithValue("@optionName", NpgsqlTypes.NpgsqlDbType.Char, option_name);
                        command.Parameters.AddWithValue("@optionText", NpgsqlTypes.NpgsqlDbType.Text, option_text);
                        // Execute the command to insert the option
                        command.ExecuteNonQuery();
                        command.Parameters.Clear();

                        Console.WriteLine(option.Name + ": " + option.Value.GetString());
                    }
                }

                // Insert Answer into answers table
                using (NpgsqlCommand command = new NpgsqlCommand("INSERT INTO quiz_answers (id, answer_name) VALUES (@id, @answerName)", connection))
                {
                    // Get the value of the "Answer" property from the JSON
                    JsonElement answer = property.Value.GetProperty("Answer");

                    string answer_name = answer.GetString() ?? "";
                    // Set the parameter values for the answer ID and answer name
                    command.Parameters.AddWithValue("@id", NpgsqlTypes.NpgsqlDbType.Integer, question_ID);
                    command.Parameters.AddWithValue("@answerName", NpgsqlTypes.NpgsqlDbType.Char, answer_name);
                    // Execute the command to insert the answer
                    command.ExecuteNonQuery();
                    command.Parameters.Clear();

                    Console.WriteLine("Answer: " + answer.GetString());

                    Console.WriteLine();
                }
            }

            // Close the database connection
            connection.Close();
        }
    } // End AddQuiz()

    public static void CreateTablesIfNotExists(string connectionString)
    {

        using var con = new NpgsqlConnection(connectionString);
        con.Open();

        using var cmd = new NpgsqlCommand();
        cmd.Connection = con;

        cmd.CommandText = @"CREATE TABLE IF NOT EXISTS quiz_prompts (id SERIAL NOT NULL PRIMARY KEY, 
                prompt_text VARCHAR NOT NULL)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE TABLE IF NOT EXISTS quiz_questions (id SERIAL NOT NULL PRIMARY KEY, 
                question_text VARCHAR NOT NULL,
                prompt_id INT NOT NULL,
                question_visible BOOL,
                FOREIGN KEY (prompt_id)
                    REFERENCES quiz_prompts (id)
                    ON DELETE CASCADE
                    ON UPDATE CASCADE)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE TABLE IF NOT EXISTS  quiz_options (id INTEGER NOT NULL, 
                option_name CHAR NOT NULL,
                option_text VARCHAR NOT NULL,
                FOREIGN KEY (id)
                    REFERENCES quiz_questions (id)
                    ON DELETE CASCADE
                    ON UPDATE CASCADE)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE TABLE IF NOT EXISTS  quiz_answers (id INTEGER NOT NULL,
                answer_name CHAR NOT NULL,
                FOREIGN KEY (id)
                    REFERENCES quiz_questions (id)
                    ON DELETE CASCADE
                    ON UPDATE CASCADE)";
        cmd.ExecuteNonQuery();

        con.Close();

    }

}