﻿using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace AtmoSync
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        void NavigateToClient(object sender, TappedRoutedEventArgs e)
        {
            Frame.Navigate(typeof(Client.ClientPage));
        }

        void NavigateToServer(object sender, TappedRoutedEventArgs e)
        {
            Frame.Navigate(typeof(Server.ServerPage));
        }
    }
}
