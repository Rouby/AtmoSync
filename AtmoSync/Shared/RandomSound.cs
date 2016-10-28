using System.Collections.ObjectModel;
using System.Linq;

namespace AtmoSync.Shared
{
    class RandomSound : Sound
    {
        ObservableCollection<string> _files = new ObservableCollection<string>();
        public string[] Files { get { return _files.ToArray(); } set { SetProperty(ref _files, new ObservableCollection<string>(value)); } }
    }
}
