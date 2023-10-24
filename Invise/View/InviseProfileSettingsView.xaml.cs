using Invise.ViewModel;

namespace Invise.View;
public partial class InviseProfileSettingsView : IBaseView
{
    public BaseViewModel ViewModel { get; set; }
    public InviseProfileSettingsView()
    {
        InitializeComponent();
    }

    private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        this.DragMove();
    }
}
