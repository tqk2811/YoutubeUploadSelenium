namespace YoutubeUploadSelenium.Interfaces
{
    public interface IVideoUpload
    {
        IVideoUploadData VideoUploadData { get; }
        IVideoUploadHandle VideoUploadHandle { get; }
    }
}
