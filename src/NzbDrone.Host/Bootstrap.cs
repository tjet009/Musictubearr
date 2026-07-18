using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using DryIoc;
using DryIoc.Microsoft.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using NLog;
using NzbDrone.Common.Composition;
using NzbDrone.Common.Composition.Extensions;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Exceptions;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Common.Options;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Datastore.Extensions;
using PostgresOptions = NzbDrone.Core.Datastore.PostgresOptions;

namespace NzbDrone.Host
{
    public static class Bootstrap
    {
        private static readonly Logger Logger = NzbDroneLogger.GetLogger(typeof(Bootstrap));

        public static void Start(string[] args, Action<IHostBuilder> trayCallback = null)
        {
            try
            {
                Logger.Info("Starting MusicTubearr - {0} - Version {1}",
                            Environment.ProcessPath,
                            Assembly.GetExecutingAssembly().GetName().Version);

                var startupContext = new StartupContext(args);

                LongPathSupport.Enable();
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

                var appMode = GetApplicationMode(startupContext);
                var config = GetConfiguration(startupContext);

                switch (appMode)
                {
                    case ApplicationModes.Service:
                        StartService(startupContext);
                        break;
                    case ApplicationModes.Interactive:
                        StartInteractive(startupContext, trayCallback);
                        break;
                    default:
                        StartUtility(startupContext, appMode, config);
                        break;
                }
            }
            catch (InvalidConfigFileException ex)
            {
                throw new LidarrStartupException(ex);
            }
            catch (AccessDeniedConfigFileException ex)
            {
                throw new LidarrStartupException(ex);
            }
            catch (TerminateApplicationException ex)
            {
                Logger.Info(ex.Message);
                LogManager.Configuration = null;
            }

            // Make sure there are no lingering database connections
            GC.Collect();
            GC.WaitForPendingFinalizers();
            SQLiteConnection.ClearAllPools();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void StartService(StartupContext context)
        {
            Logger.Debug("Service selected");

            var success = StartService(context, true, out var pluginRefs);

            if (!success)
            {
                var unloadSuccess = PluginLoader.UnloadPlugins(pluginRefs);

                if (unloadSuccess)
                {
                    StartService(context, false, out _);
                }
            }

            CreateConsoleHostBuilder(context, false, out _).UseWindowsService().Build().Run();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void StartInteractive(StartupContext context, Action<IHostBuilder> trayCallback)
        {
            Logger.Debug(trayCallback != null ? "Tray selected" : "Console selected");

            var success = StartInteractive(context, trayCallback, true, out var pluginRefs);

            if (!success)
            {
                var unloadSuccess = PluginLoader.UnloadPlugins(pluginRefs);

                if (unloadSuccess)
                {
                    StartInteractive(context, trayCallback, false, out _);
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool StartService(StartupContext context, bool usePlugins, out List<WeakReference> pluginRefs)
        {
            var builder = CreateConsoleHostBuilder(context, usePlugins, out pluginRefs).UseWindowsService();

            return RunBuilder(builder, usePlugins);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool StartInteractive(StartupContext context, Action<IHostBuilder> trayCallback, bool usePlugins, out List<WeakReference> pluginRefs)
        {
            var builder = CreateConsoleHostBuilder(context, usePlugins, out pluginRefs);

            if (trayCallback != null)
            {
                trayCallback(builder);
            }

            return RunBuilder(builder, usePlugins);
        }

        private static bool RunBuilder(IHostBuilder builder, bool usePlugins)
        {
            try
            {
                using var host = builder.Build();
                host.Run();
            }
            catch (Exception e)
            {
                if (usePlugins)
                {
                    Logger.Warn(e, "Error starting with plugins enabled");
                }

                return false;
            }

            return true;
        }

        private static void StartUtility(StartupContext context, ApplicationModes mode, IConfiguration config)
        {
            var assemblies = AssemblyLoader.LoadBaseAssemblies();
            new HostBuilder()
                .UseServiceProviderFactory(new DryIocServiceProviderFactory(new Container(rules => rules.WithNzbDroneRules())))
                .ConfigureContainer<IContainer>(c =>
                {
                    c.AutoAddServices(assemblies)
                        .AddNzbDroneLogger()
                        .AddDatabase()
                        .AddStartupContext(context)
                        .Resolve<UtilityModeRouter>()
                        .Route(mode);

                    if (config.GetValue(nameof(ConfigFileProvider.LogDbEnabled), true))
                    {
                        c.AddLogDatabase();
                    }
                    else
                    {
                        c.AddDummyLogDatabase();
                    }
                })
                .ConfigureServices(services =>
                {
                    ConfigureAppOptions(services, config);
                }).Build();
        }

        private static IHostBuilder CreateConsoleHostBuilder(StartupContext context, bool usePlugins, out List<WeakReference> pluginRef)
        {
            var config = GetConfiguration(context);

            var bindAddress = GetConfigValue(config, $"Server:{nameof(ServerOptions.BindAddress)}", nameof(ConfigFileProvider.BindAddress), "*");
            var port = GetConfigValue(config, $"Server:{nameof(ServerOptions.Port)}", nameof(ConfigFileProvider.Port), 8585);
            var sslPort = GetConfigValue(config, $"Server:{nameof(ServerOptions.SslPort)}", nameof(ConfigFileProvider.SslPort), 8586);
            var enableSsl = GetConfigValue(config, $"Server:{nameof(ServerOptions.EnableSsl)}", nameof(ConfigFileProvider.EnableSsl), false);
            var sslCertPath = GetConfigValue<string>(config, $"Server:{nameof(ServerOptions.SslCertPath)}", nameof(ConfigFileProvider.SslCertPath), null);
            var sslCertPassword = GetConfigValue<string>(config, $"Server:{nameof(ServerOptions.SslCertPassword)}", nameof(ConfigFileProvider.SslCertPassword), null);
            var logDbEnabled = GetConfigValue(config, $"Log:{nameof(LogOptions.DbEnabled)}", nameof(ConfigFileProvider.LogDbEnabled), true);

            var urls = new List<string> { BuildUrl("http", bindAddress, port) };

            if (enableSsl && sslCertPath.IsNotNullOrWhiteSpace())
            {
                urls.Add(BuildUrl("https", bindAddress, sslPort));
            }

            var assemblies = AssemblyLoader.LoadBaseAssemblies();
            pluginRef = null;

            if (usePlugins)
            {
                var pluginPaths = new AppFolderInfo(context).GetPluginAssemblies().ToList();
                (var plugins, pluginRef) = PluginLoader.LoadPlugins(pluginPaths);

                assemblies.AddRange(plugins.Where(x => x != null));
            }

            return new HostBuilder()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseServiceProviderFactory(new DryIocServiceProviderFactory(new Container(rules => rules.WithNzbDroneRules())))
                .ConfigureContainer<IContainer>(c =>
                {
                    c.AutoAddServices(assemblies)
                        .SetPluginStatus(usePlugins)
                        .AddNzbDroneLogger()
                        .AddDatabase()
                        .AddStartupContext(context);

                    if (logDbEnabled)
                    {
                        c.AddLogDatabase();
                    }
                    else
                    {
                        c.AddDummyLogDatabase();
                    }
                })
                .ConfigureServices(services =>
                {
                    ConfigureAppOptions(services, config);
                })
                .ConfigureWebHost(builder =>
                {
                    builder.UseConfiguration(config);
                    builder.UseUrls(urls.ToArray());
                    builder.UseKestrel(options =>
                    {
                        if (enableSsl && sslCertPath.IsNotNullOrWhiteSpace())
                        {
                            options.ConfigureHttpsDefaults(configureOptions =>
                            {
                                configureOptions.ServerCertificate = ValidateSslCertificate(sslCertPath, sslCertPassword);
                            });
                        }
                    });
                    builder.ConfigureKestrel(serverOptions =>
                    {
                        serverOptions.AllowSynchronousIO = false;
                        serverOptions.Limits.MaxRequestBodySize = null;
                    });
                    builder.UseStartup<Startup>();
                });
        }

        private static ApplicationModes GetApplicationMode(IStartupContext startupContext)
        {
            if (startupContext.Help)
            {
                return ApplicationModes.Help;
            }

            if (OsInfo.IsWindows && startupContext.RegisterUrl)
            {
                return ApplicationModes.RegisterUrl;
            }

            if (OsInfo.IsWindows && startupContext.InstallService)
            {
                return ApplicationModes.InstallService;
            }

            if (OsInfo.IsWindows && startupContext.UninstallService)
            {
                return ApplicationModes.UninstallService;
            }

            // IsWindowsService can throw sometimes, so wrap it
            var isWindowsService = false;
            try
            {
                isWindowsService = WindowsServiceHelpers.IsWindowsService();
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to get service status");
            }

            if (OsInfo.IsWindows && isWindowsService)
            {
                return ApplicationModes.Service;
            }

            return ApplicationModes.Interactive;
        }

        private static void ConfigureAppOptions(IServiceCollection services, IConfiguration config)
        {
            // MusicTubearr env prefix preferred; Lidarr: kept as compatibility fallback.
            services.Configure<PostgresOptions>(options =>
            {
                config.GetSection("Lidarr:Postgres").Bind(options);
                config.GetSection("MusicTubearr:Postgres").Bind(options);
            });
            services.Configure<AppOptions>(options =>
            {
                config.GetSection("Lidarr:App").Bind(options);
                config.GetSection("MusicTubearr:App").Bind(options);
            });
            services.Configure<AuthOptions>(options =>
            {
                config.GetSection("Lidarr:Auth").Bind(options);
                config.GetSection("MusicTubearr:Auth").Bind(options);
            });
            services.Configure<ServerOptions>(options =>
            {
                config.GetSection("Lidarr:Server").Bind(options);
                config.GetSection("MusicTubearr:Server").Bind(options);
            });
            services.Configure<LogOptions>(options =>
            {
                config.GetSection("Lidarr:Log").Bind(options);
                config.GetSection("MusicTubearr:Log").Bind(options);
            });
            services.Configure<UpdateOptions>(options =>
            {
                config.GetSection("Lidarr:Update").Bind(options);
                config.GetSection("MusicTubearr:Update").Bind(options);
            });
        }

        private static T GetConfigValue<T>(IConfiguration config, string relativeKey, string xmlKey, T defaultValue)
        {
            var primary = config.GetSection($"MusicTubearr:{relativeKey}");
            if (primary.Exists() && primary.Value != null)
            {
                return config.GetValue<T>($"MusicTubearr:{relativeKey}");
            }

            var legacy = config.GetSection($"Lidarr:{relativeKey}");
            if (legacy.Exists() && legacy.Value != null)
            {
                return config.GetValue<T>($"Lidarr:{relativeKey}");
            }

            return config.GetValue(xmlKey, defaultValue);
        }

        private static IConfiguration GetConfiguration(StartupContext context)
        {
            var appFolder = new AppFolderInfo(context);
            var configPath = appFolder.GetConfigPath();

            try
            {
                return new ConfigurationBuilder()
                    .AddXmlFile(configPath, optional: true, reloadOnChange: false)
                    .AddInMemoryCollection(new List<KeyValuePair<string, string>> { new ("dataProtectionFolder", appFolder.GetDataProtectionPath()) })
                    .AddEnvironmentVariables()
                    .Build();
            }
            catch (InvalidDataException ex)
            {
                Logger.Error(ex, ex.Message);

                throw new InvalidConfigFileException($"{configPath} is corrupt or invalid. Please delete the config file and MusicTubearr will recreate it.", ex);
            }
        }

        private static string BuildUrl(string scheme, string bindAddress, int port)
        {
            return $"{scheme}://{bindAddress}:{port}";
        }

        private static X509Certificate2 ValidateSslCertificate(string cert, string password)
        {
            X509Certificate2 certificate;

            try
            {
                certificate = new X509Certificate2(cert, password, X509KeyStorageFlags.DefaultKeySet);
            }
            catch (CryptographicException ex)
            {
                if (ex.HResult == 0x2 || ex.HResult == 0x2006D080)
                {
                    throw new LidarrStartupException(ex,
                        $"The SSL certificate file {cert} does not exist");
                }

                throw new LidarrStartupException(ex);
            }

            return certificate;
        }
    }
}
