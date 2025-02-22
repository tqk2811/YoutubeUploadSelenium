﻿using YoutubeUploadSelenium.Enums;
using System;

namespace YoutubeUploadSelenium.Interfaces
{
    internal interface IVideoUploadInfo : ISendTextMode
    {
        string? Description { get; }
        bool IsDraft { get; }
        bool IsMakeForKid { get; }
        bool Premiere { get; }
        IPlayListInfo? PlayList { get; }
        DateTime? Schedule { get; set; }
        string? Tags { get; }
        string? ThumbPath { get; }
        string? Title { get; }
        string VideoPath { get; }
        VideoPrivacyStatus VideoPrivacyStatus { get; }
    }
}
