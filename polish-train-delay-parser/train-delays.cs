using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
namespace IDCT
{
    /// <summary>
    /// station struct
    /// </summary>
    public struct stacja
    {
        /// <summary>
        /// train id
        /// </summary>
        public string idPociagu;

        /// <summary>
        /// train number
        /// </summary>
        public string numerPociagu;

        /// <summary>
        /// date
        /// </summary>
        public string data;

        /// <summary>
        /// route
        /// </summary>
        public string relacja;

        /// <summary>
        /// operator
        /// </summary>
        public string przewoznik;

        /// <summary>
        /// timetable arrival
        /// </summary>
        public string przyjazdPlanowy;

        /// <summary>
        /// delay
        /// </summary>
        public string opoznienie;
    }

    /// <summary>
    /// train struct (train steps)
    /// </summary>
    public struct pociag
    {
        /// <summary>
        /// station id
        /// </summary>
        public string idStacji;

        /// <summary>
        /// station name
        /// </summary>
        public string nazwaStacji;

        /// <summary>
        /// timetable arrival
        /// </summary>
        public string przyjazdPlanowy;

        /// <summary>
        /// arrival delay
        /// </summary>
        public string przyjazdOpoznienie;

        /// <summary>
        /// timetable departure
        /// </summary>
        public string odjazdPlanoway;

        /// <summary>
        /// departure delay
        /// </summary>
        public string odjazdOpoznienie;

        /// <summary>
        /// status (byl = was; jest = is; bedzie = will be)
        /// </summary>
        public string status;
    }

    /// <summary>
    /// container for train steps with current geo position
    /// </summary>
    public struct ciuchcia
    {
        /// <summary>
        /// train steps list (station list)
        /// </summary>
        public List<pociag> kroki;

        /// <summary>
        /// geoposition
        /// </summary>
        public string llat;

        /// <summary>
        /// geoposition
        /// </summary>
        public string llong;

        /// <summary>
        /// defines if has geoposition
        /// </summary>
        public bool gotPosition;
    }

    public class TrainDelays
    {
        /// <summary>
        /// error event delegate
        /// </summary>
        /// <param name="code">error code to be passed</param>
        /// <param name="message">passed message</param>
        public delegate void ErrorOccured(int code, string message);
        public event ErrorOccured errorOccured;

        /// <summary>
        /// station search results delegate
        /// </summary>
        /// <param name="stacje">dictionary of stations (id, name)</param>
        public delegate void WynikiSzukania(Dictionary<string, string> stacje);
        public event WynikiSzukania wynikiSzukania;

        /// <summary>
        /// results for a station delegate
        /// </summary>
        /// <param name="przyjazdy">list of arrivals</param>
        /// <param name="odjazdy">list of departures</param>
        public delegate void WynikiStacja(List<stacja> przyjazdy, List<stacja> odjazdy);
        public event WynikiStacja wynikiStacja;

        /// <summary>
        /// results for a train delegate
        /// </summary>
        /// <param name="kroki">steps (stations) of the train</param>
        public delegate void WynikiPociag(ciuchcia kroki);
        public event WynikiPociag wynikiPociag;

        /// <summary>
        /// defines if results should be returned in reveresed order (true). Default = false
        /// </summary>
        protected bool odwroconaKolejnosc = false;

        /// <summary>
        /// user agent used for all http calls
        /// </summary>
        protected string userAgent = "Opóźnienia Kolejowe dla Windows Phone // IDCT.pl";

        /// <summary>
        /// setter for odwroconaKolejnosc attribute which defines if results should be returned in reversed order (true)
        /// </summary>
        /// <param name="odwrocona">true = results should be returned in reversed order</param>
        public void kolejnosc(bool odwrocona)
        {
            odwroconaKolejnosc = odwrocona;
        }

        /// <summary>
        /// getter for odwroconaKolejnosc attribute which defines if results should be returned in reversed order (true)
        /// </summary>
        /// <returns>boolean which informs if results are returned in reversed order (true)</returns>
        public bool getKolejnosc()
        {
            return odwroconaKolejnosc;
        }

