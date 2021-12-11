using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// An example of how you can make a console command do stuff when they are executed
/// </summary>
public class UsageExample : MonoBehaviour, IConsoleCommand // (Add and implement the IConsoleCommand interface to quickly setup the required functions)
{
    private bool godmodeEnabled = false;

    public void OnEnable()
    {
        // Make sure this action isn't subscribed to already so we don't get duplicate calls
        // if we toggled the gameobject this script is attached to on and off for example...
        DeveloperConsole.onConsoleCommandSubmit -= OnConsoleCommandExecuted;

        // Subscribe this function to the onConsoleCommandSubmit action...
        DeveloperConsole.onConsoleCommandSubmit += OnConsoleCommandExecuted;
    }

    public void OnDisable()
    {
        // Get rid of our subscription to it, we don't care anymore...
        DeveloperConsole.onConsoleCommandSubmit -= OnConsoleCommandExecuted;
    }

    public void OnConsoleCommandExecuted(ConsoleCommand conCommand)
    {
        // Make sure you always include this line at the start of the function before doing anything else!
        if(!DeveloperConsole.HasValidParameters(conCommand)) { return; }

        // You can use a switch case if you want to do logic for multiple commands in one script...
        // If it's just a singular command just directly compare CommandType using an if statement...
        switch(conCommand.CommandType)
        {
            case CommandType.TIMESCALE:

                // Grab the first parameter, and set it to be our timescale, we don't need to store any float variable in this case...
                Time.timeScale = DeveloperConsole.CurrentParameters[0];
                DeveloperConsole.AddLogText($"Timescale set to: { Time.timeScale }"); // Log your own confirmation message to the console

            break;

            case CommandType.GOD:

                // For commands that you need to store or make a variable for, you can just set it up like this, with the bool for it inside the script you do the logic in.
                // If you want it to not clutter your scripts and be more easily readable for doing extra logic I'd recommend just
                // adding a public static getter and setter that returns the type you want in the ConsoleCommand or DeveloperConsole script...
                godmodeEnabled = !godmodeEnabled;

                DeveloperConsole.AddLogText($"Godmode enabled: { godmodeEnabled }");

            break;
        }
    }
}
