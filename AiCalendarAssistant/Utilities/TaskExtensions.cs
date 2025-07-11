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
    
    public static void SendAsyncFunc(Func<Task> taskFactory, IServiceScopeFactory serviceScopeFactory)
    {
        Task.Run(async () =>
        {
            using var scope = serviceScopeFactory.CreateScope();
            await taskFactory();
        }).ContinueWith(t =>
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Unhandled exception: {t.Exception?.GetBaseException().Message}");
            Console.ResetColor();
        }, TaskContinuationOptions.OnlyOnFaulted);
    }
}