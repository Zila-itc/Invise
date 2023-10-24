using Invise.ViewModel;

namespace Invise.View;
public interface IBaseView
{
    BaseViewModel ViewModel { get; set; }

    void Close();
}
