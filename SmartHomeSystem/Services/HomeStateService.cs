// Services/HomeStateService.cs
public class HomeStateService
{
    private bool _isHome = true;  // Default to home
    
    public bool IsHome 
    { 
        get => _isHome;
        set => _isHome = value;
    }
}