namespace YoutubeUploadSelenium.Interfaces
{
    internal interface IVideoUpload
    {
        IVideoUploadData VideoUploadData { get; }
        IVideoUploadHandle VideoUploadHandle { get; }
    }
}
