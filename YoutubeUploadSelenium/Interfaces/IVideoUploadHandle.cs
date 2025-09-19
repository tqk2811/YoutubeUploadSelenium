using System;
using System.Threading.Tasks;

namespace YoutubeUploadSelenium.Interfaces
{
    public interface IVideoUploadHandle
    {
        int TimeoutWaitLoadPlayList { get; }
        void WriteLog(string log);
        void UploadProgressCallback(int percent);
        string GetDateFormat(DateTime dateTime);
        string GetTimeFormat(DateTime dateTime);
        bool UsePasteForSpecialCharacter { get; }
        Task<IDisposable> PasteClipboardAsync(string text, CancellationToken cancellationToken = default);

        IPlayListHandle? PlayListHandle { get; }
    }
}
