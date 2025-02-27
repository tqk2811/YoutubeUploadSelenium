using System;
using System.Threading.Tasks;

namespace YoutubeUploadSelenium.Interfaces
{
    internal interface IVideoUploadHandle
    {
        int TimeoutWaitLoadPlayList { get; }
        void WriteLog(string log);
        void UploadProgressCallback(int percent);
        string GetDateFormat(DateTime dateTime);
        string GetTimeFormat(DateTime dateTime);
        bool UsePasteForSpecialCharacter { get; }
        Task PasteClipboardAsync(string text);

        IPlayListHandle? PlayListHandle { get; }
    }
}
