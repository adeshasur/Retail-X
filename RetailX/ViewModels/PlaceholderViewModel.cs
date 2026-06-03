namespace RetailX.ViewModels;

public class PlaceholderViewModel : ObservableObject
{
    public PlaceholderViewModel(string title, string description)
    {
        Title = title;
        Description = description;
    }

    public string Title { get; }

    public string Description { get; }
}
