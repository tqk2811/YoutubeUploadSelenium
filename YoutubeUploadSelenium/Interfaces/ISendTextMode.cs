using System.Threading;

namespace YoutubeUploadSelenium.Interfaces
{
    internal interface ISendTextMode
    {
        bool UsePasteForSpecialCharacter { get; }
        SynchronizationContext? SynchronizationContextForClipboard { get; }
    }
}
