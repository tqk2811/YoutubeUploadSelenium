﻿using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TqkLibrary.SeleniumSupport;
using TqkLibrary.SeleniumSupport.Helper.WaitHeplers;
using YoutubeUploadSelenium.Enums;
using YoutubeUploadSelenium.Interfaces;
using YoutubeUploadSelenium.Exceptions;
using System.Text.RegularExpressions;
namespace YoutubeUploadSelenium
{
    internal static class Extensions
    {
        public static async Task<string> UploadAsync(
            this WebDriver webDriver,
            IVideoUploadInfo videoUploadInfo,
            IVideoUploadHandle videoUploadHandle,
            CancellationToken cancellationToken = default)
        {
            if (videoUploadInfo is null) throw new ArgumentNullException(nameof(videoUploadInfo));
            if (videoUploadHandle is null) throw new ArgumentNullException(nameof(videoUploadHandle));

            var waiter = new WaitHelper(webDriver, cancellationToken);//throw if webDriver null
            waiter.Do(() => webDriver.Check());

            webDriver.Navigate().GoToUrl("https://www.youtube.com/upload");

            videoUploadHandle.WriteLog($"Upload {videoUploadInfo.VideoPath}");
            await waiter
                .WaitUntilElements(By.Name("Filedata"))
                .Until().ElementsExists()
                .WithThrow()
                .WithTimeout(20000)
                .StartAsync()
                .FirstAsync()
                .SendKeysAsync(videoUploadInfo.VideoPath);

            Task task_waitUploadDone = webDriver.ReadProgressAsync(videoUploadHandle, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            if (!string.IsNullOrEmpty(videoUploadInfo.Title))
            {
                if (videoUploadInfo.Title.Length > 100) throw new Exception("Tiêu đề dài hơn 100 ký tự");
                videoUploadHandle.WriteLog($"Set title {videoUploadInfo.Title}");
                var ele = await waiter
                    .WaitUntilElements("ytcp-social-suggestions-textbox[id='title-textarea'] :is(ytcp-social-suggestion-input,ytcp-mention-input)[id='input'] div[id='textbox']")
                    .Until().Any().Clickable()
                    .WithThrow()
                    .StartAsync()
                    .FirstAsync();
                ele.JsClick();
                ele.Clear();
                ele.SendKeys(videoUploadInfo.Title);
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (!string.IsNullOrEmpty(videoUploadInfo.Description))
            {
                videoUploadHandle.WriteLog($"Set description {videoUploadInfo.Description}");
                var ele = await waiter
                    .WaitUntilElements("div[id='description-container'] :is(ytcp-social-suggestion-input,ytcp-mention-input)[id='input'] div[id='textbox']")
                    .Until().Any().Clickable()
                    .WithThrow()
                    .StartAsync()
                    .FirstAsync();
                ele.JsClick(); 
                ele.Clear();
                ele.SendKeys(videoUploadInfo.Description);
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (File.Exists(videoUploadInfo.ThumbPath))
            {
                videoUploadHandle.WriteLog($"Set Thumb Image {videoUploadInfo.ThumbPath}");
                if (webDriver.FindElements(By.CssSelector("ytcp-thumbnails-compact-editor-uploader[feature-disabled]")).Count > 0)
                {
                    videoUploadHandle.WriteLog($"Thumbnails feature-disabled on this account");
                }
                else
                {
                    await waiter
                        .WaitUntilElements("div[id='still-picker'] input[id='file-loader']")
                        .Until().ElementsExists()
                        .WithThrow()
                        .StartAsync()
                        .FirstAsync()
                        .SendKeysAsync(videoUploadInfo.ThumbPath);
                    await Task.Delay(3000, cancellationToken);
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (videoUploadInfo.PlayList != null && videoUploadInfo.PlayList.Count > 0)
            {
                await waiter
                    .WaitUntilElements("ytcp-text-dropdown-trigger[class*='ytcp-video-metadata-playlists']")
                    .Until().Any().Clickable()
                    .WithThrow()
                    .StartAsync()
                    .FirstAsync().JsClickAsync();
                var eles = await waiter
                    .WaitUntilElements("tp-yt-paper-dialog[class*='ytcp-playlist-dialog'] ytcp-checkbox-group[id='playlists-list'] ytcp-ve[class*='ytcp-checkbox-group']")
                    .Until().ElementsExists()
                    .WithThrow()
                    .StartAsync();
                eles = await waiter
                    .WaitUntilElements("tp-yt-paper-dialog[class*='ytcp-playlist-dialog'] ytcp-checkbox-group[id='playlists-list'] ytcp-ve[class*='ytcp-checkbox-group']")
                    .Until().ElementsExists()
                    .WithThrow()
                    .StartAsync();
                foreach (var ele in eles)
                {
                    string PlayListName = ele.Text.Trim();
                    if (videoUploadInfo.PlayList.Any(x => x.Equals(PlayListName)))
                    {
                        videoUploadHandle.WriteLog($"Set playlist {PlayListName}");
                        var ele2 = ele.FindElements(By.CssSelector("ytcp-checkbox-lit")).FirstOrDefault();
                        if (ele2 != null)
                            ele2.JsClick();
                    }
                }

                await waiter
                    .WaitUntilElements("tp-yt-paper-dialog[class*='ytcp-playlist-dialog'] ytcp-button[class*='done-button']")
                    .Until().Any().Clickable()
                    .WithThrow()
                    .StartAsync()
                    .FirstAsync().JsClickAsync();
            }

            if (!string.IsNullOrWhiteSpace(videoUploadInfo.Tags))
            {
                videoUploadHandle.WriteLog($"Set tags {videoUploadInfo.Tags}");
                await waiter
                    .WaitUntilElements("ytcp-button[id='toggle-button'][class*='ytcp-video-metadata-editor']")
                    .Until().ElementsExists()
                    .WithThrow()
                    .StartAsync()
                    .FirstAsync().JsClickAsync();
                await waiter
                    .WaitUntilElements("ytcp-form-input-container[id='tags-container'] input[id='text-input']")
                    .Until().ElementsExists()
                    .WithThrow()
                    .StartAsync()
                    .FirstAsync()
                    .SendKeysAsync(videoUploadInfo.Tags);
            }

            //IsMakeForKid
            {
                videoUploadHandle.WriteLog($"Set KIDS: {videoUploadInfo.IsMakeForKid}");
                //old ver MADE_FOR_KIDS
                //new ver VIDEO_MADE_FOR_KIDS_MFK
                if (videoUploadInfo.IsMakeForKid)
                    await waiter
                        .WaitUntilElements("tp-yt-paper-radio-button:is([name='MADE_FOR_KIDS'],[name='VIDEO_MADE_FOR_KIDS_MFK'])")
                        .Until().All().Clickable()
                        .WithThrow()
                        .StartAsync()
                        .FirstAsync().JsClickAsync();
                else
                    await waiter
                        .WaitUntilElements("tp-yt-paper-radio-button:is([name='NOT_MADE_FOR_KIDS'],[name='VIDEO_MADE_FOR_KIDS_NOT_MFK'])")
                        .Until().Any().Clickable()
                        .WithThrow()
                        .StartAsync()
                        .FirstAsync().JsClickAsync();
            }

            //get url result
            var ele_ = await waiter
                    .WaitUntilElements(".video-url-fadeable")
                    .Until().All().Clickable()
                    .WithThrow()
                    .WithTimeout(60000)
                    .StartAsync()
                    .FirstAsync();
            string UrlResult = ele_.Text;
            while (string.IsNullOrWhiteSpace(UrlResult))
            {
                await Task.Delay(500, cancellationToken);
                ele_ = await waiter
                    .WaitUntilElements(".video-url-fadeable")
                    .Until().All().Clickable()
                    .WithThrow()
                    .WithTimeout(60000)
                    .StartAsync()
                    .FirstAsync();
                UrlResult = ele_.Text;
            }

            if (!videoUploadInfo.IsDraft)
            {
                //open tab REVIEW
                await waiter
                    .WaitUntilElements("button[id='step-badge-3'][test-id='REVIEW']")
                    .Until().Any().Clickable()
                    .WithThrow()
                    .StartAsync()
                    .FirstAsync().JsClickAsync();

                //Schedule
                if (videoUploadInfo.Schedule is not null)
                {
                    if (videoUploadInfo.Schedule < DateTime.Now.AddMinutes(15))
                        throw new InvalidOperationException($"Invalid Schedule Time");

                    await waiter
                        .WaitUntilElements("div#second-container div.early-access-header>ytcp-icon-button:not(hidden)")
                        .Until().All().Clickable()
                        .WithThrow()
                        .StartAsync()
                        .FirstAsync().JsClickAsync();
                    await waiter
                        .WaitUntilElements("ytcp-text-dropdown-trigger[id='datepicker-trigger']")
                        .Until().Any().Clickable()
                        .WithThrow()
                        .StartAsync()
                        .FirstAsync().JsClickAsync();
                    var ele = await waiter
                        .WaitUntilElements("tp-yt-paper-dialog[class*='ytcp-date-picker'] input[class*='tp-yt-paper-input']")
                        .Until().Any().Clickable()
                        .WithThrow()
                        .StartAsync()
                        .FirstAsync();
                    ele.JsClick();
                    ele.Clear();
                    string day = videoUploadInfo.Schedule.Value.ToString(videoUploadHandle.GetDateFormat());
                    ele.SendKeys(day + "\r\n");
                    videoUploadHandle.WriteLog($"Set SCHEDULE: Day:{day}");

                    ele = await waiter
                        .WaitUntilElements("ytcp-form-input-container#time-of-day-container tp-yt-paper-input.ytcp-datetime-picker iron-input>input")
                        .Until().Any().Clickable()
                        .WithThrow()
                        .StartAsync()
                        .FirstAsync();
                    string time = videoUploadInfo.Schedule.Value.ToString("HH:mm");
                    ele.Click();
                    ele.Clear();
                    ele.SendKeys(time + "\r\n");
                    videoUploadHandle.WriteLog($"Set SCHEDULE: Time:{time}");

                    if (videoUploadInfo.Premiere)
                    {
                        videoUploadHandle.WriteLog($"Set schedule-premiere");
                        await waiter
                            .WaitUntilElements("#schedule-type-checkbox")
                            .Until().All().Clickable()
                            .WithThrow()
                            .StartAsync()
                            .FirstAsync().JsClickAsync();
                    }
                }
                else
                {
                    videoUploadHandle.WriteLog($"Set Privacy: {videoUploadInfo.VideoPrivacyStatus}");
                    await waiter
                        .WaitUntilElements(By.Name(videoUploadInfo.VideoPrivacyStatus.ToString()))
                        .Until().All().Clickable()
                        .WithThrow()
                        .StartAsync()
                        .FirstAsync().JsClickAsync();

                    if (videoUploadInfo.VideoPrivacyStatus == VideoPrivacyStatus.PUBLIC && videoUploadInfo.Premiere)
                    {
                        videoUploadHandle.WriteLog($"Set premiere");
                        await waiter
                            .WaitUntilElements("#enable-premiere-checkbox")
                            .Until().All().Clickable()
                            .WithThrow()
                            .StartAsync()
                            .FirstAsync().JsClickAsync();
                    }
                }
            }

            //wait upload done
            await task_waitUploadDone;

            videoUploadHandle.WriteLog($"Video url: {UrlResult}");

            await Task.Delay(2000, cancellationToken);

            if (!videoUploadInfo.IsDraft)
            {
                await waiter
                    .WaitUntilElements("tp-yt-paper-dialog[class*='ytcp-uploads-dialog'] ytcp-button[id='done-button']")
                    .Until().Any().Clickable()
                    .WithThrow()
                    .StartAsync()
                    .FirstAsync().JsClickAsync();
            }            

            await Task.Delay(2000, cancellationToken);
            videoUploadHandle.WriteLog($"Upload Hoàn tất");

            return UrlResult;
        }

        private static async Task ReadProgressAsync(
            this WebDriver webDriver,
            IVideoUploadHandle videoUploadHandle,
            CancellationToken cancellationToken = default)
        {
            Regex regex_num = new Regex("\\d+");

            var waiter = new WaitHelper(webDriver, cancellationToken);
            waiter.Do(() => webDriver.Check());

            //waiter.DefaultTimeout = timeout;

            await waiter.WaitUntilElements(".ytcp-uploads-dialog ytcp-video-upload-progress[uploading]").Until().ElementsExists().StartAsync();

            while (webDriver.FindElements(By.CssSelector(".ytcp-uploads-dialog ytcp-video-upload-progress[uploading]")).Count > 0)
            {
                string? progress = webDriver.FindElements(By.CssSelector(".ytcp-uploads-dialog span.ytcp-video-upload-progress")).FirstOrDefault()?.Text?.Trim();
                if (!string.IsNullOrWhiteSpace(progress))
                {
                    videoUploadHandle?.WriteLog($"Upload: {progress}");
                    Match match = regex_num.Match(progress);
                    if (match.Success)
                    {
                        videoUploadHandle?.UploadProgressCallback(int.Parse(match.Value));
                    }
                }
                webDriver.Check();
                await Task.Delay(500, cancellationToken);
            }
            webDriver.Check();
        }

        static void Check(this WebDriver webDriver)
        {
            var eles = webDriver.FindElements("ytcp-auth-confirmation-dialog");
            if (eles.Count > 0)
                throw new YtcpAuthConfirmationDialogException();

            eles = webDriver.FindElements("ytcp-uploads-dialog[has-error]");
            if (eles.Count > 0)
                throw new Exception($"{webDriver.FindElements("ytcp-uploads-dialog[has-error] .error-short").FirstOrDefault()?.Text}");
        }
    }
}
