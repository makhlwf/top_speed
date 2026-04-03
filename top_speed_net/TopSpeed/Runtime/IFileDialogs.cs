using System;

namespace TopSpeed.Runtime
{
    internal interface IFileDialogs
    {
        void PickAudioFile(Action<string?> onCompleted);
        void PickFolder(string? initialFolder, Action<string?> onCompleted);
    }
}



