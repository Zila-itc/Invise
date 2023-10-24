using Invise.ViewModel;
using System.Windows.Input;

namespace Invise.View;

public partial class InviseProfilesView : IBaseView
{
    public BaseViewModel ViewModel { get; set; }

    public InviseProfilesView()
    {
        InitializeComponent();
    }

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        this.DragMove();
    }
}
