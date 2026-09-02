using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Serilog;
using Splat;
using SS14.Launcher.Localization;
using SS14.Launcher.Models.Data;
using SS14.Launcher.Models.EngineManager;
using SS14.Launcher.Models.ContentManagement;
using SS14.Launcher.Utility;

namespace SS14.Launcher.ViewModels.MainWindowTabs;

/// <summary>
/// ViewModel for the Development tab. Split into partial classes:
/// - DevelopmentTabViewModel.cs — CVar properties and configuration
/// - DevelopmentTabViewModel.Diagnostics.cs — Benchmarks, system/network diagnostics
/// - DevelopmentTabViewModel.Maintenance.cs — Clearing engines, logs, caches, CVars
/// </summary>
public sealed partial class DevelopmentTabViewModel : MainWindowTabViewModel
{
    private readonly LocalizationManager _loc = LocalizationManager.Instance;
    private readonly DataManager _cfg = Locator.Current.GetRequiredService<DataManager>();
    private readonly IEngineManager _engineManager = Locator.Current.GetRequiredService<IEngineManager>();
    private readonly ContentManager _contentManager = Locator.Current.GetRequiredService<ContentManager>();

    private string _actionStatus = "";

    public string ActionStatus
    {
        get => _actionStatus;
        set => SetProperty(ref _actionStatus, value);
    }

    public string[] LogLevels => ["Default", "Verbose", "Debug", "Info", "Warning", "Error"];
    public string[] GraphicsBackends => ["Default", "OpenGL", "OpenGLES", "Software"];
    public string[] DisplayModes => ["Default", "Windowed", "Borderless", "Fullscreen"];

