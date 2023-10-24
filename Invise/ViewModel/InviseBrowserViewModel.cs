using System;
using CefSharp;
using System.IO;
using System.Linq;
using Invise.Model;
using System.Windows;
using Invise.Core.Web;
using System.Text.Json;
using System.Globalization;
using System.Windows.Input;
using Invise.Core.ChromeApi;
using CefSharp.ModelBinding;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows.Controls;
using Invise.Services.Commands;
using Invise.Services.UI.Button;
using System.Collections.Generic;
using Invise.Core.ChromeApi.Proxy;
using System.Collections.ObjectModel;
using Invise.Core.ChromeApi.Handlers;
using Invise.Services.UI.ListView.ListViewItem;

namespace Invise.ViewModel;
public class InviseBrowserViewModel : BaseViewModel
{
    #region Fields
    private int _mainIDCounter;
    private DelegateCommand _closeCommand;
    private Label _tabBtnToDrag;
    private readonly string _profileHistoryPath;
    private ListView _listView;

    #region BrowserSettings & Other
    private List<InviseBrowser> _browsers;
    private InviseProfile _inviseProfileToStart;
    private InviseProfile _inviseProfile;
    private IpInfoResult _proxyInfo;
    private BlockManager _blockManager;
    private NativeSourceManager _nativeManager;
    private RequestHandler _requestHandler;
    private RequestContextSettings _requestContextSettings;
    private RequestContext _context;
    private LifespanHandler _lifespanHandler;
    private RenderMessageHandler _renderMessageHandler;
    private LoadHandler _loadHandler;
    private JsWorker _jsWorker;
    #endregion
    #endregion

    #region Commands
    public RelayCommand MinimizeWindowCommand { get; private set; }
    public RelayCommand MaximizeWindowCommand { get; private set; }
    public RelayCommand NormalStateWindowCommand { get; private set; }
    public RelayCommand AddTabCommand { get; private set; }
    public RelayCommand OpenTabCommand { get; private set; }
    public RelayCommand CloseTabCommand { get; private set; }
    public RelayCommand RefreshCommand { get; private set; }
    public RelayCommand ForwardCommand { get; private set; }
    public RelayCommand BackCommand { get; private set; }
    public RelayCommand OpenHistoryCommand { get; private set; }
    public RelayCommand LoadHistoryLinkCommand { get; private set; }
    public RelayCommand AddressOnKeyDownCommand { get; private set; }
    public RelayCommand OpenContextMenuSettingsCommand { get; private set; }
    public DelegateCommand CloseCommand =>
          _closeCommand ?? (_closeCommand = new DelegateCommand(obj => CloseWindow(obj)));
    #endregion

    #region Properties
    private TabItem _currentTabItem;
    public TabItem CurrentTabItem
    {
        get => _currentTabItem;
        set => Set(ref _currentTabItem, value);
    }

    private ObservableCollection<InviseHistoryItem> _inviseHistoryList;
    public ObservableCollection<InviseHistoryItem> InviseHistoryList
    {
        get => _inviseHistoryList;
        set => Set(ref _inviseHistoryList, value);
    }

    private string _address;
    public string Address
    {
        get => _address;
        set
        {
            if (_address != value)
            {
                _address = value;
                OnPropertyChanged(nameof(Address));
            }
        }
    }

    private ObservableCollection<TabItem> tabs;
    public ObservableCollection<TabItem> Tabs
    {
        get => tabs;
        set => Set(ref tabs, value);
    }

    private ObservableCollection<UIElement> _tabBtnsAndAddTabBtn;
    public ObservableCollection<UIElement> TabBtnsAndAddTabBtn
    {
        get => _tabBtnsAndAddTabBtn;
        set => Set(ref _tabBtnsAndAddTabBtn, value);
    }

    public Action Close { get; set; }

    private WindowState _curWindowState;
    public WindowState CurWindowState
    {
        get => _curWindowState;
        set => Set(ref _curWindowState, value);
    }


    #endregion