        /// <summary>
        /// defines if only future train stations should be returned
        /// </summary>
        protected bool tylkoPrzyszle = false;

        /// <summary>
        /// setter for the tylkoPrzyszle variable which defines if only future trains should be returned
        /// </summary>
        /// <param name="przyszle">boolean which informs if out of the results only future stations should be returned</param>
        public void setTylkoPrzyszle(bool przyszle)
        {
            tylkoPrzyszle = przyszle;
        }

        /// <summary>
        /// getter for the tylkoPrzyszle variable
        /// </summary>
        /// <returns>boolean which informs if out of the results only future stations should be returned (true)</returns>
        public bool getTylkoPrzyszle()
        {
            return tylkoPrzyszle;
        }


        /// <summary>
        /// method used to search for a station by name
        /// </summary>
        /// <param name="szukajString">name partial to search for</param>
        /// <param name="silent">defines if method should trigger exceptions: useful to set false for auto-search</param>
        public void szukajStacji(string szukajString, bool silent)
        {
            try
            {
                //first let us get the website with the data
                HttpWebRequest requ = HttpWebRequest.CreateHttp("http://infopasazer.intercity.pl/index_set.php?stacja=" + szukajString + "&t=" + DateTime.Now.Ticks.ToString());

                //in case there is any autoredirect let us follow it
                requ.AllowAutoRedirect = true;

                //it is nice and kind to identify yourself in case somebody would like to praise or ban you
                requ.UserAgent = userAgent;

                //getting the http response
                requ.BeginGetResponse(delegate(IAsyncResult result)
                {
                    try
                    {
                        //assign the response to a variable
                        HttpWebRequest response = (HttpWebRequest)result.AsyncState;

                        //try to create a streamreader on the received stream
                        StreamReader read = new StreamReader(response.EndGetResponse(result).GetResponseStream());

                        //read the whole content to a variable: in the end in should not take a lot of memory
                        string content = read.ReadToEnd();

                        //we won't need the reader anymore
                        read.Close();

                        //on of the greatest bottlenecks: we just parse the website :) so if they change html structure app will start to fail.
                        //try to match the table with the stations
                        Match mtm = Regex.Match(content, "<table[^>].*?contacts[^>].*?>(.*)</table>", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);
                        if (mtm.Success == false)
                        {
                            //if no results and we are not running in silent mode send an error to the delegate to inform the user (or do something else)
                            if (silent != true)
                            {
                                errorOccured(-2, "Brak danych lub ich niepoprawny format.");
                            }
                        }
                        else
                        {
                            //try to match all entries with station id and station name
                            MatchCollection mtms = Regex.Matches(mtm.Groups[1].Value, "<a href=\"index3.php[?]nr_sta[=](.*?)\">(.*?)</a>", RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnoreCase);

                            //new dictionary of stations
                            Dictionary<string, string> stacje = new Dictionary<string, string>();

                            //dictionary of important stations (consiting word "główn": for instance Szczecin Główny or word "central" like "Warszawa Centralna")
                            Dictionary<string, string> wazne = new Dictionary<string, string>();

                            //for every entry in the results
                            foreach (Match entry in mtms)
                            {
                                //assign the station to a proper dictionary
                                if (entry.Groups[2].Value.Trim().ToLower().Contains("główn") || entry.Groups[2].Value.Trim().ToLower().Contains("central"))
                                {
                                    wazne.Add(entry.Groups[1].Value, entry.Groups[2].Value.Trim());
                                }
                                else
                                {
                                    stacje.Add(entry.Groups[1].Value, entry.Groups[2].Value.Trim());
                                }
                            }

                            //add the less important results to the dictionary of the important ones: in the end we want one dictionary just with important stations at the top
                            foreach (string key in stacje.Keys)
                            {
                                wazne.Add(key, stacje[key]);
                            }

                            //call the search resulsts delegate with the stations (results) dictionary
                            wynikiSzukania(wazne);
                        }
                    }
                    catch (Exception)
                    {
                        //error with the stream: most likely memory issues or invalid response. Trigger error delegate if not running in silent mode
                        if (silent != true)
                            errorOccured(-3, "Nie było można pobrać danych.");
                    }
                }, requ);
            }
            catch (Exception)
            {
                //error with the http call: most likely no internet connection. Trigger error delegate if not running in silent mode
                if (silent != true)
                    errorOccured(-1, "Nie było można pobrać danych - czy masz połączenie z siecią Internet?");
            }
        }