    public DevelopmentTabViewModel()
    {
        _cfg.GetCVarEntry(CVars.EngineOverrideEnabled).PropertyChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(Name));
        };
        _cfg.GetCVarEntry(CVars.CustomDevelopmentTabName).PropertyChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(Name));
        };
    }

    public override string Name
    {
        get
        {
            var custom = _cfg.GetCVar(CVars.CustomDevelopmentTabName);
            if (!string.IsNullOrWhiteSpace(custom))
                return custom;
            return _cfg.GetCVar(CVars.EngineOverrideEnabled)
                ? _loc.GetString("tab-development-title-override")
                : _loc.GetString("tab-development-title");
        }
    }

    public bool DisableSigning
    {
        get => _cfg.GetCVar(CVars.DisableSigning);
        set
        {
            _cfg.SetCVar(CVars.DisableSigning, value);
            _cfg.CommitConfig();
        }
    }

    public bool EngineOverrideEnabled
    {
        get => _cfg.GetCVar(CVars.EngineOverrideEnabled);
        set
        {
            _cfg.SetCVar(CVars.EngineOverrideEnabled, value);
            _cfg.CommitConfig();
        }
    }

    public string EngineOverridePath
    {
        get => _cfg.GetCVar(CVars.EngineOverridePath);
        set
        {
            _cfg.SetCVar(CVars.EngineOverridePath, value);
            _cfg.CommitConfig();
        }
    }

    public string DevCustomLaunchArguments
    {
        get => _cfg.GetCVar(CVars.DevCustomLaunchArguments);
        set
        {
            _cfg.SetCVar(CVars.DevCustomLaunchArguments, value);
            _cfg.CommitConfig();
        }
    }

    public string DevLogLevel
    {
        get => _cfg.GetCVar(CVars.DevLogLevel);
        set
        {
            _cfg.SetCVar(CVars.DevLogLevel, value);
            _cfg.CommitConfig();
        }
    }

    public bool DevUncappedFps
    {
        get => _cfg.GetCVar(CVars.DevUncappedFps);
        set
        {
            _cfg.SetCVar(CVars.DevUncappedFps, value);
            _cfg.CommitConfig();
        }
    }

    public bool DevGenerateCrashDumps
    {
        get => _cfg.GetCVar(CVars.DevGenerateCrashDumps);
        set
        {
            _cfg.SetCVar(CVars.DevGenerateCrashDumps, value);
            _cfg.CommitConfig();
        }
    }

    public bool DevRenderValidation
    {
        get => _cfg.GetCVar(CVars.DevRenderValidation);
        set
        {
            _cfg.SetCVar(CVars.DevRenderValidation, value);
            _cfg.CommitConfig();
        }
    }

    public bool DevFastThreadPool
    {
        get => _cfg.GetCVar(CVars.DevFastThreadPool);
        set
        {
            _cfg.SetCVar(CVars.DevFastThreadPool, value);
            _cfg.CommitConfig();
        }
    }

    public bool DevAggressiveLohTrim
    {
        get => _cfg.GetCVar(CVars.DevAggressiveLohTrim);
        set
        {
            _cfg.SetCVar(CVars.DevAggressiveLohTrim, value);
            _cfg.CommitConfig();
        }
    }

    public bool DevStrictDiagnostics
    {
        get => _cfg.GetCVar(CVars.DevStrictDiagnostics);
        set
        {
            _cfg.SetCVar(CVars.DevStrictDiagnostics, value);
            _cfg.CommitConfig();
        }
    }

    public bool DevTieredPgo
    {
        get => _cfg.GetCVar(CVars.DevTieredPgo);
        set
        {
            _cfg.SetCVar(CVars.DevTieredPgo, value);
            _cfg.CommitConfig();
        }
    }

    public int DevGcHeapLimitMb
    {
        get => _cfg.GetCVar(CVars.DevGcHeapLimitMb);
        set
        {
            _cfg.SetCVar(CVars.DevGcHeapLimitMb, Math.Max(0, value));
            _cfg.CommitConfig();
        }
    }

    public string DevGraphicsBackend
    {
        get => _cfg.GetCVar(CVars.DevGraphicsBackend);
        set
        {
            _cfg.SetCVar(CVars.DevGraphicsBackend, value);
            _cfg.CommitConfig();
        }
    }

    public string DevDisplayMode
    {
        get => _cfg.GetCVar(CVars.DevDisplayMode);
        set
        {
            _cfg.SetCVar(CVars.DevDisplayMode, value);
            _cfg.CommitConfig();
        }
    }

    public bool DevDebugFpsOverlay
    {
        get => _cfg.GetCVar(CVars.DevDebugFpsOverlay);
        set
        {
            _cfg.SetCVar(CVars.DevDebugFpsOverlay, value);
            _cfg.CommitConfig();
        }
    }

    public bool DevDebugNetGraph
    {
        get => _cfg.GetCVar(CVars.DevDebugNetGraph);
        set
        {
            _cfg.SetCVar(CVars.DevDebugNetGraph, value);
            _cfg.CommitConfig();
        }
    }

    public bool DevOpenConsoleOnStart
    {
        get => _cfg.GetCVar(CVars.DevOpenConsoleOnStart);
        set
        {
            _cfg.SetCVar(CVars.DevOpenConsoleOnStart, value);
            _cfg.CommitConfig();
        }
    }

    public bool DevPhysicsDebug
    {
        get => _cfg.GetCVar(CVars.DevPhysicsDebug);
        set
        {
            _cfg.SetCVar(CVars.DevPhysicsDebug, value);
            _cfg.CommitConfig();
        }
    }

    public string DevCustomEnvVars
    {
        get => _cfg.GetCVar(CVars.DevCustomEnvVars);
        set
        {
            _cfg.SetCVar(CVars.DevCustomEnvVars, value);
            _cfg.CommitConfig();
        }
    }

    public int DevAudioBufferSize
    {
        get => _cfg.GetCVar(CVars.DevAudioBufferSize);
        set
        {
            _cfg.SetCVar(CVars.DevAudioBufferSize, value);
            _cfg.CommitConfig();
        }
    }

    public int DevSimulatedPingMs
    {
        get => _cfg.GetCVar(CVars.DevSimulatedPingMs);
        set
        {
            _cfg.SetCVar(CVars.DevSimulatedPingMs, Math.Clamp(value, 0, 2000));
            _cfg.CommitConfig();
        }
    }

    public int DevSimulatedJitterMs
    {
        get => _cfg.GetCVar(CVars.DevSimulatedJitterMs);
        set
        {
            _cfg.SetCVar(CVars.DevSimulatedJitterMs, Math.Clamp(value, 0, 500));
            _cfg.CommitConfig();
        }
    }

    public int DevSimulatedPacketLoss
    {
        get => _cfg.GetCVar(CVars.DevSimulatedPacketLoss);
        set
        {
            _cfg.SetCVar(CVars.DevSimulatedPacketLoss, Math.Clamp(value, 0, 100));
            _cfg.CommitConfig();
        }
    }

    public bool DevMuteAudio
    {
        get => _cfg.GetCVar(CVars.DevMuteAudio);
        set
        {
            _cfg.SetCVar(CVars.DevMuteAudio, value);
            _cfg.CommitConfig();
        }
    }

    public bool DevShowLightMap
    {
        get => _cfg.GetCVar(CVars.DevShowLightMap);
        set
        {
            _cfg.SetCVar(CVars.DevShowLightMap, value);
            _cfg.CommitConfig();
        }
    }

    public bool DevShowEntityBounds
    {
        get => _cfg.GetCVar(CVars.DevShowEntityBounds);
        set
        {
            _cfg.SetCVar(CVars.DevShowEntityBounds, value);
            _cfg.CommitConfig();
        }
    }

    public bool DevDisableNetCompression
    {
        get => _cfg.GetCVar(CVars.DevDisableNetCompression);
        set
        {
            _cfg.SetCVar(CVars.DevDisableNetCompression, value);
            _cfg.CommitConfig();
        }
    }

    public bool DevGCNoAffinitize
    {
        get => _cfg.GetCVar(CVars.DevGCNoAffinitize);
        set
        {
            _cfg.SetCVar(CVars.DevGCNoAffinitize, value);
            _cfg.CommitConfig();
        }
    }

    public bool DevForceScalarSearch
    {
        get => _cfg.GetCVar(CVars.DevForceScalarSearch);
        set
        {
            _cfg.SetCVar(CVars.DevForceScalarSearch, value);
            _cfg.CommitConfig();
        }
    }

    public bool HighProcessPriority
    {
        get => _cfg.GetCVar(CVars.HighProcessPriority);
        set
        {
            _cfg.SetCVar(CVars.HighProcessPriority, value);
            _cfg.CommitConfig();
        }
    }

    public bool ForceDedicatedGpu
    {
        get => _cfg.GetCVar(CVars.ForceDedicatedGpu);
        set
        {
            _cfg.SetCVar(CVars.ForceDedicatedGpu, value);
            _cfg.CommitConfig();
        }
    }

    public bool MaxPerformanceJit
    {
        get => _cfg.GetCVar(CVars.MaxPerformanceJit);
        set
        {
            _cfg.SetCVar(CVars.MaxPerformanceJit, value);
            _cfg.CommitConfig();
        }
    }

    public bool LowLatencyNetworking
    {
        get => _cfg.GetCVar(CVars.LowLatencyNetworking);
        set
        {
            _cfg.SetCVar(CVars.LowLatencyNetworking, value);
            _cfg.CommitConfig();
        }
    }

}

