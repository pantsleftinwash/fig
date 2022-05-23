using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Fig.Client.ClientSecret;
using Fig.Client.Configuration;
using Fig.Client.IPAddress;
using Fig.Client.Versions;
using Fig.Contracts.Status;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Fig.Client.Status
{
    public class SettingStatusMonitor : ISettingStatusMonitor
    {
        private readonly IIpAddressResolver _ipAddressResolver;
        private readonly IVersionProvider _versionProvider;
        private readonly Timer _statusTimer;
        private readonly Guid _runSessionId;
        private readonly DateTime _startTime;
        private DateTime _lastSettingUpdate;
        private ILogger _logger;
        private IFigOptions _options;
        private IClientSecretProvider _clientSecretProvider;
        private SettingsBase _settings;
        private bool _liveReload;
        private bool _isOffline;
        public event EventHandler SettingsChanged;
        public event EventHandler ReconnectedToApi;
        public event EventHandler OfflineSettingsDisabled;

        public SettingStatusMonitor(IIpAddressResolver ipAddressResolver, IVersionProvider versionProvider)
        {
            _ipAddressResolver = ipAddressResolver;
            _versionProvider = versionProvider;
            _startTime = DateTime.UtcNow;
            _runSessionId = Guid.NewGuid();
            _statusTimer = new Timer();
            _statusTimer.Elapsed += OnStatusTimerElapsed;
        }

        public bool AllowOfflineSettings { get; private set; }

        public void Initialize<T>(T settings, IFigOptions figOptions, IClientSecretProvider clientSecretProvider, ILogger logger) where T : SettingsBase
        {
            _logger = logger;
            _options = figOptions;
            _clientSecretProvider = clientSecretProvider;
            _settings = settings;
            _liveReload = figOptions.LiveReload;
            _lastSettingUpdate = DateTime.UtcNow;
            _statusTimer.Interval = figOptions.PollIntervalMs;
            _statusTimer.Start();
        }

        public void SettingsUpdated()
        {
            _lastSettingUpdate = DateTime.UtcNow;
        }
        
        private async void OnStatusTimerElapsed(object sender, ElapsedEventArgs e)
        {
            _statusTimer.Stop();
            try
            {
                await GetStatus();

                if (_isOffline)
                {
                    _logger.LogError("Reconnected to Fig API.");
                    ReconnectedToApi?.Invoke(this, EventArgs.Empty);
                }
                
                _isOffline = false;
            }
            catch (HttpRequestException exception)
            {
                _isOffline = true;
                _logger.LogError($"Unable to contact Fig API. {exception.Message}");
            }
            catch (Exception exception)
            {
                _logger.LogError($"Error getting status: {exception}");
            }
            finally
            {
                _statusTimer.Start();
            }
        }

        private async Task GetStatus()
        {
            using var client = new HttpClient();
            client.BaseAddress = _options.ApiUri;

            var request = new StatusRequestDataContract
            {
                RunSessionId = _runSessionId,
                UptimeSeconds = (DateTime.UtcNow - _startTime).TotalSeconds,
                LastSettingUpdate = _lastSettingUpdate,
                PollIntervalMs = _statusTimer.Interval,
                LiveReload = _liveReload,
                FigVersion = _versionProvider.GetFigVersion(),
                ApplicationVersion = _versionProvider.GetHostVersion(),
                OfflineSettingsEnabled = _options.AllowOfflineSettings && AllowOfflineSettings
            };

            var json = JsonConvert.SerializeObject(request);
            var data = new StringContent(json, Encoding.UTF8, "application/json");
            client.DefaultRequestHeaders.Add("Fig_IpAddress", _ipAddressResolver.Resolve());
            client.DefaultRequestHeaders.Add("Fig_Hostname", Environment.MachineName);
            
            client.DefaultRequestHeaders.Add("clientSecret", _clientSecretProvider.GetSecret(_settings.ClientName));
            var response = await client.PutAsync($"/statuses/{_settings.ClientName}", data);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Failed to get status from Fig API. {response.StatusCode}");
                return;
            }

            var result = await response.Content.ReadAsStringAsync();
            var statusResponse = JsonConvert.DeserializeObject<StatusResponseDataContract>(result);
            ProcessResponse(statusResponse);
        }

        private void ProcessResponse(StatusResponseDataContract? statusResponse)
        {
            if (statusResponse is null)
                return;

            _statusTimer.Interval = statusResponse.PollIntervalMs;
            _liveReload = statusResponse.LiveReload;
            
            if (statusResponse.LiveReload && statusResponse.SettingUpdateAvailable)
                SettingsChanged?.Invoke(this, EventArgs.Empty);

            AllowOfflineSettings = statusResponse.AllowOfflineSettings;
            if (!AllowOfflineSettings)
                OfflineSettingsDisabled?.Invoke(this, EventArgs.Empty);
        }
    }
}