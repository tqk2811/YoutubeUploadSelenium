namespace YoutubeUploadSelenium.Interfaces
{
    internal interface IVideoUploadHandle
    {
        void WriteLog(string log);
        void UploadProgressCallback(int percent);
    }
}
