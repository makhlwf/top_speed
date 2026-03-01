namespace TopSpeed.Menu
{
    internal interface IMenuServerActions
    {
        void StartServerDiscovery();
        void OpenSavedServersManager();
        void BeginManualServerEntry();
        void BeginServerPortEntry();
    }
}
