using System.Globalization;
using System.Threading.Tasks;

namespace YoutubeUploadSelenium.Interfaces
{
    internal interface IVideoUploadHandle
    {
        int TimeoutWaitLoadPlayList { get; }
        void WriteLog(string log);
        void UploadProgressCallback(int percent);
        string DateFormat { get; }
        CultureInfo? DateCultureInfo { get; }
        string TimeFormat { get; }
        CultureInfo? TimeCultureInfo { get; }
    }
}
