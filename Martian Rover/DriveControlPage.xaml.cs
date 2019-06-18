using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Streams;
using Windows.System.Threading;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Leere Seite" wird unter https://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace Martian_Rover {
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class DriveControlPage : Page {
        ThreadPoolTimer timer;

        string[] servoNames = { "Basis", "1. Gelenk", "2. Gelenk", "Greifer", "Test" };

        public DriveControlPage() {
            this.InitializeComponent();
            for (int i = 0; i < 4; i++) {
                var slider = new Slider() {
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(55, 100 + (50 * i), 0, 0),
                    Width = 250,
                    Minimum = 0,
                    Maximum = 180,
                    Name = "servo_" + i.ToString(),
                    Header = servoNames[i]
                };
                slider.ValueChanged += servoSlider_ValueChanged;
                armControls.Children.Add(slider);
            }
        }

        private async void back_Click(object sender, RoutedEventArgs e) {
            ContentDialog confirmDialog = new ContentDialog {
                Title = "Sicher?",
                Content = "Wirklich trennen?",
                PrimaryButtonText = "Ja",
                CloseButtonText = "Nein"
            };
            ContentDialogResult result = await confirmDialog.ShowAsync();
            if (result == ContentDialogResult.Primary) {
                ((App)Application.Current).streamSocket.Dispose();
                this.Frame.Navigate(typeof(MainPage));
            }
        }

        private async void test_Click(object sender, RoutedEventArgs e) {
            await sendCommand("ping");
        }

        private async void forw_Press(object sender, PointerRoutedEventArgs e) {
            await sendCommand("forward");
        }

        private async void backw_Press(object sender, PointerRoutedEventArgs e) {
            await sendCommand("backward");
        }

        private async void left_Press(object sender, PointerRoutedEventArgs e) {
            await sendCommand("left_wheel");
        }

        private async void right_Press(object sender, PointerRoutedEventArgs e) {
            await sendCommand("right_wheel");
        }

        private async void btn_Release(object sender, PointerRoutedEventArgs e) {
            await sendCommand("motor_stop");
        }

        private void Rectangle_PointerPressed(object sender, PointerRoutedEventArgs e) {
            writeToBox("Pressed");
        }

        private void Rectangle_PointerReleased(object sender, PointerRoutedEventArgs e) {
            writeToBox("Released");
        }

        private void control_servo_Click(object sender, RoutedEventArgs e) {
            if (driveControls.Visibility != Visibility.Collapsed) {
                driveControls.Visibility = Visibility.Collapsed;
                armControls.Visibility = Visibility.Visible;
            } else {
                driveControls.Visibility = Visibility.Visible;
                armControls.Visibility = Visibility.Collapsed;
            }
        }

        private void servoSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e) {
            Slider slider = sender as Slider;
            if (slider != null) {
                string servo = slider.Name.Replace("servo_", "");
                if (this.timer != null) this.timer.Cancel();
                this.timer = ThreadPoolTimer.CreateTimer(async (timer) => {
                    await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () => {
                        await sendCommand("servo", servo + "_" + slider.Value);
                    });
                }, new TimeSpan(2000000));
            }
        }

        private async Task<string> sendCommand(string cmd, string args = "") {
            cmd = cmd + "+" + args;
            writeToBox(cmd);
            cmd += "\n";
            try {
                using (var dwriter = new DataWriter(((App)Application.Current).streamSocket.OutputStream)) {
                    // UInt32 len = dwriter.MeasureString(cmd);
                    // dwriter.WriteUInt32(len);
                    dwriter.WriteString(cmd);
                    await dwriter.StoreAsync();
                    await dwriter.FlushAsync();
                    dwriter.DetachStream();
                }
                return await this.receiveResponse();
            } catch (Exception ex) {
                writeToBox("Write error: " + ex.Message);
            }
            return "Error";
        }

        private async Task<string> receiveResponse() {
            try {
                using (var dreader = new DataReader(((App)Application.Current).streamSocket.InputStream)) {
                    string response = "";
                    string c = "";
                    while (c != "\n") {
                        response += c;
                        await dreader.LoadAsync(sizeof(byte));
                        c = dreader.ReadString(1);
                    }
                    dreader.DetachStream();
                    writeToBox(response);
                    return response;
                }
            } catch (Exception ex) {
                writeToBox("Read error: " + ex.Message);
            }
            return "Error";
        }

        private void writeToBox(string text) {
            responseBox.Text += text + "\n"; ;
            responseScroller.ChangeView(0, double.MaxValue, 1);
        }
    }
}
