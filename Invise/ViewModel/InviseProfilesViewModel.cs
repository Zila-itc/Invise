using System;
using System.IO;
using System.Linq;
using Invise.Model;
using System.Windows;
using System.Threading.Tasks;
using Invise.Services.Commands;
using Invise.Services.Settings;
using System.Collections.ObjectModel;

namespace Invise.ViewModel;
public class InviseProfilesViewModel : BaseViewModel
{
    #region Commands
    public RelayCommand CloseAppCommand { get; private set; }
    public RelayCommand CreateProfileCommand { get; private set; }
    public RelayCommand StartProfileCommand { get; private set; }
    public RelayCommand EditProfileCommand { get; private set; }
    public RelayCommand DeleteProfileCommand { get; private set; }
    public RelayCommand RefreshProfilesCommand { get; private set; }
    public RelayCommand ChangeWindowStateCommand { get; private set; }
    #endregion

    #region Properties
    private ObservableCollection<ProfileTab> _profileTabs;
    public ObservableCollection<ProfileTab> ProfileTabs
    {
        get => _profileTabs;
        set => Set(ref _profileTabs, value);
    }

    private WindowState _windowState;
    public WindowState WindowState
    {
        get => _windowState;
        set => Set(ref _windowState, value);
    }

    private InviseProfileSettingsViewModel _inviseProfileSettingsVM;
    public InviseProfileSettingsViewModel InviseProfileSettingsVM
    {
        get => _inviseProfileSettingsVM;
        set => Set(ref _inviseProfileSettingsVM, value);
    }

    private InviseBrowserViewModel _inviseBrowserViewModelVM;
    public InviseBrowserViewModel InviseBrowserViewModelVM
    {
        get => _inviseBrowserViewModelVM;
        set => Set(ref _inviseBrowserViewModelVM, value);
    }

    private Setting _setting;
    public Setting Setting
    {
        get => _setting;
        set => Set(ref _setting, value);
    }
    #endregion

    #region Ctor
    public InviseProfilesViewModel()
    {
        CreateProfileCommand = new RelayCommand(CreateProfile);
        ChangeWindowStateCommand = new RelayCommand(CloseWindowState);
        CloseAppCommand = new RelayCommand(CloseApp);
        StartProfileCommand = new RelayCommand(StartProfile);
        EditProfileCommand = new RelayCommand(EditProfile);
        DeleteProfileCommand = new RelayCommand(DeleteProfile);
        RefreshProfilesCommand = new RelayCommand(RefreshProfiles);
        ProfileTabs = new();
        Setting = new();
        Task.Run(() => { LoadTabs(); });
    }
    #endregion

    #region Profile Work
    private void StartProfile(object arg)
    {
        InviseBrowserViewModelVM = new(Setting.InviseProfiles.Where(x => x.Id == (int)arg).First());
        ViewManager.Show(InviseBrowserViewModelVM);
        Setting.SaveSettings();
    }
    private void CreateProfile(object arg)
    {
        Setting.InviseProfiles.Add(InviseProfile.GenerateNewProfile("Profile"));
        InviseProfileSettingsVM = new InviseProfileSettingsViewModel(Setting.InviseProfiles.Last());
        this.NextStep(InviseProfileSettingsVM);
        InviseProfileSettingsVM.InviseProfilesVM = this;
    }
    private void DeleteProfile(object arg)
    {
        var profilesToRemove = Setting.InviseProfiles.Where(profile => profile.Id == (int)arg).ToList();
        foreach (var profile in profilesToRemove)
        {
            if (Directory.Exists(profile.CachePath))
            {
                Directory.Delete(profile.CachePath, true);
            }
            Setting.InviseProfiles.Remove(profile);
        }

        var tabsToRemove = ProfileTabs.Where(item => item.Id == (int)arg).ToList();
        foreach (var item in tabsToRemove)
        {
            ProfileTabs.Remove(item);
        }

        Setting.SaveSettings();
    }
    private void RefreshProfiles(object arg)
    {
        ProfileTabs.Clear();
        LoadTabs();
    }
    private void EditProfile(object arg)
    {
        InviseProfileSettingsVM = new InviseProfileSettingsViewModel(
            Setting.InviseProfiles.Where(x => x.Id == (int)arg).First())
        {
            SaveProfileButtonContent = "Save"
        };
        this.NextStep(InviseProfileSettingsVM);
        InviseProfileSettingsVM.InviseProfilesVM = this;
    }
    #endregion

    #region Window Work & Actions
    public void LoadTabs()
    {
        foreach (var item in Setting.InviseProfiles)
        {

            ProfileTabs.Add(new ProfileTab(this)
            {
                Name = item.Name,
                Id = item.Id,
                Status = item.Status,
                Tags = item.Tags,
                //ProxyHostPort = item.Proxy.ProxyAddress + ":" + item.Proxy.ProxyPort,
                //ProxyLoginPass = item.Proxy.ProxyLogin + ":" + item.Proxy.ProxyPassword
                ProxyHostPort = item.Proxy.ProxyAddress == "" && item.Proxy.ProxyPort == 8080 ? "" : item.Proxy.ProxyAddress + ":" + item.Proxy.ProxyPort,
                ProxyLoginPass = item.Proxy.ProxyLogin == "" && item.Proxy.ProxyPassword == "" ? "" : item.Proxy.ProxyLogin + ":" + item.Proxy.ProxyPassword
            });
        }
    }
    private void CloseWindowState(object arg)
    {
        WindowState = WindowState.Minimized;
    }
    private void CloseApp(object arg)
    {
        Environment.Exit(0);
    }
    #endregion
}