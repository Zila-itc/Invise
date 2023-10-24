using Invise.ViewModel;
using System.Windows.Input;

namespace Invise.View;
public partial class InviseBrowserView : IBaseView
{
    public BaseViewModel ViewModel { get; set; }

    public InviseBrowserView()
    {
        InitializeComponent();
    }
    private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { DragMove(); }
}