    #region Ctor
    public InviseBrowserViewModel() { }
    public InviseBrowserViewModel(InviseProfile inviseProfileToStart)
    {
        _inviseProfileToStart = inviseProfileToStart;
        _mainIDCounter = 0;
        Tabs = new();
        _browsers = new();
        _listView = new();
        InviseHistoryList = new();
        _profileHistoryPath = _inviseProfileToStart.CachePath + "\\History.json";

        MinimizeWindowCommand = new RelayCommand(MinimizedWindow);
        MaximizeWindowCommand = new RelayCommand(MaximizedWindow);
        NormalStateWindowCommand = new RelayCommand(NormalStateWindow);
        AddTabCommand = new RelayCommand(AddTab);
        OpenTabCommand = new RelayCommand(OpenTab);
        CloseTabCommand = new RelayCommand(CloseTab);
        RefreshCommand = new RelayCommand(Refresh);
        ForwardCommand = new RelayCommand(Forward);
        BackCommand = new RelayCommand(Back);
        AddressOnKeyDownCommand = new RelayCommand(AddressOnKeyDown);
        LoadHistoryLinkCommand = new RelayCommand(LoadHistoryLink);
        OpenHistoryCommand = new RelayCommand(AddTabHistory);
        OpenContextMenuSettingsCommand = new RelayCommand(OpenContextMenuSettings);

        try
        {
            ChromiumInit.Init(inviseProfileToStart);
        }
        catch (Exception ex)
        {
            File.WriteAllText("fail.txt", ex.Message);
            throw;
        }

        LoadHistoryJson();
        TabBtnsAndAddTabBtn = new() { InitAddTabBtn.CreateBtn(AddTab) };
        AddTab();
    }
    #endregion

