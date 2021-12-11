public interface IConsoleCommand
{
    void OnEnable();

    void OnDisable();

    void OnConsoleCommandExecuted(ConsoleCommand conCommand);
}
