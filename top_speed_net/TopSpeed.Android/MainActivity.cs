using System;
using System.IO;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Java.Interop;

namespace TopSpeed.Android;

[Register("org/libsdl/app/SDLActivity", DoNotGenerateAcw = true)]
public class SdlActivityBase : Activity
{
    private static readonly JniPeerMembers Members = new XAPeerMembers("org/libsdl/app/SDLActivity", typeof(SdlActivityBase));

    public override JniPeerMembers JniPeerMembers => Members;

    protected override IntPtr ThresholdClass => Members.JniPeerType.PeerReference.Handle;

    protected override Type ThresholdType => Members.ManagedPeerType;

    public unsafe SdlActivityBase()
        : base(IntPtr.Zero, JniHandleOwnership.DoNotTransfer)
    {
        const string id = "()V";

        if (Handle != IntPtr.Zero)
            return;

        var result = Members.InstanceMethods.StartCreateInstance(id, GetType(), null);
        SetHandle(result.Handle, JniHandleOwnership.TransferLocalRef);
        Members.InstanceMethods.FinishCreateInstance(id, this, null);
    }

    protected SdlActivityBase(IntPtr javaReference, JniHandleOwnership transfer)
        : base(javaReference, transfer)
    {
    }

}

[Activity(
    Label = "@string/app_name",
    Theme = "@style/TopSpeed.Fullscreen",
    MainLauncher = true,
    Exported = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.KeyboardHidden)]
public class MainActivity : SdlActivityBase
{
    private const string LogTag = "TopSpeed.Android";
    private string? _assetRoot;
    private AndroidMotionSteeringSource? _motionSteering;
    private AndroidSpeechThreadDispatcher? _speechDispatcher;
    private AndroidUpdatePackageInstaller? _updateInstaller;

    public MainActivity()
    {
    }

    protected MainActivity(IntPtr javaReference, JniHandleOwnership transfer)
        : base(javaReference, transfer)
    {
    }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        Log.Info(LogTag, "MainActivity.OnCreate");
        RequestWindowFeature(WindowFeatures.NoTitle);
        Window?.AddFlags(WindowManagerFlags.Fullscreen);
        Window?.ClearFlags(WindowManagerFlags.ForceNotFullscreen);
        _assetRoot = EnsureRuntimeAssets();
        _motionSteering = new AndroidMotionSteeringSource(this);
        _speechDispatcher = new AndroidSpeechThreadDispatcher();
        _updateInstaller = new AndroidUpdatePackageInstaller(this);
        global::TopSpeed.Runtime.MotionSteeringRuntime.SetSource(_motionSteering);
        global::TopSpeed.Runtime.SpeechThreadRuntime.SetDispatcher(_speechDispatcher);
        global::TopSpeed.Runtime.UpdatePackageRuntime.SetInstaller(_updateInstaller);
        base.OnCreate(savedInstanceState);
        ApplyImmersiveMode();
    }

    public override void OnWindowFocusChanged(bool hasFocus)
    {
        base.OnWindowFocusChanged(hasFocus);
        if (hasFocus)
            ApplyImmersiveMode();
    }

    protected override void OnDestroy()
    {
        global::TopSpeed.Runtime.MotionSteeringRuntime.SetSource(null);
        global::TopSpeed.Runtime.SpeechThreadRuntime.SetDispatcher(null);
        global::TopSpeed.Runtime.UpdatePackageRuntime.SetInstaller(null);
        _speechDispatcher?.Dispose();
        _speechDispatcher = null;
        _motionSteering = null;
        _updateInstaller = null;
        global::TopSpeed.AndroidLauncher.RequestClose();
        base.OnDestroy();
    }

    [Export("getLibraries")]
    public string[] GetLibraries()
    {
        Log.Info(LogTag, "MainActivity.getLibraries -> SDL3");
        return
        [
            "SDL3"
        ];
    }

    [Export("loadLibraries")]
    public void LoadLibraries()
    {
        Log.Info(LogTag, "MainActivity.loadLibraries -> SDL3");
        Java.Lang.JavaSystem.LoadLibrary("SDL3");
    }

    [Export("main")]
    public void RunSdlMain()
    {
        Log.Info(LogTag, "MainActivity.main (managed entry)");
        try
        {
            global::TopSpeed.AndroidLauncher.SetAssetRoot(_assetRoot ?? EnsureRuntimeAssets());
            global::TopSpeed.AndroidLauncher.Run();
        }
        catch (Exception ex)
        {
            Log.Error(LogTag, ex.ToString());
            ShowLaunchError(ex);
        }
    }

    private void ShowLaunchError(Exception ex)
    {
        RunOnUiThread(() =>
        {
            if (IsFinishing || IsDestroyed)
                return;

            var dialogBuilder = new AlertDialog.Builder(this);
            dialogBuilder.SetTitle("Top Speed startup failed");
            dialogBuilder.SetMessage(ex.ToString());
            dialogBuilder.SetCancelable(false);
            dialogBuilder.SetPositiveButton("Close", (_, _) => FinishAffinity());
            var dialog = dialogBuilder.Create();
            if (dialog == null)
                return;

            dialog.Show();
        });
    }

    private string EnsureRuntimeAssets()
    {
        var filesRoot = FilesDir?.AbsolutePath;
        var targetRoot = Path.Combine(
            string.IsNullOrWhiteSpace(filesRoot) ? AppContext.BaseDirectory : filesRoot!,
            "topspeed_assets");

        CopyAssetTree("Sounds", Path.Combine(targetRoot, "Sounds"));
        CopyAssetTree("Tracks", Path.Combine(targetRoot, "Tracks"));
        CopyAssetTree("Vehicles", Path.Combine(targetRoot, "Vehicles"));
        CopyAssetTree("languages", Path.Combine(targetRoot, "languages"));
        CopyAssetTree("docs", Path.Combine(targetRoot, "docs"));
        return targetRoot;
    }

    private void CopyAssetTree(string assetPath, string targetPath)
    {
        var assetManager = Assets;
        if (assetManager == null)
            return;

        string[] entries;
        try
        {
            entries = assetManager.List(assetPath) ?? Array.Empty<string>();
        }
        catch
        {
            return;
        }

        if (entries.Length == 0)
        {
            CopyAssetFile(assetPath, targetPath);
            return;
        }

        Directory.CreateDirectory(targetPath);
        for (var i = 0; i < entries.Length; i++)
        {
            var entry = entries[i];
            if (string.IsNullOrWhiteSpace(entry))
                continue;

            var childAssetPath = string.IsNullOrEmpty(assetPath) ? entry : $"{assetPath}/{entry}";
            var childTargetPath = Path.Combine(targetPath, entry);
            CopyAssetTree(childAssetPath, childTargetPath);
        }
    }

    private void CopyAssetFile(string assetPath, string targetPath)
    {
        if (File.Exists(targetPath))
            return;

        var directory = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory!);

        try
        {
            var assetManager = Assets;
            if (assetManager == null)
                return;

            using var source = assetManager.Open(assetPath);
            using var destination = File.Create(targetPath);
            source.CopyTo(destination);
        }
        catch
        {
            // Ignore extraction failures for optional assets.
        }
    }

    private void ApplyImmersiveMode()
    {
        try
        {
            var window = Window;
            if (window == null)
                return;

            window.SetDecorFitsSystemWindows(false);
            var controller = window.InsetsController;
            if (controller == null)
                return;

            controller.Hide(WindowInsets.Type.StatusBars() | WindowInsets.Type.NavigationBars());
            controller.SystemBarsBehavior = 2;
        }
        catch
        {
            // Window insets controller may not be available during early activity creation.
        }
    }
}