    #region InviseBrowser Work
    private async Task<InviseBrowser> InitBrowser(bool isNewPage)
    {
        InviseBrowser browser = await CreateBrowser(isNewPage, _mainIDCounter, _inviseProfileToStart);
        _browsers.Add(browser);
        return browser;
    }
    private async Task<InviseBrowser> CreateBrowser(bool isNewPage, object id, InviseProfile inviseProfile)
    {
        if (!isNewPage)
        {
            _inviseProfile = inviseProfile;
            _blockManager = new BlockManager();
            _nativeManager = new NativeSourceManager();
            _blockManager.IsWork = _inviseProfile.IsAdBlock;
            _requestHandler = new RequestHandler(_blockManager);
            _requestContextSettings = new RequestContextSettings();
            _lifespanHandler = new LifespanHandler();

            if (_inviseProfile.IsLoadCacheInMemory)
            {
                _requestContextSettings.CachePath = _inviseProfile.CachePath;
                _requestContextSettings.PersistSessionCookies = true;
            }
            else
            {
                DateTime now = DateTime.Now;
                Random random = new((int)now.TimeOfDay.TotalMilliseconds);
                string tempPath = Path.GetTempPath();
                string path2 = "Invise" + random.Next();
                string normalStringUpper = _inviseProfile.Name;
                now = DateTime.Now;
                string str1 = new Random((int)now.TimeOfDay.TotalMilliseconds + random.Next()).Next().ToString();
                string path3 = normalStringUpper + str1;
                string str2 = Path.Combine(tempPath, path2, path3, "cache");
                _requestContextSettings.CachePath = str2;
                _inviseProfile.CachePath = Path.Combine(tempPath, path2);
                _requestContextSettings.PersistSessionCookies = false;
                _requestContextSettings.PersistUserPreferences = false;
            }

            _context = new RequestContext(_requestContextSettings);
            _context.DisableWebRtc();

            _jsWorker = new(_context);

            if (_inviseProfile.Proxy.IsCustomProxy)
            {
                var chromeProxy = _inviseProfile.Proxy.ToChromeProxy();
                _context.SetProxy(chromeProxy);

                if (_inviseProfile.Proxy.IsProxyAuth)
                {
                    _proxyInfo = await IpInfoClient.CheckClientProxy(_inviseProfile.Proxy);
                    _requestHandler.SetAuthCredentials(new ProxyAuthCredentials()
                    {
                        Login = _inviseProfile.Proxy.ProxyLogin,
                        Password = _inviseProfile.Proxy.ProxyPassword
                    });
                }
            }

            return InitBasicSettingsBrowser(isNewPage, id, inviseProfile);
        }
        else { return InitBasicSettingsBrowser(isNewPage, id, inviseProfile); }
    }
    private InviseBrowser InitBasicSettingsBrowser(bool isNewPage, object id, InviseProfile inviseProfile)
    {
        var inviseBrowser = new InviseBrowser(_context);
        inviseBrowser.LifeSpanHandler = _lifespanHandler;
        inviseBrowser.IsBrowserInitializedChanged += InviseBrowser_IsBrowserInitializedChanged;
        inviseBrowser.BrowserSettings.ImageLoading = inviseProfile.IsLoadImage ? CefState.Enabled : CefState.Disabled;
        inviseBrowser.BrowserSettings.RemoteFonts = CefState.Enabled;
        inviseBrowser.BrowserSettings.JavascriptCloseWindows = CefState.Disabled;
        if (isNewPage)
        {
            var codeForFakeProfile = _nativeManager.GetCodeForFakeProfile("fakeinject", inviseProfile.FakeProfile);
            _renderMessageHandler = new RenderMessageHandler(codeForFakeProfile);
            inviseBrowser.RenderProcessMessageHandler = _renderMessageHandler;
            _loadHandler = new LoadHandler("777", codeForFakeProfile, () => { ProfileFail(); });
            inviseBrowser.LoadHandler = _loadHandler;
        }
        else
        {
            inviseBrowser.RenderProcessMessageHandler = _renderMessageHandler;
            inviseBrowser.LoadHandler = _loadHandler;
        }
        inviseBrowser.RequestHandler = _requestHandler;
        inviseBrowser.JavascriptObjectRepository.Settings.JavascriptBindingApiEnabled = false;
        inviseBrowser.JavascriptObjectRepository.Settings.LegacyBindingEnabled = true;
        inviseBrowser.JavascriptObjectRepository.NameConverter = new MyCamelCaseNameConverter();
        inviseBrowser.JavascriptObjectRepository.Register(
            "worker",
            _jsWorker,
            options: new BindingOptions()
            {
                Binder = new DefaultBinder(new MyCamelCaseNameConverter())
            });
        ExecGeoScript(inviseBrowser);

        inviseBrowser.Tag = id;
        inviseBrowser.Address = "https://google.com/";
        return inviseBrowser;
    }
    private void ExecGeoScript(InviseBrowser inviseBrowser)
    {
        double latitude;
        double longitude;
        if (_inviseProfile.FakeProfile.GeoSettings.Latitude == null
            || _inviseProfile.FakeProfile.GeoSettings.Longitude == null)
            return;

        if (_inviseProfile.FakeProfile.GeoSettings.Status == Core.ChromeApi.Model.Configs.AutoManualEnum.AUTO)
        {
            if (!_inviseProfile.Proxy.IsProxyAuth)
                return;
            latitude = double.Parse(_proxyInfo.Loc.Split(',')[0], CultureInfo.InvariantCulture);
            longitude = double.Parse(_proxyInfo.Loc.Split(',')[1], CultureInfo.InvariantCulture);
        }
        else
        {
            latitude = double.Parse(_inviseProfile.FakeProfile.GeoSettings.Latitude, CultureInfo.InvariantCulture);
            longitude = double.Parse(_inviseProfile.FakeProfile.GeoSettings.Longitude, CultureInfo.InvariantCulture);
        }

        string script = $@"
            navigator.permissions.query = options => {{
              return Promise.resolve({{
                state: 'granted'
              }});
            }};
            navigator.geolocation.getCurrentPosition = (success, error, options) => {{
              success({{
                coords: {{
                  latitude: {latitude.ToString("0.000000", CultureInfo.InvariantCulture)},
                  longitude: {longitude.ToString("0.000000", CultureInfo.InvariantCulture)},
                  accuracy: 10,
                  altitude: null,
                  altitudeAccuracy: null,
                  heading: null,
                  speed: null
                }},
                timestamp: Date.now()
              }});
            }};
            navigator.geolocation.watchPosition = (success, error, options) => {{
              success({{
                coords: {{
                  latitude: {latitude.ToString("0.000000", CultureInfo.InvariantCulture)},
                  longitude: {longitude.ToString("0.000000", CultureInfo.InvariantCulture)},
                  accuracy: 49,
                  altitude: null,
                  altitudeAccuracy: null,
                  heading: null,
                  speed: null
                }},
                timestamp: Date.now()
              }});
              return 0;
            }};
            ";

        inviseBrowser.ExecuteScriptAsyncWhenPageLoaded(script, oneTime: false);
    }
    private void ProfileFail() { }
    private async void InviseBrowser_IsBrowserInitializedChanged(
        object sender,
        DependencyPropertyChangedEventArgs e)
    {
        if (!(bool)e.NewValue)
        {
            return;
        }

        var browser = (InviseBrowser)sender;

        using (var client = browser.GetDevToolsClient())
        {
            var canEmu = await client.Emulation.CanEmulateAsync();
            if (canEmu.Result)
            {

                //await client.Emulation.SetDeviceMetricsOverrideAsync(_inviseProfile.FakeProfile.ScreenSize.Width, _inviseProfile.FakeProfile.ScreenSize.Height, 1, false);
                await client.Emulation.SetUserAgentOverrideAsync(_inviseProfile.FakeProfile.UserAgent);
                await client.Emulation.SetLocaleOverrideAsync(_inviseProfile.FakeProfile.ChromeLanguageInfo.Locale);
                if (_inviseProfile.Proxy.IsProxyAuth)
                {
                    if (_proxyInfo == null)
                    {
                        MessageBox.Show("PROXY DONT WORK!");
                    }

                    await client.Emulation.SetTimezoneOverrideAsync(_proxyInfo.Timezone);
                }
            }
        }
    }

