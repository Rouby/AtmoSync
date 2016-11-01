using AtmoSync.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtmoSync.Client
{
    interface IClient
    {
        Task<bool> SoundExistsAsync(Guid id);
        void AddSound(Sound sound);
        void SyncSound(Guid id, Sound sound);
        void RemoveSound(Guid id); 
    }
}
