using YoutubeUploadSelenium.Enums;
using System;

namespace YoutubeUploadSelenium.Interfaces
{
    public interface IVideoUploadData
    {
        string? Description { get; }
        bool IsDraft { get; }
        bool IsMakeForKid { get; }
        bool Premiere { get; }
        DateTime? Schedule { get; set; }
        string? Tags { get; }
        string? ThumbPath { get; }
        string? Title { get; }
        string VideoPath { get; }
        VideoPrivacyStatus VideoPrivacyStatus { get; }
    }
}