    private void Browser_TitleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        foreach (var item in TabBtnsAndAddTabBtn)
        {
            if ((item as Label) != null)
            {
                if ((item as Label).Tag.ToString() == (sender as InviseBrowser).Tag.ToString())
                {
                    (item as Label).Content = e.NewValue;
                    SaveHistoryJson((sender as InviseBrowser).Address, e.NewValue.ToString());
                    break;
                }
            }
        }
    }

    private void Browser_AddressChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        Address = e.NewValue.ToString();
    }

    private void Browser_LoadingStateChanged(object? sender, LoadingStateChangedEventArgs e)
    {
    }
    #endregion

    #region HistoryWork
    private void SaveHistoryJson(string address, string desc)
    {
        if (!File.Exists(_profileHistoryPath)) { File.Create(_profileHistoryPath).Close(); }

        Task.Run(() =>
        {
            var hist = new InviseHistoryItem(DateTime.Now.ToString("MM/dd HH:mm"),
                desc, address.Replace("https://", ""));
            using StreamWriter writer = new(_profileHistoryPath);
            InviseHistoryList.Insert(0, hist);

            var doc = JsonSerializer.Serialize(InviseHistoryList);
            writer.Write(doc);
            writer.Close();
            Application.Current.Dispatcher.Invoke(delegate
            {
                var listBoxItem = new ListViewItem();

                ListViewItemProperties.SetTimeHistory(listBoxItem, hist.Time);
                ListViewItemProperties.SetDescHistory(listBoxItem, hist.Description);
                ListViewItemProperties.SetLinkPreview(listBoxItem, hist.Link[..hist.Link.IndexOf('/')]);
                ListViewItemProperties.SetFullLink(listBoxItem, hist.Link);
                _listView.Items.Insert(0, listBoxItem);
            });
        });
    }
    private void LoadHistoryJson()
    {
        if (!File.Exists(_profileHistoryPath)) { return; }

        using StreamReader reader = new(_profileHistoryPath);
        var json = reader.ReadToEnd();
        reader.Close();
        InviseHistoryList = JsonSerializer.Deserialize<ObservableCollection<InviseHistoryItem>>(JsonNode.Parse(json).ToString());
    }
    private void LoadHistoryLink(object link)
    {
        AddTab();
        (CurrentTabItem.Content as InviseBrowser).LoadUrlAsync(link.ToString());
    }
    private void OpenContextMenuSettings(object arg)
    {
        if (arg is StackPanel button)
        {
            button.ContextMenu.DataContext = button.DataContext;
            button.ContextMenu.IsOpen = true;
        }
    }

    private void AddListViewItem(InviseHistoryItem item)
    {
        Application.Current.Dispatcher.Invoke((Action)delegate
        {
            var listBoxItem = new ListViewItem();

            ListViewItemProperties.SetTimeHistory(listBoxItem, item.Time);
            ListViewItemProperties.SetDescHistory(listBoxItem, item.Description);
            ListViewItemProperties.SetLinkPreview(listBoxItem, item.Link[..item.Link.IndexOf('/')]);
            ListViewItemProperties.SetFullLink(listBoxItem, item.Link);
            _listView.Items.Add(listBoxItem);
        });
    }
    private async void LoadHistoryAsListView()
    {
        _listView.Items.Clear();
        Task.Run(() =>
        {
            foreach (var item in InviseHistoryList)
            {
                AddListViewItem(item);
            }
        });
    }
    #endregion

    #region Tab Work
    private async void AddTabHistory()
    {
        if (InviseHistoryList.Count == 0) { return; }

        Tabs.Add(new TabItem() { Tag = _mainIDCounter, Content = _listView });
        CurrentTabItem = Tabs.Last();
        Address = "invise://history/";
        var button = new Label
        {
            Content = "History",
            AllowDrop = true,
            Tag = _mainIDCounter
        };
        button.DragEnter += BtnTabDragEnter;
        button.MouseLeftButtonDown += BtnMouseDownForDragAndOpenTab;

        if (_mainIDCounter == 0) { TabBtnsAndAddTabBtn.Insert(0, button); }
        else { TabBtnsAndAddTabBtn.Insert(TabBtnsAndAddTabBtn.Count - 1, button); }

        _mainIDCounter++;
        LoadHistoryAsListView();
    }
    private async void AddTab()
    {
        var browser = await InitBrowser(_mainIDCounter > 0);
        browser.TitleChanged += Browser_TitleChanged;
        browser.LoadingStateChanged += Browser_LoadingStateChanged;
        browser.AddressChanged += Browser_AddressChanged;

        Tabs.Add(new TabItem() { Tag = _mainIDCounter, Content = browser });
        CurrentTabItem = Tabs.Last();

        var button = new Label
        {
            Content = browser.Title,
            AllowDrop = true,
            Tag = _mainIDCounter
        };
        button.DragEnter += BtnTabDragEnter;
        button.MouseLeftButtonDown += BtnMouseDownForDragAndOpenTab;

        if (_mainIDCounter == 0) { TabBtnsAndAddTabBtn.Insert(0, button); }
        else { TabBtnsAndAddTabBtn.Insert(TabBtnsAndAddTabBtn.Count - 1, button); }

        _mainIDCounter++;
    }

    private void BtnMouseDownForDragAndOpenTab(object sender, MouseButtonEventArgs e)
    {
        _tabBtnToDrag = (sender as Label);
        DragDrop.DoDragDrop(_tabBtnToDrag, _tabBtnToDrag, DragDropEffects.Move);
        e.Handled = true;
        OpenTab(_tabBtnToDrag.Tag);
    }

    private void BtnTabDragEnter(object sender, DragEventArgs e)
    {
        #region Animation to fututur updates
        //System.Windows.Media.Animation.Storyboard fadeInStoryboard = new();
        //System.Windows.Media.Animation.DoubleAnimation fadeInAnimation = new()
        //{
        //    From = 0,
        //    To = 1,
        //    Duration = TimeSpan.FromSeconds(0.5)
        //};
        //System.Windows.Media.Animation.Storyboard.SetTargetProperty(fadeInAnimation, new PropertyPath(UIElement.OpacityProperty));
        //fadeInStoryboard.Children.Add(fadeInAnimation);
        //fadeInStoryboard.Begin(_tabBtnToDrag);
        #endregion

        if (e.Source.ToString().Contains("Border") || e.Source.ToString().Contains("TextBlock")) { return; }
        Label btn = (Label)e.Source;
        int where_to_drop = TabBtnsAndAddTabBtn.IndexOf(btn);
        TabBtnsAndAddTabBtn.Remove(_tabBtnToDrag);
        TabBtnsAndAddTabBtn.Insert(where_to_drop, _tabBtnToDrag);
    }

    private void CloseTab(object arg)
    {
        if (((MouseButtonEventArgs)arg).Source is not TextBlock tb) return;
        var id = (int)tb.Tag;

        // Delete from Tabs
        var itemToRemove = Tabs.FirstOrDefault(item => (int)item.Tag == id);
        if (itemToRemove != null)
        {
            itemToRemove.Content = null;
            if (Tabs.Count == 0) return;

            int currentIndex = Tabs.IndexOf(itemToRemove);
            if (currentIndex > 0)
            {
                CurrentTabItem = currentIndex > 0 ? Tabs[currentIndex - 1] : Tabs[currentIndex + 1];
            }

            if (currentIndex == 0 && Tabs.Count > 1) { CurrentTabItem = Tabs[currentIndex + 1]; }

            Tabs.Remove(itemToRemove);
        }

        // Delete from TabBtns
        var tabBtnToRemove = TabBtnsAndAddTabBtn.OfType<Label>().FirstOrDefault(item => (int)item.Tag == id);
        if (tabBtnToRemove != null) { TabBtnsAndAddTabBtn.Remove(tabBtnToRemove); }

        // Delete from Browsers
        _browsers = _browsers.Where(item => (int)item.Tag != id).ToList();
    }

    private void OpenTab(object arg)
    {
        var tabToSelect = tabs.FirstOrDefault(item => (int)item.Tag == (int)arg);
        if (tabToSelect != null)
        {
            CurrentTabItem = tabToSelect;
            if (CurrentTabItem.Content.ToString().Contains("ListView"))
            {
                Address = "invise://history/";
            }
            else { Address = (CurrentTabItem.Content as InviseBrowser).Address; }
        }
    }
    #endregion

    #region Window Work
    private void AddressOnKeyDown(object arg)
    {
        if ((arg as KeyEventArgs).Key == Key.Enter)
        {
            (CurrentTabItem.Content as InviseBrowser).LoadUrlAsync(Address);
        }
    }
    private void Back(object arg)
    {
        if (CurrentTabItem == null) return;
        (CurrentTabItem.Content as InviseBrowser).Back();
    }
    private void Forward(object arg)
    {
        if (CurrentTabItem == null) return;
        (CurrentTabItem.Content as InviseBrowser).Forward();
    }
    private void Refresh(object arg)
    {
        if (CurrentTabItem == null) return;

        if (CurrentTabItem.Content.ToString().Contains("ListView"))
        {
            CurrentTabItem.Content = _listView;
        }
        else
        {
            (CurrentTabItem.Content as InviseBrowser).Reload();
        }
    }
    private void NormalStateWindow(object arg)
    {
        //if ((arg as MouseEventArgs).LeftButton == MouseButtonState.Pressed)
        //{
        //    CurWindowState = WindowState.Normal;
        //}
    }
    private void MinimizedWindow(object arg)
    {
        CurWindowState = WindowState.Minimized;
    }
    private void MaximizedWindow(object arg)
    {
        CurWindowState = WindowState.Maximized;

        //if (CurWindowState == WindowState.Maximized)
        //{
        //    CurWindowState = WindowState.Normal;
        //}
        //else
        //{
        //    CurWindowState = WindowState.Maximized;
        //}
    }
    private void CloseWindow(object obj)
    {
        Close?.Invoke();
    }
    public bool CanClose()
    {
        return true;
    }
    #endregion
}