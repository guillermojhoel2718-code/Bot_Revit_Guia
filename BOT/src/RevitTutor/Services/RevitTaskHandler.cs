using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Autodesk.Revit.UI;

namespace RevitTutor
{
    public class RevitTaskHandler : IExternalEventHandler
    {
        private readonly ConcurrentQueue<Action<UIApplication>> _tasks = new ConcurrentQueue<Action<UIApplication>>();

        public void Execute(UIApplication app)
        {
            while (_tasks.TryDequeue(out var task))
            {
                try
                {
                    task(app);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }
            }
        }

        public string GetName() => "RevitTaskHandler";

        public Task<T> RunAsync<T>(ExternalEvent extEvent, Func<UIApplication, T> func)
        {
            var tcs = new TaskCompletionSource<T>();
            _tasks.Enqueue((app) =>
            {
                try
                {
                    T result = func(app);
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            extEvent.Raise();
            return tcs.Task;
        }
    }
}
