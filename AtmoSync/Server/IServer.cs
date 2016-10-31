using System;
using System.Threading.Tasks;
using Windows.Storage;

namespace AtmoSync.Server
{
    interface IServer
    {
        Task<IStorageFile> GetSoundFileAsync(Guid id);
    }
}
