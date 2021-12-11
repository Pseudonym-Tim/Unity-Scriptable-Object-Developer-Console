using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using System;
using UnityEngine.UI;

public class DeveloperConsole : ConsoleSingleton<DeveloperConsole>
{ 
	private const int MAX_AUTOCOMPLETE_RESULTS = 4;

	[Header("General")]
	[SerializeField] private bool devModeEnabled = true;
	[SerializeField] private bool commandAutocompletion = true;
	[SerializeField] private bool submitScrollToEnd = true;
	[SerializeField] private bool autocompleteParameters = true;
	[SerializeField] private bool pauseTimeWhenActive = true;
	[SerializeField] private bool toggleCursor = true;
	[SerializeField] private KeyCode toggleKey = KeyCode.Tilde;
	[SerializeField] private KeyCode submitKey = KeyCode.Return;
	[SerializeField] private List<ConsoleCommand> consoleCommands = new List<ConsoleCommand>();

	[Header("UI")]
	[SerializeField] private Color logTextColor = Color.red;
	[SerializeField] private GameObject devConPanel;
	[SerializeField] private TextMeshProUGUI logText;
	[SerializeField] private TMP_InputField inputField;
	[SerializeField] private ScrollRect scrollRect;
	[SerializeField] private GameObject autoCompletePanel;
	[SerializeField] private GameObject textResultPrefab;

	private ConsoleCommand currentCommand;
	private string currentCommandName = null;
	private string[] curUnfixedParameters;
	private List<float> curParameters = new List<float>();
	private bool devConsoleActive = false;
	private RectTransform autoCompletePanelTransform;
	private List<string> autoCompleteResults = new List<string>();
	private string curAutoCompleteParameters = null;
	private float curTimescale = 0;

	public struct Messages
	{
		public static string IncorrectParamNumbersMessage = "Incorrect number of parameters!";
		public static string IncorrectParamTypeMessage = "Incorrect parameter type!";
		public static string UnknownCommandMessage = "Unknown command! Type \"help\" for help!";
		public static string CommandTypeEnumUndefinedMessage = "Command type enum undefined!";
		public static string ConsoleStartupMessage = "Developer console loaded! Type \"help\" for help!";
	}

	private void OnEnable()
    {
		onConsoleCommandSubmit -= OnConsoleCommandSubmitted;
		onConsoleCommandSubmit += OnConsoleCommandSubmitted;
	}

    private void OnDisable()
    {
		onConsoleCommandSubmit -= OnConsoleCommandSubmitted;
	}

    private void Awake()
    {
		devConPanel.SetActive(false);
		logText.color = logTextColor;
		autoCompletePanel.SetActive(false);
		autoCompletePanelTransform = autoCompletePanel.GetComponent<RectTransform>();

        if(commandAutocompletion)
        {
			inputField.onValueChanged.AddListener(OnInputValueChanged);
		}

		// Remove this if you already do this elsewhere!
        if(toggleCursor)
        {
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
        }
		
		AddLogText(Messages.ConsoleStartupMessage);
	}

    private void Update()
    {
        if(Input.GetKeyDown(toggleKey)) { ToggleConsole(); }

        if(Input.GetKeyDown(submitKey))
		{
			inputField.ActivateInputField();

			if(commandAutocompletion)
            {
				// Auto fill the first result into the input field and move to the end...
				if(autoCompleteResults.Count > 0 && autoCompletePanel.activeInHierarchy)
                {
					inputField.text = autoCompleteResults[0];

					// If the command entered has parameters...
					if(GetCommandByName(GetCommandNameFromInput(inputField.text)).ParamCount > 0)
					{
						// We don't want to autocomplete the parameter help, get rid of it...
						inputField.text = inputField.text.Replace(curAutoCompleteParameters, null).TrimEnd();

						// (Add a space to eliminate needing to do that to start inputting the first parameter)
						inputField.text += " ";
					}

					inputField.MoveToEndOfLine(false, false);
					return;
				}
			}

			SubmitCommand();
		}
    }

	private void OnConsoleCommandSubmitted(ConsoleCommand conCommand)
	{
        if(!CanDoCommandLogic(conCommand)) { return; }

		// (Developer console specific command logic should go here)
		switch(conCommand.CommandType)
        {
			case CommandType.CLEAR: logText.text = null; break;
			case CommandType.HELP: LogCommandList(); break;
			case CommandType.QUIT: QuitGameOrExitPlaymode(); break;
        }
	}

