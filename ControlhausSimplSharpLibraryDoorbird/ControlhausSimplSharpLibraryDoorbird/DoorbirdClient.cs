using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ControlhausSIMPLSharpLibraryDoorbird
{
    //Create and return doorbird url's.
    public class SimplSharpDoorbirdUrls
    {        
        public string createLiveVideoUrl(string userName, string password, string doorbirdHost)
        {
            return ("http://" + doorbirdHost + "/bha-api/video.cgi?http-user=" + userName + "&http-password=" + password);
        }
        public string createLiveImageUrl(string userName, string password, string doorbirdHost)
        {
            return ("http://" + doorbirdHost + "/bha-api/image.cgi?http-user=" + userName + "&http-password=" + password);
        }
        public string createHistoryImageUrl(string userName, string password, string doorbirdHost, int imageIndex)
        {
            return ("http://" + doorbirdHost + "/bha-api/history.cgi?index=" + imageIndex + "http-user=" + userName + "&http-password=" + password);
        }
    }

    //Create a Client object to post get requests to doorbird.
    public class SimplSharpDoorbirdCommunications
    {
        //Define a Client object 
        HttpClient doorbirdClient;
        HttpServer doorbirdServer;
        int debug;
        string userName;
        string password;
        string doorbirdHost;
        string processorHost;
        int serverNotificationPort;
        int doorBellNotificationRelaxationTime;
        int motionSensorNotificationRelaxationTime;
        int doorOpenNotificationRelaxationTime;

        //Create delegate to send unsolicited messages to simpl+
        public delegate void SimplPlusDataHandler(SimplSharpString key, SimplSharpString value);
        public SimplPlusDataHandler onSimplPlusDataHandler { set; get; }

        //Create a new HttpClient object and get this processor's ip address.
        public void initializeClient(
            int debug, 
            string userName, 
            string password, 
            string doorbirdHost, 
            short processorEthernetAdapterId, 
            int serverNotificationPort, 
            int doorBellNotificationRelaxationTime, 
            int motionSensorNotificationRelaxationTime, 
            int doorOpenNotificationRelaxationTime
        )
        {
            this.debug = debug;
            this.userName = userName;
            this.password = password;
            this.doorbirdHost = doorbirdHost;
            this.serverNotificationPort = serverNotificationPort;
            if (doorBellNotificationRelaxationTime >= 10 && doorBellNotificationRelaxationTime <= 10000)
            {
                this.doorBellNotificationRelaxationTime = doorBellNotificationRelaxationTime;
            }
            else
            {
                this.doorBellNotificationRelaxationTime = 10;
            }
            if (motionSensorNotificationRelaxationTime >= 10 && motionSensorNotificationRelaxationTime <= 10000)
            {
                this.motionSensorNotificationRelaxationTime = motionSensorNotificationRelaxationTime;
            }
            else
            {
                this.motionSensorNotificationRelaxationTime = 10;
            }
            if (doorOpenNotificationRelaxationTime >= 10 && doorOpenNotificationRelaxationTime <= 10000)
            {
                this.doorOpenNotificationRelaxationTime = doorOpenNotificationRelaxationTime;
            }
            else
            {
                this.doorOpenNotificationRelaxationTime = 10;
            }

            if (doorbirdClient == null)
            {
                doorbirdClient = new HttpClient();
            }
            this.processorHost = getProcessorIP(processorEthernetAdapterId);
            initializeServer();

            if (this.debug > 0) 
            {
                CrestronConsole.PrintLine("doorbird InitializeClient with userName: {0}, doorbirdhost: {1}, processorHost: {2}, serverNotificationPort: {3}", userName, doorbirdHost, processorHost, serverNotificationPort);
            }
        }

        //Set relaxation times.
        public void setDoorBellNotificationRelaxationTime(int time) 
        {
            doorBellNotificationRelaxationTime = time;
        }

        public void setMotionSensorNotificationRelaxationTime(int time)
        {
            motionSensorNotificationRelaxationTime = time;
        }

        public void setDoorOpenNotificationRelaxationTime(int time)
        {
            doorOpenNotificationRelaxationTime = time;
        }
        
        //Get IPAddress of this control processor.
        //This is used when subscribing to notifications in doorbird.
        public string getProcessorIP(short processorEthernetAdapterId)
        {
            try
            {
                return CrestronEthernetHelper.GetEthernetParameter(CrestronEthernetHelper.ETHERNET_PARAMETER_TO_GET.GET_CURRENT_IP_ADDRESS, processorEthernetAdapterId);
            }
            catch (Exception exception)
            {
                if (debug > 0)
                {
                    CrestronConsole.PrintLine("doorbird exception getProcessorIP:\n" + exception);
                }
                return "0.0.0.0";
            }
        }
        
        //Create requests
        public void request(string request)
        {
            switch (request)
            {
                case "openDoor":
                    get("/bha-api/open-door.cgi");
                    break;
                case "lightOn":
                    get("/bha-api/light-on.cgi");
                    break;
                case "doorBellNotificationRequestOn":
                    get("/bha-api/notification.cgi?url=http://" + processorHost + ":" + serverNotificationPort + "/doorBellNotification&event=doorbell&relaxation=" + doorBellNotificationRelaxationTime + "&subscribe=1");
                    break;                                                        
                case "doorBellNotificationRequestOff":
                    get("/bha-api/notification.cgi?url=http://" + processorHost + ":" + serverNotificationPort + "/doorBellNotification&event=doorbell&relaxation=" + doorBellNotificationRelaxationTime + "&subscribe=0");
                    break;
                case "motionSensorNotificationRequestOn":
                    get("/bha-api/notification.cgi?url=http://" + processorHost + ":" + serverNotificationPort + "/motionSensorNotification&event=motionsensor&relaxation=" + motionSensorNotificationRelaxationTime + "&subscribe=1");
                    break;
                case "motionSensorNotificationRequestOff":
                    get("/bha-api/notification.cgi?url=http://" + processorHost + ":" + serverNotificationPort + "/motionSensorNotification&event=motionsensor&relaxation=" + motionSensorNotificationRelaxationTime + "&subscribe=0");
                    break;
                case "doorOpenNotificationRequestOn":
                    get("/bha-api/notification.cgi?url=http://" + processorHost + ":" + serverNotificationPort + "/doorOpenNotification&event=dooropen&relaxation=" + doorOpenNotificationRelaxationTime + "&subscribe=1");
                    break;
                case "doorOpenNotificationRequestOff":
                    get("/bha-api/notification.cgi?url=http://" + processorHost + ":" + serverNotificationPort + "/doorOpenNotification&event=dooropen&relaxation=" + doorOpenNotificationRelaxationTime + "&subscribe=0");
                    break;
                /*Feedback from these messages is not JSON. Could not figure out how to parse it.
                case "doorbellCheckRequest":
                    get("/bha-api/monitor.cgi?check=doorbell");
                    break;
                case "motionSensorCheckRequest":
                    get("/bha-api/monitor.cgi?check=motionsensor");
                    break;
                case "doorbellMonitorRequest":
                    get("/bha-api/monitor.cgi?ring=doorbell"); 
                    break;
                case "motionSensorMonitorRequest":
                    get("/bha-api/monitor.cgi?ring=motionsensor"); 
                    break;
                 */
                case "liveAudioReceive":
                    get("/bha-api/audio-receive.cgi");
                    break;
                case "liveAudioTransmit":
                    get("/bha-api/audio-transmit.cgi");
                    break;
                case "infoRequest":
                    get("/bha-api/info.cgi");
                    break;
                 
            }
        }
        
        //Dispatch requests
        public void get(string getRequest)
        {
            String requestString = "http://" + doorbirdHost + getRequest + "?http-user=" + userName + "&http-password=" + password;
            HttpClientRequest request = new HttpClientRequest();
            request.Url.Parse(requestString);
            request.RequestType = Crestron.SimplSharp.Net.Http.RequestType.Get;
            try
            {
                if (debug > 0)
                {
                    CrestronConsole.PrintLine("doorbird trying get request:\n" + requestString);             
                }
                
                doorbirdClient.DispatchAsync(request, OnHTTPClientResponseCallback);                
            }
            catch (Exception exception)
            {
                CrestronConsole.PrintLine("doorbird Exception openDoor DispatchAsync:\n" + exception);           
            }                 
        }
       
        //Handle client responses       
        public void OnHTTPClientResponseCallback(HttpClientResponse response, HTTP_CALLBACK_ERROR error)
        {
            if (debug > 0)
            {
                CrestronConsole.PrintLine("\ndoorbird OnHTTPClientResponseCallback: " + response.ContentString);
                if (error > 0)
                {
                    CrestronConsole.PrintLine("doorbird error:\n" + error);
                }
            }
            if (response.ContentString.Length > 0)
            {
                try
                {
                    if (JObject.Parse(response.ContentString) != null)
                    {
                        JObject responseObject = JObject.Parse(response.ContentString);
                        if (responseObject["BHA"] != null)
                        {
                            //Parse {"BHA": {"RETURNCODE": "1","VERSION": [{"FIRMWARE": "000100", "BUILD_NUMBER": "43119","WIFI_MAC_ADDR": "1CCAE37073BE"}]}}            
                            if (responseObject["BHA"]["VERSION"] != null)
                            {
                                if ((JObject)responseObject["BHA"]["VERSION"][0] != null)
                                {
                                    IDictionary<string, JToken> parsedResponseObject = (JObject)responseObject["BHA"]["VERSION"][0];
                                    Dictionary<string, string> responseDictionary = parsedResponseObject.ToDictionary(t => t.Key, t => (string)t.Value);
                                    foreach (KeyValuePair<string, string> responseDictionaryPair in responseDictionary)
                                    {
                                        onSimplPlusDataHandler(responseDictionaryPair.Key, responseDictionaryPair.Value);
                                        if (debug > 0)
                                        {
                                            switch (responseDictionaryPair.Key)
                                            {
                                                case "FIRMWARE":
                                                    CrestronConsole.PrintLine("\r\ndoorbird response FIRMWARE :\r\n" + responseDictionaryPair.Value);
                                                    break;
                                                case "BUILD_NUMBER":
                                                    CrestronConsole.PrintLine("\r\ndoorbird response BUILD_NUMBER :\r\n" + responseDictionaryPair.Value);
                                                    break;
                                                case "WIFI_MAC_ADDR":
                                                    CrestronConsole.PrintLine("\r\ndoorbird response WIFI_MAC_ADDR :\r\n" + responseDictionaryPair.Value);
                                                    break;
                                                default:
                                                    CrestronConsole.PrintLine("\r\ndoorbird no match found for VERSION response :\r\n" + responseDictionaryPair.Key);
                                                    break;
                                            }
                                        }
                                    }
                                }
                            }
                            //Parse {"BHA": { "RETURNCODE": "1", "NOTIFICATIONS": [{"event": "motionsensor","subscribe": "1","url": "http://192.168.1.49/motionSensorNotification","user": "","password": "","relaxation": "10"},{"event": "doorbell","subscribe": "1","url": "http://192.168.1.49/doorBellNotification","user": "","password": "","relaxation": "10"},{"event": "dooropen","subscribe": "1","url": "http://192.168.1.49/doorOpenNotification","user": "","password": "","relaxation": "10"}]}}
                            else if (responseObject["BHA"]["NOTIFICATIONS"] != null)
                            {
                                if ((JObject)responseObject["BHA"]["NOTIFICATIONS"][0] != null)
                                {
                                    foreach (var responseObjectArrayIndex in (responseObject["BHA"]["NOTIFICATIONS"]))
                                    {
                                        IDictionary<string, JToken> parsedResponseObject = (JObject)responseObjectArrayIndex;
                                        Dictionary<string, string> responseDictionary = parsedResponseObject.ToDictionary(t => t.Key, t => (string)t.Value);
                                        foreach (KeyValuePair<string, string> responseDictionaryPair in responseDictionary)
                                        {
                                            onSimplPlusDataHandler(responseDictionaryPair.Key, responseDictionaryPair.Value);
                                            if (debug > 0)
                                            {
                                                switch (responseDictionaryPair.Key)
                                                {
                                                    case "event":
                                                        CrestronConsole.PrintLine("\r\ndoorbird response event :\r\n" + responseDictionaryPair.Value);
                                                        break;
                                                    case "subscribe":
                                                        CrestronConsole.PrintLine("\r\ndoorbird response subscribe :\r\n" + responseDictionaryPair.Value);
                                                        break;
                                                    case "url":
                                                        CrestronConsole.PrintLine("\r\ndoorbird response url :\r\n" + responseDictionaryPair.Value);
                                                        break;
                                                    case "user":
                                                        CrestronConsole.PrintLine("\r\ndoorbird response url :\r\n" + responseDictionaryPair.Value);
                                                        break;
                                                    case "password":
                                                        CrestronConsole.PrintLine("\r\ndoorbird response url :\r\n" + responseDictionaryPair.Value);
                                                        break;
                                                    case "relaxation":
                                                        CrestronConsole.PrintLine("\r\ndoorbird response url :\r\n" + responseDictionaryPair.Value);
                                                        break;
                                                    default:
                                                        CrestronConsole.PrintLine("\r\ndoorbird no match found for VERSION response :\r\n" + responseDictionaryPair.Key);
                                                        break;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception exception)
                {
                    CrestronConsole.PrintLine("doorbird exception OnHTTPClientResponseCallback:\n" + exception);                
                }
            }
        }
        
        //Create a new HttpServer and event handler for doorbird notifications.
        public void initializeServer()
        {
            if (doorbirdServer == null)
            {
                doorbirdServer = new HttpServer();
                doorbirdServer.ServerName = processorHost;
                doorbirdServer.Port = serverNotificationPort;
                doorbirdServer.OnHttpRequest += new OnHttpRequestHandler(OnHTTPServerRequestEventHandler);
                doorbirdServer.Active = true;

            }
            if (this.debug > 0)
            {
                CrestronConsole.PrintLine("doorbird initializeServer with doorbirdHost: {0}, Port: {1}", doorbirdServer.ServerName, doorbirdServer.Port);
                CrestronConsole.PrintLine("doorbird doorbirdServer ");
            }
        }

        public void OnHTTPServerRequestEventHandler(Object sender, OnHttpRequestArgs requestArgs) //requestArgs
        {
            if (this.debug > 0)
            {
                CrestronConsole.Print("doorbird OnHTTPServerRequestEventHandler: " + requestArgs.Request.Path);
            }
            onSimplPlusDataHandler("notifications", requestArgs.Request.Path);
            if (debug > 0)
            {
                switch (requestArgs.Request.Path)
                {
                    case "/doorBellNotification":
                        if (this.debug > 0)
                        {
                            CrestronConsole.Print("doorbird doorBellNotification");
                        }
                        break;
                    case "/motionSensorNotification":
                        if (this.debug > 0)
                        {
                            CrestronConsole.Print("doorbird motionSensorNotification");
                        }
                        break;
                    case "/doorOpenNotification":
                        if (this.debug > 0)
                        {
                            CrestronConsole.Print("doorbird doorOpenNotification");
                        }
                        break;
                    default:
                        if (this.debug > 0)
                        {
                            CrestronConsole.Print("doorbird request not found.");
                        }
                        break;
                }
            }
        }
    }
}