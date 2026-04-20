namespace TopSpeed.Runtime
{
    public interface IUpdatePackageInstaller
    {
        bool TryInstallPackage(string packagePath, out string errorMessage);
    }
}
