using System.Collections.Generic;

namespace Hibzz.ReflectionToolkit
{
    /// <summary>
    /// Used to process commands passed as string
    /// </summary>
    public class Command
    {
        // The primary command
        public string Primary { get; protected set; } = null;

        // a key value pair representing the parameters (context + values pair)
        public Dictionary<string, string> Parameters { get; protected set; } = new Dictionary<string, string>();

        public Command(string text)
        {
            // tokens represents individual words seperated by a space
            var tokens = text.Split(" ");

            // the first token is primary command
            if(tokens.Length <= 0) { return; }
            Primary = tokens[0];

            // loop through all other tokens and populate the parameters
            for(int i = 1; i < tokens.Length; i++)
            {
                // if it starts with a dash, then it's a key and the next token is the value
                if (tokens[i].StartsWith("-"))
                {
                    // make sure that a next token is available
                    if (i + 1 >= tokens.Length) { continue; }

                    Parameters[tokens[i]] = tokens[i + 1];
                    i++;
                }
            }
        }
    }
}
