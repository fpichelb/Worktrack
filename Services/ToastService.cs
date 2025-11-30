using System;

namespace Worktrack.Services
{
    public enum ToastType
    {
        Success,
        Info,
        Warning,
        Error
    }

    public class ToastMessage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public ToastType Type { get; set; } = ToastType.Info;
        public int DurationMs { get; set; } = 4000;
    }

    public class ToastService
    {
        public event Action<ToastMessage>? OnShow;
        public event Action<Guid>? OnDismiss;

        public void ShowSuccess(string title, string message, int durationMs = 4000) =>
            Show(title, message, ToastType.Success, durationMs);

        public void ShowInfo(string title, string message, int durationMs = 4000) =>
            Show(title, message, ToastType.Info, durationMs);

        public void ShowWarning(string title, string message, int durationMs = 4000) =>
            Show(title, message, ToastType.Warning, durationMs);

        public void ShowError(string title, string message, int durationMs = 5000) =>
            Show(title, message, ToastType.Error, durationMs);

        public void Dismiss(Guid id) => OnDismiss?.Invoke(id);

        private void Show(string title, string message, ToastType type, int durationMs)
        {  
            OnShow?.Invoke(new ToastMessage
            {
                Title = title,
                Message = message,
                Type = type,
                DurationMs = durationMs
            });
        }
    }
}
