// ReSharper disable UnusedMember.Global

namespace Web.Services;

public class SpinnerService
{
    public event Action? LoadCompleted;

    public void LoadComplete()
    {
        LoadCompleted?.Invoke();
    }
}