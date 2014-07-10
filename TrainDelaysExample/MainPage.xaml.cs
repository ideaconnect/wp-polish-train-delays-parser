using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using IDCT;
namespace TrainDelaysExample
{
    public partial class MainPage : PhoneApplicationPage
    {
        TrainDelays delaysInstance = new TrainDelays();
        
        // Constructor
        public MainPage()
        {
            InitializeComponent();

            //assign delegates for actions
            delaysInstance.errorOccured += delaysInstance_errorOccured;
            delaysInstance.wynikiSzukania += delaysInstance_wynikiSzukania;
            delaysInstance.wynikiStacja += delaysInstance_wynikiStacja;
            delaysInstance.wynikiPociag += delaysInstance_wynikiPociag;
        }

        void delaysInstance_wynikiPociag(ciuchcia kroki)
        {
            //we output steps of a train in the simplest way:
            //station name \ departure timetable (delay) \ arrival timetable (delay)
            //save station id in "tag" for future use"
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                trainstationslist.Items.Clear();
                foreach (pociag pociag in kroki.kroki)
                {
                    ListBoxItem item = new ListBoxItem();
                    item.Tag = "INITIAL:" + pociag.idStacji;
                    item.Content = pociag.nazwaStacji + "\r\n" + pociag.odjazdPlanoway + " (" + pociag.odjazdOpoznienie + ")\r\n" + pociag.przyjazdPlanowy + " (" + pociag.przyjazdOpoznienie + ")";
                    item.Margin = new Thickness(5, 15, 5, 15);
                    trainstationslist.Items.Add(item);
                }
            });
        }

        void delaysInstance_wynikiStacja(List<stacja> przyjazdy, List<stacja> odjazdy)
        {
            //results for station: for testing we display only arrivals in a format: train number \ route \ timetable arrival (delay)
            //save train id in "tag" for uuse in next actions
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                trainstationslist.Items.Clear();
                foreach (stacja station in przyjazdy)
                {
                    ListBoxItem item = new ListBoxItem();
                    item.Tag = "TRAIN:" + station.idPociagu;
                    item.Content = station.numerPociagu + "\r\n" + station.relacja + "\r\n" + station.przyjazdPlanowy + " (" + station.opoznienie + ")";
                    item.Margin = new Thickness(5, 15, 5, 15);
                    trainstationslist.Items.Add(item);
                }
            });
        }

        void delaysInstance_wynikiSzukania(Dictionary<string, string> stacje)
        {
            //output all stations: save station_id to "tag" so that we could use it for next actions
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                trainstationslist.Items.Clear();
                foreach (string station_id in stacje.Keys)
                {
                    ListBoxItem item = new ListBoxItem();
                    item.Tag = "INITIAL:" + station_id;
                    item.Content = stacje[station_id];
                    item.Margin = new Thickness(5, 15, 5, 15);
                    trainstationslist.Items.Add(item);
                }
            });
        }

        void delaysInstance_errorOccured(int code, string message)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                MessageBox.Show(message);
            });
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            delaysInstance.szukajStacji(station.Text, false);
        }

        private void trainstationslist_DoubleTap(object sender, GestureEventArgs e)
        {
            // based on the data saved in 'tag' we are going to decide if load TRAIN or STATION data
            if (trainstationslist.SelectedItems.Count > 0)
            {
                ListBoxItem item = (ListBoxItem)trainstationslist.SelectedItem;
                string[] tagData = item.Tag.ToString().Split(':');
                switch (tagData[0])
                {
                    case "INITIAL":
                        delaysInstance.loadStacja(tagData[1]);
                        break;
                    case "TRAIN":
                        delaysInstance.loadPociag(tagData[1],"","",false);
                        break;                    
                }
            }
        }
    }
}