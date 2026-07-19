public class OpenScreenCommand : IInteractionCommand
{
    private readonly ScreenId _screenId;
    public OpenScreenCommand(ScreenId screenId)
        => _screenId = screenId;

    public void Execute()
    {
        if (ScreenManager.Exists) ScreenManager.Instance.Open(_screenId);
    }
}