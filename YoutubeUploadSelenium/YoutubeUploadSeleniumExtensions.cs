using OpenQA.Selenium;
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
using Nito.AsyncEx;
using System.Windows;
using System.Globalization;
namespace YoutubeUploadSelenium
{
    internal static class YoutubeUploadSeleniumExtensions
    {
        public static TimeSpan MinScheduleTime { get; set; } = TimeSpan.FromMinutes(2);
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

            if (!string.IsNullOrEmpty(videoUploadInfo.Description))
            {
                videoUploadHandle.WriteLog($"Set description {videoUploadInfo.Description}");
                var ele = await waiter
                    .WaitUntilElements("div#description-container :is(ytcp-social-suggestion-input,ytcp-mention-input)[id='input'] div#textbox")
                    .Until().Any().Clickable()
                    .WithThrow()
                    .StartAsync()
                    .FirstAsync();
                ele.JsClick();
                ele.Clear();
                await ele.SendTextAsync(videoUploadInfo, videoUploadInfo.Description, cancellationToken);
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
                        .WaitUntilElements("ytcp-video-thumbnail-editor input[id='file-loader']")
                        .Until().ElementsExists()
                        .WithThrow()
                        .StartAsync()
                        .FirstAsync()
                        .SendKeysAsync(videoUploadInfo.ThumbPath);
                    await Task.Delay(3000, cancellationToken);
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (videoUploadInfo.PlayList is not null)
            {
                await waiter
                    .WaitUntilElements("ytcp-text-dropdown-trigger.ytcp-video-metadata-playlists")
                    .Until().Any().Clickable()
                    .WithThrow()
                    .StartAsync()
                    .FirstAsync().JsClickAsync();
                var eles = await waiter
                    .WaitUntilElements("tp-yt-paper-dialog.ytcp-playlist-dialog ytcp-checkbox-group#playlists-list")
                    .Until().ElementsExists()
                    .WithThrow()
                    .StartAsync();
                await Task.Delay(100, cancellationToken);
                eles = await waiter
                    .WaitUntilElements("tp-yt-paper-dialog.ytcp-playlist-dialog ytcp-checkbox-group#playlists-list ytcp-ve.ytcp-checkbox-group")
                    .Until().ElementsExists()
                    .WithTimeout(videoUploadHandle.TimeoutWaitLoadPlayList)
                    .StartAsync();
                List<string> availablePlayList = new();
                foreach (var ele in eles)
                {
                    string PlayListName = ele.Text.Trim();
                    availablePlayList.Add(PlayListName);
                    if (await videoUploadInfo.PlayList.PlayListHandleAsync(PlayListName, cancellationToken))
                    {
                        videoUploadHandle.WriteLog($"Set playlist {PlayListName}");
                        var ele2 = ele.FindElements(By.CssSelector("ytcp-checkbox-lit")).FirstOrDefault();
                        if (ele2 != null)
                            ele2.JsClick();
                    }
                }
                IEnumerable<string> playlistCreate = await videoUploadInfo.PlayList.GetPlayListCreateAsync(cancellationToken);

                foreach (var item in playlistCreate)
                {
                    //create
                    await waiter
                        .WaitUntilElements("tp-yt-paper-dialog.ytcp-playlist-dialog ytcp-button.new-playlist-button")
                        .Until().Any().Visible()
                        .WithThrow()
                        .StartAsync().FirstAsync()
                        .JsClickAsync();
                    await Task.Delay(100, cancellationToken);
                    await waiter
                        .WaitUntilElements("tp-yt-paper-dialog.ytcp-playlist-dialog ytcp-text-menu.ytcp-playlist-dialog tp-yt-paper-item[test-id='new_playlist']")
                        .Until().Any().Visible()
                        .WithThrow()
                        .StartAsync().FirstAsync()
                        .JsClickAsync();
                    await Task.Delay(100, cancellationToken);

                    var ele = await waiter
                        .WaitUntilElements("ytcp-playlist-creation-dialog[dialog-type='CREATE_PLAYLIST'] ytcp-social-suggestions-textbox#title-textarea")
                        .Until().Any().Visible()
                        .WithThrow()
                        .StartAsync().FirstAsync()
                        .JsClickAsync()
                        .DelayAsync(500, cancellationToken);
                    await ele.SendTextAsync(videoUploadInfo, item, cancellationToken);


                    await Task.Delay(100, cancellationToken);
                    await waiter
                         .WaitUntilElements("ytcp-playlist-creation-dialog[dialog-type='CREATE_PLAYLIST'] ytcp-button#create-button")
                         .Until().Any().Visible()
                         .WithThrow()
                         .StartAsync().FirstAsync()
                         .JsClickAsync();
                    await Task.Delay(100, cancellationToken);
                    await waiter
                        .WaitUntilElements("ytcp-playlist-creation-dialog[dialog-type='CREATE_PLAYLIST']")
                        .UntilNotExist().Any().Visible()
                        .WithTimeout(2000)
                        .StartAsync();//wait it hidden
                }

                await waiter
                    .WaitUntilElements("tp-yt-paper-dialog.ytcp-playlist-dialog ytcp-button.done-button")
                    .Until().Any().Clickable()
                    .WithThrow()
                    .StartAsync()
                    .FirstAsync().JsClickAsync();
                await waiter
                    .WaitUntilElements("tp-yt-paper-dialog.ytcp-playlist-dialog ytcp-button.new-playlist-button")
                    .UntilNotExist().Any().Visible()
                    .WithTimeout(2000)
                    .StartAsync();//wait it hidden
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (!string.IsNullOrWhiteSpace(videoUploadInfo.Tags))
            {
                videoUploadHandle.WriteLog($"Set tags {videoUploadInfo.Tags}");
                await waiter
                    .WaitUntilElements("ytcp-button#toggle-button.ytcp-video-metadata-editor")
                    .Until().ElementsExists()
                    .WithThrow()
                    .StartAsync()
                    .FirstAsync().JsClickAsync();
                var ele = await waiter
                    .WaitUntilElements("ytcp-form-input-container#tags-container input#text-input")
                    .Until().ElementsExists()
                    .WithThrow()
                    .StartAsync()
                    .FirstAsync();

                await ele.SendTextAsync(videoUploadInfo, videoUploadInfo.Tags, cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();

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

            cancellationToken.ThrowIfCancellationRequested();

            if (!string.IsNullOrEmpty(videoUploadInfo.Title))
            {
                if (videoUploadInfo.Title.Length > 100) throw new Exception("Tiêu đề dài hơn 100 ký tự");
                videoUploadHandle.WriteLog($"Set title {videoUploadInfo.Title}");
                var ele = await waiter
                    .WaitUntilElements("ytcp-social-suggestions-textbox[id='title-textarea'] :is(ytcp-social-suggestion-input,ytcp-mention-input)[id='input'] div#textbox")
                    .Until().Any().Clickable()
                    .WithThrow()
                    .StartAsync()
                    .FirstAsync();
                ele.JsClick();
                ele.Clear();
                await ele.SendTextAsync(videoUploadInfo, videoUploadInfo.Title, cancellationToken);
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

            //wait upload done
            await task_waitUploadDone;

            videoUploadHandle.WriteLog($"Video url: {UrlResult}");

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
                if (videoUploadInfo.Schedule is not null && videoUploadInfo.Schedule >= DateTime.Now.Add(MinScheduleTime))
                {
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
                    string day = videoUploadInfo.Schedule.Value.ToString(videoUploadHandle.DateFormat, videoUploadHandle.DateCultureInfo);
                    ele.SendKeys(day + "\r\n");
                    videoUploadHandle.WriteLog($"Set SCHEDULE: Day:{day}");

                    ele = await waiter
                        .WaitUntilElements("ytcp-form-input-container#time-of-day-container tp-yt-paper-input.ytcp-datetime-picker input")
                        .Until().Any().Clickable()
                        .WithThrow()
                        .StartAsync()
                        .FirstAsync();
                    string time = videoUploadInfo.Schedule.Value.ToString(videoUploadHandle.TimeFormat, videoUploadHandle.TimeCultureInfo);
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
                    videoUploadInfo.Schedule = null;
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


                await waiter
                    .WaitUntilElements("tp-yt-paper-dialog[class*='ytcp-uploads-dialog'] ytcp-button[id='done-button']")
                    .Until().Any().Clickable()
                    .WithThrow()
                    .StartAsync()
                    .FirstAsync().JsClickAsync();

                await waiter
                    .WaitUntilElements("tp-yt-paper-dialog[class*='ytcp-uploads-dialog'] ytcp-button[id='done-button']")
                    .UntilNotExist().Any().Clickable()
                    .WithTimeout(5000)
                    .StartAsync();
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

        static readonly AsyncLock _lock_clipboard = new AsyncLock();
        static async Task SendTextAsync(this IWebElement webElement, ISendTextMode sendTextMode, string text, CancellationToken cancellationToken = default)
        {
            if (sendTextMode.UsePasteForSpecialCharacter && sendTextMode.SynchronizationContextForClipboard is not null)
            {
                using var l = await _lock_clipboard.LockAsync(cancellationToken);
                await sendTextMode.SynchronizationContextForClipboard.PostAsync(() => Clipboard.SetText(text));
                await Task.Delay(100, cancellationToken);
                webElement.SendKeys(Keys.Control + "v");
                await Task.Delay(100, cancellationToken);
            }
            else
            {
                webElement.SendKeys(text);
            }
        }
        static async Task<T> DelayAsync<T>(this Task<T> task, int delay, CancellationToken cancellationToken = default)
        {
            T result = await task;
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            return result;
        }
    }
}
