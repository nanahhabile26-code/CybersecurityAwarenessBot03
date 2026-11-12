using System.Collections.Generic;

namespace CybersecurityAwarenessBot;

/// <summary>What the chatbot remembers about the user during the conversation (Part 2 memory).</summary>
public class UserMemory
{
    /// <summary>Defaults to "friend" until the user says "my name is ...".</summary>
    public string Name { get; set; } = "friend";

    public string FavouriteTopic { get; set; } = "";

    public string LastSentiment { get; set; } = "";

    /// <summary>Topics already covered, so the bot can recall them later.</summary>
    public List<string> TopicsDiscussed { get; } = new();
}