	private bool CanDoCommandLogic(ConsoleCommand conCommand)
    {
		if(!HasValidParameters(conCommand))
		{
			// Log parameter help message...
			if(!conCommand.CheckParamCountValid(CurrentUnfixedParameters))
			{
				LogParameterHelperMessage(conCommand);
			}

			// Log type mismatch message...
			if(conCommand.ParamCount > 0)
			{
				if(CurrentUnfixedParameters.Length > 0 && !conCommand.CheckParamTypesValid(CurrentUnfixedParameters))
                {
					AddLogText(Messages.IncorrectParamTypeMessage);
				}
			}

			return false;
		}

		return true;
	}

	public static void LogCurrentInput()
    {
		AddLogText("> " + Instance.CurrentInput);
	}

	public void ToggleConsole()
    {
		if(!devModeEnabled) { return; }

		devConsoleActive = !devConsoleActive;
		devConPanel.SetActive(devConsoleActive);

		if(Time.timeScale != 0) { curTimescale = Time.timeScale; }

		if(pauseTimeWhenActive)
        {
			Time.timeScale = devConsoleActive ? 0 : curTimescale;
		}

        if(toggleCursor)
        {
			Cursor.visible = devConsoleActive;
			Cursor.lockState = devConsoleActive ? CursorLockMode.None : CursorLockMode.Locked;
		}
		
		UpdateInputField();
	}

	private void LogParameterHelperMessage(ConsoleCommand conCommand)
    {
		string incorrectParamNumMessage = Messages.IncorrectParamNumbersMessage;

		if(conCommand.ParamCount == 0)
		{
			incorrectParamNumMessage += $"\n{ conCommand.commandName } takes no parameters!";
		}
		else
		{
			string plural = conCommand.ParamCount > 1 ? "s" : "";
			incorrectParamNumMessage += $"\n{ conCommand.commandName } takes { conCommand.ParamCount } parameter{ plural }!";
		}

		AddLogText(incorrectParamNumMessage);
	}

	private void OnInputValueChanged(string inputText)
	{
		inputText = inputText.ToLower();

		autoCompleteResults.Clear();

		autoCompleteResults.AddRange(GetAutocompleteResults(inputText));

		autoCompletePanel.SetActive(!ShouldHideAutocompletePanel(inputText));

		DestroyAutoCompleteResults();
		FillAutoCompleteResults(autoCompleteResults);
	}

	private bool ShouldHideAutocompletePanel(string inputText)
    {
		bool inputIsEmpty = string.IsNullOrWhiteSpace(inputText);
		bool curParametersEmpty = string.IsNullOrWhiteSpace(curAutoCompleteParameters);
		bool gotResults = autoCompleteResults.Count > 0;
		bool matchedCommandName = false;

		if(!curParametersEmpty && !inputIsEmpty && gotResults)
		{
			matchedCommandName = inputText == autoCompleteResults[0].Replace(curAutoCompleteParameters, null);
		}

		return (inputIsEmpty || !gotResults || matchedCommandName);
	}

	private void DestroyAutoCompleteResults()
	{
		// Reverse loop since we're destroying children
		for(int childIndex = autoCompletePanelTransform.childCount - 1; childIndex >= 0; --childIndex)
		{
			Transform child = autoCompletePanelTransform.GetChild(childIndex);
			child.SetParent(null);
			Destroy(child.gameObject);
		}
	}

	private void FillAutoCompleteResults(List<string> results)
	{
		for(int resultIndex = 0; resultIndex < results.Count; resultIndex++)
		{
			// Prevent displaying more results than this...
			if(resultIndex == MAX_AUTOCOMPLETE_RESULTS) { break; }

			RectTransform childRect = Instantiate(textResultPrefab).GetComponent<RectTransform>();
			TextMeshProUGUI childText = childRect.GetComponentInChildren<TextMeshProUGUI>();

			// (Prefix autocompletion submit hint/indicator)
			if(resultIndex == 0) { childText.text += "> "; }

			childText.text += results[resultIndex];

			childRect.SetParent(autoCompletePanelTransform);
			childRect.transform.localScale = Vector3Int.RoundToInt(childRect.transform.localScale);
		}
	}

	private List<string> GetAutocompleteResults(string input)
	{
		List<string> results = new List<string>();

		foreach(ConsoleCommand consoleCommand in consoleCommands)
		{
			string result = consoleCommand.commandName;

            if(commandAutocompletion && autocompleteParameters)
            {
				// Append parameters to result if it takes any...
				if(consoleCommand.ParamCount > 0)
				{
					result += $" [";

					for(int i = 0; i < consoleCommand.ParamCount; i++)
					{
						result += consoleCommand.Parameters[i].parameterName;

						if(i != consoleCommand.ParamCount - 1) { result += ", "; }
					}

					result += "]";

					// Store these parameters so we can use them later
					curAutoCompleteParameters = result.Replace(consoleCommand.commandName, null).Trim();
				}
			}
			
			results.Add(result);
		}

		return results.FindAll((str) => str.IndexOf(input.Trim()) >= 0);
	}

