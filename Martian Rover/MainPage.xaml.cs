using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.Sockets;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Leere Seite" wird unter https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x407 dokumentiert.

namespace Martian_Rover {
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class MainPage : Page {
        //public List<string> deviceList = new List<string>();

        public MainPage() {
            this.InitializeComponent();
            reloadDevices();
        }

        private async void loadDevices_Click(object sender, RoutedEventArgs e) {
            reloadDevices();
        }

        private async void connect_Click(object sender, RoutedEventArgs e) {
            if (this.deviceSelector.SelectedValue is DeviceListItem) {
                var deviceInfo = ((DeviceListItem)this.deviceSelector.SelectedValue).deviceInfo;
                var deviceService = await RfcommDeviceService.FromIdAsync(deviceInfo.Id);
                if (deviceService == null) {
                    errorBox.Text = "Device not found!";
                } else {
                    try {
                        connect.Content = "Connecting...";
                        if (((App)Application.Current).streamSocket != null) ((App)Application.Current).streamSocket.Dispose();
                        ((App)Application.Current).streamSocket = new StreamSocket();
                        await ((App) Application.Current).streamSocket.ConnectAsync(deviceService.ConnectionHostName, deviceService.ConnectionServiceName);
                        this.Frame.Navigate(typeof(DriveControlPage));
                    } catch (Exception ex) {
                        connect.Content = "Connect";
                        errorBox.Text = "Cannot connect bluetooth device:" + ex.Message;
                    }
                }
            }
        }

        private async void reloadDevices() {
            var selector = RfcommDeviceService.GetDeviceSelector(RfcommServiceId.SerialPort);
            var services = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(selector);
            this.deviceSelector.Items.Clear();
            foreach (var service in services) {
                //this.deviceList.Add(service);
                this.deviceSelector.Items.Add(new DeviceListItem(service));
            }
            if (this.deviceSelector.Items.Count > 0) this.deviceSelector.SelectedIndex = 0;
        }
    }

    class DeviceListItem {
        public Windows.Devices.Enumeration.DeviceInformation deviceInfo { get; }


        public DeviceListItem(Windows.Devices.Enumeration.DeviceInformation deviceInfo) {
            this.deviceInfo = deviceInfo;
        }

        public override string ToString() {
            return this.deviceInfo.Name;
        }
    }
}
