using AtmoSync.Shared;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace AtmoSync.Server
{
    public sealed partial class SoundControl : UserControl
    {
        public SoundControl()
        {
            this.InitializeComponent();

            RotateSync.Begin();
        }


        private void ShowFlyout(object sender, TappedRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
        }

        private void PlaySound(object sender, TappedRoutedEventArgs e)
        {
            var sound = (Sound)((FrameworkElement)sender).DataContext;
            sound.Status = Status.Playing;
        }

        private void PauseSound(object sender, TappedRoutedEventArgs e)
        {
            var sound = (Sound)((FrameworkElement)sender).DataContext;
            sound.Status = Status.Paused;
        }

        private void StopSound(object sender, TappedRoutedEventArgs e)
        {
            var sound = (Sound)((FrameworkElement)sender).DataContext;
            sound.Status = Status.Stopped;
        }

        private void ToggleSync(object sender, RoutedEventArgs e)
        {

        }
    }
}
