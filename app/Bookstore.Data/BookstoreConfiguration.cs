using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Bookstore.Data
{
    public sealed class BookstoreConfiguration
    {
        private readonly Dictionary<string, string> _appSettings = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _connectionStrings = new Dictionary<string, string>();

        public BookstoreConfiguration(IConfiguration configuration)
        {
            // Load app settings from IConfiguration (appsettings.json, environment, etc.)
            var appSettingsSection = configuration.GetSection("AppSettings");
            foreach (var child in appSettingsSection.GetChildren())
            {
                _appSettings[child.Key] = child.Value;
            }

            // Environment variables still override, preserving original behavior
            foreach (var key in _appSettings.Keys.ToList())
            {
                var envValue = Environment.GetEnvironmentVariable(key);
                if (envValue != null)
                {
                    _appSettings[key] = envValue;
                }
            }

            // Load connection strings from IConfiguration
            var connectionStringsSection = configuration.GetSection("ConnectionStrings");
            foreach (var child in connectionStringsSection.GetChildren())
            {
                _connectionStrings[child.Key] = child.Value;
            }
        }

        public void AddSetting(string key, string value)
        {
            _appSettings[key] = value;
        }

        public string GetSetting(string key)
        {
            return _appSettings[key];
        }

        public T GetSetting<T>(string key)
        {
            var value = _appSettings[key];
            return (T)Convert.ChangeType(value, typeof(T));
        }

        public void AddConnectionString(string key, string value)
        {
            _connectionStrings[key] = value;
        }

        public string GetConnectionString(string key)
        {
            return _connectionStrings[key];
        }
    }
}