	private void UpdateInputField()
    {
		if(devConsoleActive)
		{
			EventSystem.current.SetSelectedGameObject(inputField.gameObject);
			inputField.ActivateInputField();
			return;
		}

		// Make sure we remove the toggle key result in the input field
		// if the toggle key even inputs anything this frame...
		if(Input.inputString != null)
        {
			if(!string.IsNullOrWhiteSpace(CurrentInput))
            {
				inputField.text = CurrentInput.Replace(Input.inputString, null);
			}
		}

		inputField.DeactivateInputField(false);
	}

	public void SubmitCommand()
    {
        if(!string.IsNullOrWhiteSpace(CurrentInput))
        {
			CommandSubmitted(CurrentInput);
		}
    }

	public static bool HasValidParameters(ConsoleCommand command)
	{
		bool parameterCountIsZero = command.ParamCount == 0;
		bool hasCorrectParameterCount = command.CheckParamCountValid(CurrentUnfixedParameters);

        if(!hasCorrectParameterCount) { return false; }

		bool hasCorrectParameterType = command.CheckParamTypesValid(CurrentUnfixedParameters);

		return parameterCountIsZero || (hasCorrectParameterCount && hasCorrectParameterType);
	}

	private void CommandSubmitted(string command)
    {
		currentCommandName = GetCommandNameFromInput(command.Trim());
		currentCommand = GetCommandByName(currentCommandName);
		curUnfixedParameters = GetUnfixedCommandParamsFromInput(command.Trim());

		if(currentCommand == null) { return; }

		currentCommand.ExecuteCommand();

		ResetInputFieldAndScroll();
	}

	private void StartScrollSnapToEndOfLog()
    {
        if(!submitScrollToEnd) { return; }

		StopAllCoroutines();
		StartCoroutine(ScrollSnapToEndOfLog());
	}

	private IEnumerator ScrollSnapToEndOfLog()
	{
		yield return new WaitForEndOfFrame();
		scrollRect.verticalNormalizedPosition = 0;
	}

	private void ResetInputFieldAndScroll()
    {
		inputField.text = null;
		inputField.ActivateInputField();
		StartScrollSnapToEndOfLog();
	}

	public static void AddLogText(string textToLog)
	{
		Instance.AppendLine(textToLog);
	}

	public static string[] CurrentUnfixedParameters
	{
		get { return Instance.curUnfixedParameters; }
	}

	public static List<float> CurrentParameters
	{
		get { return Instance.curParameters; }
	}

	private void AppendLine(string line)
	{
		TextMeshProUGUI debugText = logText;
		debugText.text = debugText.text + line + "\n";
	}

	private string GetCommandNameFromInput(string input)
	{
		return input.Split(new char[] { ' ' })[0].ToLower();
	}

	private string[] GetUnfixedCommandParamsFromInput(string input)
	{
		List<string> unfixedParams = new List<string>(input.Split(new char[] { ' ' }));
		unfixedParams.RemoveAt(0);

		return unfixedParams.ToArray();
	}

	private ConsoleCommand GetCommandByName(string commandName)
	{
		foreach(ConsoleCommand debugCommand in consoleCommands)
		{
			if(commandName == debugCommand.commandName)
			{
				return debugCommand;
			}
		}

		LogCurrentInput();
		AddLogText(Messages.UnknownCommandMessage);
		ResetInputFieldAndScroll();

		return null;
	}

	private void LogCommandList()
	{
		foreach(ConsoleCommand conCommand in consoleCommands)
		{
			// Generically check for the help command by converting the CommandType to a string
			// so we don't need to define a CommandType to get commands to log their info...
			bool isHelpCommandGeneric = conCommand.commandName == CommandType.HELP.ToString().ToLower();

			// (Don't log the help command for obvious reasons...)
			if(!isHelpCommandGeneric)
			{
				conCommand.LogCommandInfo();
			}
		}
	}

	private void QuitGameOrExitPlaymode()
    {
		#if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;
		#else
			 Application.Quit();
		#endif
	}

	public static bool DevModeEnabled { get { return Instance.devModeEnabled; } }

	public static Action<ConsoleCommand> onConsoleCommandSubmit { get; set; }

	public string CurrentInput { get { return inputField.text; } }
}
