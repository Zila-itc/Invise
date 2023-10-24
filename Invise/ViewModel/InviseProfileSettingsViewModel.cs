using Invise.Model;
using System.Windows;
using Invise.Core.Web;
using System.Windows.Media;
using System.Threading.Tasks;
using Invise.Services.Commands;

namespace Invise.ViewModel;
public class InviseProfileSettingsViewModel : BaseViewModel
{
    #region Commands
    public RelayCommand CloseProfileSettingsCommand { get; private set; }
    public RelayCommand ChangeWindowStateCommand { get; private set; }
    public RelayCommand CheckProxyCommand { get; private set; }
    public RelayCommand SaveProfileCommand { get; private set; }
    #endregion

    #region Properties
    private WindowState _windowState;
    public WindowState WindowState
    {
        get => _windowState;
        set => Set(ref _windowState, value);
    }

    private InviseProfilesViewModel _inviseProfilesVM;
    public InviseProfilesViewModel InviseProfilesVM
    {
        get => _inviseProfilesVM;
        set => Set(ref _inviseProfilesVM, value);
    }


    private string _saveProfileButtonContent = "Create";
    public string SaveProfileButtonContent
    {
        get => _saveProfileButtonContent;
        set => Set(ref _saveProfileButtonContent, value);
    }

    private Brush _tbProxyBrush = Brushes.White;
    public Brush TbProxyBrush
    {
        get => _tbProxyBrush;
        set => Set(ref _tbProxyBrush, value);
    }

    private InviseProfile _inviseProf;
    public InviseProfile InviseProf
    {
        get => _inviseProf;
        set => Set(ref _inviseProf, value);
    }
    #endregion

    #region Ctor
    public InviseProfileSettingsViewModel() { }
    public InviseProfileSettingsViewModel(InviseProfile inviseProfile)
    {
        CloseProfileSettingsCommand = new RelayCommand(CloseProfileSettings);
        SaveProfileCommand = new RelayCommand(SaveProfile);
        ChangeWindowStateCommand = new RelayCommand(CloseWindowState);
        CheckProxyCommand = new RelayCommand(CheckProxy);
        InviseProf = inviseProfile;
    }
    #endregion

    #region Window Work & Actions
    private void SaveProfile(object arg)
    {
        if (SaveProfileButtonContent == "Create")
        {
            ViewManager.Close(this);
            InviseProfilesVM.ProfileTabs.Add(new ProfileTab(InviseProfilesVM)
            {
                Name = InviseProf.Name,
                Id = InviseProf.Id,
                Status = InviseProf.Status,
                Tags = InviseProf.Tags,
                ProxyHostPort = InviseProf.Proxy.ProxyAddress == "" && InviseProf.Proxy.ProxyPort == 8080 ? "" : InviseProf.Proxy.ProxyAddress + ":" + InviseProf.Proxy.ProxyPort,
                ProxyLoginPass = InviseProf.Proxy.ProxyLogin == "" && InviseProf.Proxy.ProxyPassword == "" ? "" : InviseProf.Proxy.ProxyLogin + ":" + InviseProf.Proxy.ProxyPassword
            });
            InviseProfilesVM.Setting.SaveSettings();
        }
        else
        {
            ViewManager.Close(this);
            InviseProfilesVM.Setting.SaveSettings();
            InviseProfilesVM.ProfileTabs.Clear();
            InviseProfilesVM.LoadTabs();
        }
    }
    private async void CheckProxy()
    {
        if (InviseProf.Proxy.ProxyAddress == "") return;
        var result = await IpInfoClient.CheckClientProxy(InviseProf.Proxy);
        if (result == null)
        {
            TbProxyBrush = Brushes.Red;
            await Task.Delay(2000);
            TbProxyBrush = Brushes.White;
            return;
        }
        if (result.Ip == InviseProf.Proxy.ProxyAddress)
        {
            TbProxyBrush = Brushes.Green;
            await Task.Delay(2000);
            TbProxyBrush = Brushes.White;
        }
    }
    private void CloseProfileSettings(object arg)
    {
        ViewManager.Close(this);
    }
    private void CloseWindowState(object arg)
    {
        WindowState = WindowState.Minimized;
    }
    #endregion
}