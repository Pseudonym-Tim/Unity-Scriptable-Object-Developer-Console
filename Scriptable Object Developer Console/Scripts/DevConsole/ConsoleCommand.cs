using System;
using System.Collections.Generic;
using UnityEngine;

// NOTE: Add new command types in this enum when you make a new command!
// Make sure that the name of the command exactly matches the name of the enum (not case sensitive)!
// This is recommend as direct commandName string comparison is unwise!

public enum CommandType
{
	HELP,
	CLEAR,
	TIMESCALE,
	GOD,
	QUIT,
}

[CreateAssetMenu(fileName = "ConsoleCommand", menuName = "Developer Console/ConsoleCommand")]
public class ConsoleCommand : ScriptableObject
{
	public string commandName = "NewCommand";
	[TextArea(1, 3)] public string helpMessage = "NewHelpMessage";
	[SerializeField] private List<Parameter> parameters;

	[Serializable]
	public struct Parameter
    {
		public string parameterName;
    }

	public void ExecuteCommand()
	{
		DeveloperConsole.LogCurrentInput();
		DeveloperConsole.onConsoleCommandSubmit(this);
	}

    private void OnValidate()
    {
		// Make sure the command name is lowercase...
		commandName = commandName.ToLower();
	}

    public bool CheckParamCountValid(string[] param)
	{
		if((ParamCount == 0 && param.Length > 0) || param.Length < ParamCount || param.Length > ParamCount)
		{
			return false;
		}

		return true;
	}

	public bool CheckParamTypesValid(string[] param)
	{
		if(param.Length <= 0) { return false; }

		DeveloperConsole.CurrentParameters.Clear();

		for(int i = 0; i < ParamCount; i++)
		{
			float newParam = 0f;

			if(!float.TryParse(param[i], out newParam))
			{
				DeveloperConsole.AddLogText(DeveloperConsole.Messages.IncorrectParamTypeMessage);
				return false;
			}

			DeveloperConsole.CurrentParameters.Add(newParam);
		}

		return true;
	}

	public int ParamCount { get { return parameters.Count; } }

	public List<Parameter> Parameters { get { return parameters; } }

	public CommandType CommandType
    {
        get 
		{
			CommandType commandType;
			bool enumIsDefined = Enum.TryParse(commandName, true, out commandType);
		
            if(!enumIsDefined)
            {
				Debug.Log($"CommandType enum is undefined for the \"{ commandName }\" command!");
			}

			return commandType;
		}
    }

	public void LogCommandInfo()
	{
		string logText = commandName + ": " + helpMessage;

		if(string.IsNullOrWhiteSpace(helpMessage))
		{
			Debug.Log(commandName + " does not have a help message!");
			return;
        }

		// Append syntax help if we take in a parameter...
		if(ParamCount > 0)
        {
			logText += $" (syntax: { commandName } [";

			for(int i = 0; i < ParamCount; i++)
			{
				logText += parameters[i].parameterName;

				if(i != ParamCount - 1) { logText += ", "; }
			}

			logText += "])";
		}

		DeveloperConsole.AddLogText(logText);
	}
}
