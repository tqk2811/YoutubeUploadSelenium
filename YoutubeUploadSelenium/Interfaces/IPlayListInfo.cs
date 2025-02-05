using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace YoutubeUploadSelenium.Interfaces
{
    internal interface IPlayListInfo
    {
        Task<bool> PlayListHandleAsync(string name, CancellationToken cancellationToken = default);
        Task<IEnumerable<string>> GetPlayListCreateAsync(CancellationToken cancellationToken = default);
    }
}
