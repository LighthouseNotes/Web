// ReSharper disable UnusedMember.Global

namespace Web.Services;

public class SpinnerService
{
    public event Action? LoadCompleted;

    // Mark the page load as completed
    public void LoadComplete()
    {
        LoadCompleted?.Invoke();
    }
}