        /// <summary>
        /// load results (arrivals and departures with delays) for a station
        /// </summary>
        /// <param name="idstacji">id of the station which can be obtained using the szukajStacji method</param>
        public void loadStacja(string idstacji)
        {
            try
            {
                //create a http request as previosuly: another point of failure - if PKP will change the URL the app will start to fail - YAY!
                HttpWebRequest requ = HttpWebRequest.CreateHttp("http://infopasazer.intercity.pl/index3.php?nr_sta=" + idstacji + "&t=" + DateTime.Now.Ticks.ToString());

                //in case there is any autoredirect let us follow it
                requ.AllowAutoRedirect = true;

                //it is nice and kind to identify yourself in case somebody would like to praise or ban you
                requ.UserAgent = userAgent;

                //let us get the response of the http request
                requ.BeginGetResponse(delegate(IAsyncResult result)
                {
                    try
                    {
                        //create a list of arrivals
                        List<stacja> przyjazdy = new List<stacja>();

                        //create a list of departures
                        List<stacja> odjazdy = new List<stacja>();

                        //assign the response to a variable
                        HttpWebRequest response = (HttpWebRequest)result.AsyncState;

                        //create a reader over the response stream
                        StreamReader read = new StreamReader(response.EndGetResponse(result).GetResponseStream());

                        //read it to a string - in the end it should not be big
                        string content = read.ReadToEnd();

                        //we do not need the reader and the stream any more
                        read.Close();

                        //created a scope here, yet TBH it is not really needed
                        {
                            //try to match the table with the arrivals
                            Match mtm = Regex.Match(content, "<a name='przyjazdy'></a>(.*?)<table[^>].*?contacts[^>].*?>(.*)</table>", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

                            //trigger the error delegate if no data
                            if (mtm.Success == false)
                            {
                                //no results
                                errorOccured(-2, "Brak danych lub ich niepoprawny format.");
                            }
                            else
                            {
                                //try to match all train arrivals on the station
                                MatchCollection mtms = Regex.Matches(mtm.Groups[2].Value, "<td[^>].*?><a href=\"index_pociag.php[?]nr_id[=](.*?)\">(.*?)</a></td><td[^>].*?><a [^>].*?>(.*?)</a></td><td[^>].*?>(.*?)</td><td[^>].*?>(.*?)</td><td[^>].*?>(.*?)</td><td[^>].*?>(.*?)</td></tr>", RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnoreCase);

                                //for every match...
                                foreach (Match entry in mtms)
                                {
                                    //create a new object
                                    stacja stacjaEntry = new stacja();

                                    //html regex groups:
                                    /*
                                     1 - id
                                     2 - nazwapociagu: train name
                                     3 - przewoznik: operator
                                     4 - data: date
                                     5 - relacja: route
                                     6 - planowany czas: time on timetable
                                     7 - opoznienie: delay
                                     */

                                    //those regex replaces could have been done smarter...
                                    stacjaEntry.idPociagu = Regex.Replace(entry.Groups[1].Value, "<[^>].*?>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase).Trim();
                                    stacjaEntry.numerPociagu = Regex.Replace(entry.Groups[2].Value, "<[^>].*?>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase).Trim();
                                    stacjaEntry.przewoznik = Regex.Replace(entry.Groups[3].Value, "<[^>].*?>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase).Trim();
                                    stacjaEntry.data = Regex.Replace(entry.Groups[4].Value, "<[^>].*?>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase).Trim();
                                    stacjaEntry.relacja = Regex.Replace(entry.Groups[5].Value.Replace("<br>", " - "), "<[^>].*?>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase).Trim();
                                    stacjaEntry.przyjazdPlanowy = Regex.Replace(entry.Groups[6].Value, "<[^>].*?>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase).Trim();
                                    stacjaEntry.opoznienie = Regex.Replace(entry.Groups[7].Value, "<[^>].*?>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase).Trim();

                                    //add to the list of arrivals
                                    przyjazdy.Add(stacjaEntry);
                                }
                            }
                        }

                        //second scope for departures: could have been done in a form of simple loop
                        {
                            //try to match the table with the departures
                            Match mtm = Regex.Match(content, "<a name='odjazdy'></a>(.*?)<table[^>].*?contacts[^>].*?>(.*)</table>", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

                            //if no results and not silent trigger the error delegate 
                            if (mtm.Success == false)
                            {
                                //no results
                                errorOccured(-2, "Brak danych lub ich niepoprawny format.");
                            }
                            else
                            {
                                //try to match all train departures on the station
                                MatchCollection mtms = Regex.Matches(mtm.Groups[2].Value, "<td[^>].*?><a href=\"index_pociag.php[?]nr_id[=](.*?)\">(.*?)</a></td><td[^>].*?><a [^>].*?>(.*?)</a></td><td[^>].*?>(.*?)</td><td[^>].*?>(.*?)</td><td[^>].*?>(.*?)</td><td[^>].*?>(.*?)</td></tr>", RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnoreCase);

                                //for every match...
                                foreach (Match entry in mtms)
                                {
                                    //create a new object
                                    stacja stacjaEntry = new stacja();
                                    //html regex groups:
                                    /*
                                     1 - id
                                     2 - nazwapociagu: train name
                                     3 - przewoznik: operator
                                     4 - data: date
                                     5 - relacja: route
                                     6 - planowany czas: time on timetable
                                     7 - opoznienie: delay
                                     */

                                    //those regex replaces could have been done smarter...
                                    stacjaEntry.idPociagu = Regex.Replace(entry.Groups[1].Value, "<[^>].*?>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase).Trim();
                                    stacjaEntry.numerPociagu = Regex.Replace(entry.Groups[2].Value, "<[^>].*?>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase).Trim();
                                    stacjaEntry.przewoznik = Regex.Replace(entry.Groups[3].Value, "<[^>].*?>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase).Trim();
                                    stacjaEntry.data = Regex.Replace(entry.Groups[4].Value, "<[^>].*?>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase).Trim();
                                    stacjaEntry.relacja = Regex.Replace(entry.Groups[5].Value.Replace("<br>", " - "), "<[^>].*?>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase).Trim();
                                    stacjaEntry.przyjazdPlanowy = Regex.Replace(entry.Groups[6].Value, "<[^>].*?>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase).Trim();
                                    stacjaEntry.opoznienie = Regex.Replace(entry.Groups[7].Value, "<[^>].*?>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase).Trim();

                                    //add to the list of departures
                                    odjazdy.Add(stacjaEntry);
                                }
                            }
                        }

                        //call the delegate responsible for handling the station results with created lists of arrivals and departures
                        wynikiStacja(przyjazdy, odjazdy);
                    }
                    catch (Exception)
                    {
                        //most likely memory issue as streams could not be created
                        errorOccured(-1, "Nie było można pobrać danych!");
                    }

                }, requ);
            }
            catch (Exception)
            {
                //most likely no Internet access
                errorOccured(-1, "Nie było można pobrać danych - czy masz połączenie z siecią Internet?");
            }
        }

        /// <summary>
        /// method which loads train details (it's path and delays)
        /// </summary>
        /// <param name="idpociagu">id of the train</param>
        /// <param name="przewoznik">train's operator (used to define if GPS data should be loaded)</param>
        /// <param name="nrpociagu">number of the train (it's name on the list)</param>
        /// <param name="tryGPS">defines if method should to try to load the GPS data for the train</param>
        public void loadPociag(string idpociagu, string przewoznik, string nrpociagu, bool tryGPS = false)
        {
            try
            {
                //create a http request as previosuly: another point of failure - if PKP will change the URL the app will start to fail - YAY!
                HttpWebRequest requ = HttpWebRequest.CreateHttp("http://infopasazer.intercity.pl/index_pociag.php?nr_id=" + idpociagu + "&t=" + DateTime.Now.Ticks.ToString());

                //in case there is any autoredirect let us follow it
                requ.AllowAutoRedirect = true;

                //it is nice and kind to identify yourself in case somebody would like to praise or ban you
                requ.UserAgent = userAgent;

                //let us get the response of the http request
                requ.BeginGetResponse(delegate(IAsyncResult result)
                {
                    try
                    {
                        //assign the response to a variable
                        HttpWebRequest response = (HttpWebRequest)result.AsyncState;

                        //create a stream reader over the stream
                        StreamReader read = new StreamReader(response.EndGetResponse(result).GetResponseStream());

                        //read the whole stream to a string (in the end it is not big)
                        string content = read.ReadToEnd();

                        //close the reader - we do not need it
                        read.Close();

                        //list of train's steps (stations)
                        List<pociag> kroki = new List<pociag>();
                        {
                            //try to load the table which holds the data
                            MatchCollection mtms = Regex.Matches(content, "<tr[^>].*?bgcolor[=]([^>]*?)><td[^>].*?class[=]([^>]*?)>.*?</td><td[^>].*?>.*?</td><td[^>].*?>.*?</td><td[^>].*?><a href='index3.php[?]nr_sta=(.*?)'>(.*?)</a></td><td[^>].*?>(.*?)</td><td[^>].*?>(.*?)</td><td[^>].*?>(.*?)</td><td[^>].*?>(.*?)</td></tr>", RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnoreCase);

                            //trigger the error delegate if no data
                            if (mtms.Count == 0)
                            {
                                errorOccured(-2, "Brak danych lub ich niepoprawny format.");
                            }
                            else
                            {
                                //for every match
                                foreach (Match entry in mtms)
                                {
                                    //create a new "train entry"
                                    pociag pociagEntry = new pociag();

                                    //a bit confusing part: train status. This should have been done using enum or at least a flag...yet I have used a string var: sorry!
                                    pociagEntry.status = Regex.Replace(entry.Groups[2].Value, "<[^>].*?>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase).Trim();

                                    //if a particular class (loaded line above) consistes "left" it means that the train is currently here
                                    if (pociagEntry.status.Contains("left"))
                                    {
                                        //mark it with status "jest" (= "is")
                                        pociagEntry.status = "jest";
                                    }
                                    else if (pociagEntry.status.Contains("in") && entry.Groups[1].Value == "'#80cc80'") //in a case the class is "in" and particular colour is set: mark as "byl" (= "was")
                                    {
                                        pociagEntry.status = "byl";
                                    }
                                    else // a different scenario means that the train will be here in the future 
                                    {
                                        pociagEntry.status = "bedzie"; //bedzie (= "will be")
                                    }

                                    //station id
                                    pociagEntry.idStacji = Regex.Replace(entry.Groups[3].Value, "<[^>].*?>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase).Trim();

                                    //station name
                                    pociagEntry.nazwaStacji = Regex.Replace(entry.Groups[4].Value, "<[^>].*?>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase).Trim();

                                    //timetable arrival
                                    pociagEntry.przyjazdPlanowy = Regex.Replace(entry.Groups[5].Value, "<[^>].*?>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase).Trim();

                                    //arrival delay
                                    pociagEntry.przyjazdOpoznienie = Regex.Replace(entry.Groups[6].Value, "<[^>].*?>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase).Trim();

                                    //timetable departure
                                    pociagEntry.odjazdPlanoway = Regex.Replace(entry.Groups[7].Value, "<[^>].*?>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase).Trim();

                                    //departure delay
                                    pociagEntry.odjazdOpoznienie = Regex.Replace(entry.Groups[8].Value, "<[^>].*?>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase).Trim();

                                    //depending on the order...
                                    if (odwroconaKolejnosc == false)
                                    {
                                        //if only future stations
                                        if (tylkoPrzyszle == true)
                                        {
                                            //check if train "was" here and if not add it to the list of steps
                                            if (pociagEntry.status != "byl")
                                            {
                                                kroki.Add(pociagEntry);
                                            }
                                        }
                                        else
                                        {
                                            //simply add the entry to the list of steps
                                            kroki.Add(pociagEntry);
                                        }
                                    }
                                    else
                                    {
                                        //if only future stations
                                        if (tylkoPrzyszle == true)
                                        {
                                            //check if train "was" here and if not insert it on the first position
                                            if (pociagEntry.status != "byl")
                                            {
                                                kroki.Insert(0, pociagEntry);
                                            }
                                        }
                                        else
                                        {
                                            //simply add the entry to the first position of the steps list
                                            kroki.Insert(0, pociagEntry);
                                        }
                                    }
                                }
                            }

                        }

                        //this was a part made really quick: people who can understand Polish will see how stupid is the object name (ciuchcia)

                        //create new object
                        ciuchcia n = new ciuchcia();

                        //assume no GPS data
                        n.gotPosition = false;

                        //if tryGPS is true and operator is supported for such operations
                        if ((tryGPS == true) && (przewoznik == "interRegio" || przewoznik == "Przewozy Regionalne" || przewoznik == "PR" || przewoznik == "regioExpress"))
                        {
                            try
                            {
                                //load the GPS data website (again: format and url change will break the lib)
                                HttpWebRequest gpsrequ = HttpWebRequest.CreateHttp(@"http://82.160.42.14/opoznienia/?t=" + DateTime.Now.Ticks.ToString());

                                //again we identify our app for easier banning :)
                                gpsrequ.UserAgent = userAgent;

                                //let us try to get the response...
                                gpsrequ.BeginGetResponse(delegate(IAsyncResult result_inner)
                                {

                                    //get the response stream (technically we should TRY to get it...)
                                    StreamReader reads = new StreamReader(gpsrequ.EndGetResponse(result_inner).GetResponseStream());

                                    //read the whole data
                                    string data = reads.ReadToEnd();

                                    //and close the reader
                                    reads.Close();

                                    //we need only the number of the train and sometimes it has got also a name after a space
                                    string[] samnumer = nrpociagu.Split(' ');

                                    //try to find data for the particular train on the page
                                    Match m = Regex.Match(data, "http[:][/][/]maps[.]google[.]pl/maps[?]q[=]" + samnumer[0] + ".*?[@](.*?)[,](.*?)[&]t");

                                    //if found it
                                    if (m.Success == true)
                                    {
                                        //assigna lat and long values
                                        n.llat = m.Groups[1].Value;
                                        n.llong = m.Groups[2].Value;
                                        n.gotPosition = true;
                                    }

                                    //assign previosuly loaded steps 
                                    n.kroki = kroki;

                                    //call the delegate responsible for handling the data for the user
                                    wynikiPociag(n);
                                }, gpsrequ);
                            }
                            catch (Exception)
                            {
                                //networking issue?
                                errorOccured(-1, "Nie było można pobrać danych - czy masz połączenie z siecią Internet?");
                            }
                        }
                        else
                        {
                            //assigne previously loaded steps
                            n.kroki = kroki;

                            //call the delegate responsible for handling the data for the user
                            wynikiPociag(n);
                        }
                    }
                    catch (Exception)
                    {
                        //was meant really for the networking issue, yet i see it really covered everything....sorry
                        errorOccured(-1, "Nie było można pobrać danych!");
                    }
                }, requ);
            }
            catch (Exception)
            {
                //same as above
                errorOccured(-1, "Nie było można pobrać danych - czy masz połączenie z siecią Internet?");
            }
            //TOO MANY and TOO BIG try...catches!
        }


    }
}
