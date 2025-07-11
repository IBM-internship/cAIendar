namespace AiCalendarAssistant.Utilities;

public static class TaskExtensions
{
    public static void SendAsyncFunc(Task task)
    {
        task.ContinueWith(t =>
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Unhandled exception: {t.Exception?.GetBaseException().Message}");
            Console.ResetColor();
        }, TaskContinuationOptions.OnlyOnFaulted);
    }
}