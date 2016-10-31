using AtmoSync.Shared;
using System;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace AtmoSync.Server
{
    public sealed partial class SoundControl : UserControl
    {
        public event EventHandler SoundRemoved;

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

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private async void SoundControl_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var sound = (Sound)sender;
            switch (e.PropertyName)
            {
                case nameof(Sound.Status):
                    switch (sound.Status)
                    {
                        case Status.Playing:
                            mediaElement.Play();
                            break;
                        case Status.Paused:
                            mediaElement.Pause();
                            break;
                        case Status.Stopped:
                            mediaElement.Stop();
                            break;
                    }
                    break;
                case nameof(Sound.File):
                    try
                    {
                        var file = await StorageFile.GetFileFromPathAsync(sound.File);
                        mediaElement.SetSource(await file.OpenReadAsync(), file.ContentType);
                    }
                    catch (Exception exc)
                    {
                        sound.Invalid = true;
                        sound.InvalidMessage = exc.Message;
                    }
                    break;
            }
            if (e.PropertyName == nameof(Sound.Status))
            {
            }
        }

        private void RemoveSoundTapped(object sender, TappedRoutedEventArgs e)
        {
            SoundRemoved?.Invoke(DataContext, new EventArgs());
        }

        private async void UserControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            var sound = (Sound)DataContext;
            sound.PropertyChanged += SoundControl_PropertyChanged;
            try
            {
                var file = await StorageFile.GetFileFromPathAsync(sound.File);
                mediaElement.SetSource(await file.OpenReadAsync(), file.ContentType);
            }
            catch (Exception exc)
            {
                sound.Invalid = true;
                sound.InvalidMessage = exc.Message;
            }
        }

        private void mediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            var sound = (Sound)DataContext;
            sound.Status = Status.Stopped;
        }
    }
}
