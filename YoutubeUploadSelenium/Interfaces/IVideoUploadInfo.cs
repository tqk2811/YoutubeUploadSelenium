using YoutubeUploadSelenium.Enums;
using System.Collections.Generic;
using System;

namespace YoutubeUploadSelenium.Interfaces
{
    internal interface IVideoUploadInfo
    {
        string? Description { get; }
        bool IsDraft { get; }
        bool IsMakeForKid { get; }
        List<string>? PlayList { get; }
        bool Premiere { get; }
        DateTime? Schedule { get; }
        string? Tags { get; }
        string? ThumbPath { get; }
        string? Title { get; }
        string VideoPath { get; }
        VideoPrivacyStatus VideoPrivacyStatus { get; }
    }
}
