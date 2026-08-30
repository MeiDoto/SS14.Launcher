namespace SS14.Launcher.ViewModels.MainWindowTabs;

public abstract class MainWindowTabViewModel : ViewModelBase
{
    public abstract string Name { get; }

    public void NotifyNameChanged() => OnPropertyChanged(nameof(Name));

    public bool IsSelected { get; set; }

    public virtual void Selected()
    {
    }

    public virtual void Unselected()
    {
    }
}
