using System;
using System.Threading.Tasks;

namespace AtmoSync.Client
{
    interface IClient
    {
        Task<bool> SoundExistsAsync(Guid id, string path);
    }
}
