using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using System.Collections;
using System.Xml.Linq;
using System.Net;
using System.IO;

using UniversalApiClient.AirService;
using UniversalApiClient.SystemService;
using UniversalApiClient.HotelService;
using UniversalApiClient.Client;
using UniversalApiClient.Utility;

namespace UniversalApiClient.Controllers
{
    public class AirController : Controller
    {
        public ActionResult Search()
        {
            return View();
        }

        public ActionResult AirSearch()
        {
            //
            // PING REQUEST
            //
            String payload = "Demonstration for using Travel Ports Universal api....";
            String someTraceId = "doesntmatter-8176";
            String originApp = "UAPI";

            //set up the request parameters into a PingReq object
            PingReq req = new PingReq();
            req.Payload = payload;
            req.TraceId = someTraceId;

            SystemService.BillingPointOfSaleInfo billSetInfo = new SystemService.BillingPointOfSaleInfo();
            billSetInfo.OriginApplication = originApp;

            req.BillingPointOfSaleInfo = billSetInfo;
            req.TargetBranch = CommonUtility.GetConfigValue(ProjectConstants.G_TARGET_BRANCH);
            //Console.WriteLine(req);
            ViewBag.PingRequest = ObjectDumper.Dump(req);

            var xdoc = TestRest("denver");
            ViewBag.Xdoc = xdoc;

            try
            {
                //run the ping request
                //WSDLService.sysPing.showXML(true);
                SystemPingPortTypeClient client = new SystemPingPortTypeClient("SystemPingPort", WsdlService.SYSTEM_ENDPOINT);
                ViewBag.ClientEndpoint = client.Endpoint;
                client.ClientCredentials.UserName.UserName = Helper.RetrunUsername();
                ViewBag.Username = client.ClientCredentials.UserName.UserName;
                client.ClientCredentials.UserName.Password = Helper.ReturnPassword();
                ViewBag.Password = client.ClientCredentials.UserName.Password;

                var httpHeaders = Helper.ReturnHttpHeader();
                client.Endpoint.EndpointBehaviors.Add(new HttpHeadersEndpointBehavior(httpHeaders));
                ViewBag.ClientReq = client;

                PingRsp rsp = client.service(req);
                ViewBag.PingRsp = ObjectDumper.Dump(rsp);
                //print results.. payload and trace ID are echoed back in response
                //Console.WriteLine(rsp.Payload);
                //Console.WriteLine(rsp.TraceId);
                //Console.WriteLine(rsp.TransactionId);
                ViewBag.RspPayload = ObjectDumper.Dump(rsp.Payload);

                AirportDetails airports = new AirportDetails();
                //Here we are getting the list of airports, we can use it anyway we want
                IDictionary<String, String> airportsList = airports.AllAirportsList();
                ViewBag.AirportList = airportsList;


                //Here we are getting the list of airports in a particular city, we are harcoding the city as New York here
                IDictionary<String, String> airportInCityList = airports.GetAllAiportsFromParticualrCity("New York");


                AirSvcTest airTest = new AirSvcTest();
                airTest.Availability();

                AirLFSTest lfsTest = new AirLFSTest();
                Boolean solutionResult = false; //Change it to true if you want AirPricingSolution, by default it is false
                                                //and will send AirPricePoint in the result
                LowFareSearchRsp lowFareRsp = lfsTest.LowFareShop(solutionResult);

                if (lowFareRsp != null)
                {
                    typeBaseAirSegment[] airSegments = lowFareRsp.AirSegmentList;
                    List<typeBaseAirSegment> pricingSegments = new List<typeBaseAirSegment>();

                    IEnumerator items = lowFareRsp.Items.GetEnumerator();
                    ViewBag.lowFareRsp = ObjectDumper.Dump(lowFareRsp);
                    AirPricingSolution lowestFare = null;
                    AirPricePoint lowest = null;

                    while (items.MoveNext())
                    {
                        if (solutionResult)
                        {
                            AirPricingSolution airPricingSolution = (AirPricingSolution)items.Current;
                            if (lowestFare == null)
                            {
                                lowestFare = airPricingSolution;
                            }
                            else
                            {
                                if (Helper.ConvertToDecimal(lowestFare.TotalPrice) > Helper.ConvertToDecimal(airPricingSolution.TotalPrice))
                                {
                                    lowestFare = airPricingSolution;
                                }
                            }
                        }
                        else
                        {
                            AirPricePointList airPricePointList = (AirPricePointList)items.Current;

                            if (airPricePointList != null)
                            {
                                foreach (var airPricePoint in airPricePointList.AirPricePoint)
                                {
                                    if (lowest == null)
                                    {
                                        lowest = airPricePoint;
                                    }
                                    else
                                    {
                                        if (Helper.ConvertToDecimal(lowest.TotalPrice) > Helper.ConvertToDecimal(airPricePoint.TotalPrice))
                                        {
                                            lowest = airPricePoint;
                                        }
                                    }
                                }
                            }
                        }

                    }
                    if (lowestFare != null)
                    {
                        IEnumerator journeys = lowestFare.Journey.GetEnumerator();
                        while (journeys.MoveNext())
                        {
                            Journey journeyDetails = (Journey)journeys.Current;
                            if (journeyDetails != null)
                            {
                                AirSegmentRef[] segmentRef = journeyDetails.AirSegmentRef;
                                string refKey = segmentRef[0].Key;
                                IEnumerator airSegmentList = airSegments.GetEnumerator();
                                while (airSegmentList.MoveNext())
                                {
                                    typeBaseAirSegment airSeg = (typeBaseAirSegment)airSegmentList.Current;
                                    if (airSeg.Key.CompareTo(refKey) == 0)
                                    {
                                        pricingSegments.Add(airSeg);
                                        break;
                                    }

                                }
                            }
                        }
                    }

                    if (lowest != null)
                    {
                        IEnumerator pricingInfos = lowest.AirPricingInfo.GetEnumerator();

                        while (pricingInfos.MoveNext())
                        {
                            AirPricingInfo priceInfo = (AirPricingInfo)pricingInfos.Current;
                            if (priceInfo != null)
                            {
                                foreach (var flightOption in priceInfo.FlightOptionsList)
                                {
                                    FlightOption option = flightOption;
                                    IEnumerator options = option.Option.GetEnumerator();
                                    if (options.MoveNext())
                                    {
                                        Option opt = (Option)options.Current;
                                        if (opt != null)
                                        {
                                            IEnumerator bookingInfoList = opt.BookingInfo.GetEnumerator();
                                            if (bookingInfoList.MoveNext())
                                            {
                                                BookingInfo bookingInfo = (BookingInfo)bookingInfoList.Current;
                                                if (bookingInfo != null)
                                                {
                                                    String key = bookingInfo.SegmentRef;
                                                    IEnumerator airSegmentList = airSegments.GetEnumerator();
                                                    while (airSegmentList.MoveNext())
                                                    {
                                                        typeBaseAirSegment airSeg = (typeBaseAirSegment)airSegmentList.Current;
                                                        if (airSeg.Key.CompareTo(key) == 0)
                                                        {
                                                            pricingSegments.Add(airSeg);
                                                            break;
                                                        }

                                                    }
                                                }
                                            }
                                        }
                                    }

                                    break;
                                }
                            }
                        }
                    }

                    AirFareDisplay fareDisplay = new AirFareDisplay();
                    AirFareDisplayRsp fareDisplayRsp = fareDisplay.GetAirFareDisplayDetails(pricingSegments);
                    fareDisplay.GetAirFareRules(fareDisplayRsp, null);
                    ViewBag.FareDisplayRsp = ObjectDumper.Dump(fareDisplayRsp);

                    AirPriceRsp priceRsp = AirReq.AirPrice(pricingSegments);
                    ViewBag.priceRsp = ObjectDumper.Dump(priceRsp);
                    AirPricingSolution lowestPrice = null;
                    if (priceRsp != null)
                    {
                        if (priceRsp.AirPriceResult != null)
                        {
                            IEnumerator priceResults = priceRsp.AirPriceResult.GetEnumerator();
                            if (priceResults.MoveNext())//We would take  the first Price Result and will Search for the lowest Price
                            {
                                AirPriceResult result = (AirPriceResult)priceResults.Current;
                                if (result.AirPricingSolution != null)
                                {
                                    IEnumerator priceingSolutions = result.AirPricingSolution.GetEnumerator();
                                    while (priceingSolutions.MoveNext())
                                    {
                                        AirPricingSolution priceSol = (AirPricingSolution)priceingSolutions.Current;
                                        if (lowestPrice == null)
                                        {
                                            lowestPrice = priceSol;
                                        }
                                        else
                                        {
                                            if (Helper.ConvertToDecimal(lowestPrice.TotalPrice) > Helper.ConvertToDecimal(priceSol.TotalPrice))
                                            {
                                                lowestPrice = priceSol;
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        if (lowestPrice != null && priceRsp.AirItinerary != null)
                        {
                            AirBookTest book = new AirBookTest();
                            UniversalService.AirCreateReservationRsp bookResponse = book.AirBook(lowestPrice, priceRsp.AirItinerary);

                            if (bookResponse != null)
                            {
                                var urLocatorCode = bookResponse.UniversalRecord.LocatorCode;
                                Console.WriteLine("Universal Record Locator Code :" + urLocatorCode);
                                ViewBag.UrlLocator = ObjectDumper.Dump(urLocatorCode);
                                UniversalRetrieveTest univ = new UniversalRetrieveTest();
                                UniversalService.UniversalRecordRetrieveRsp univRetRsp = univ.RetrieveRecord(urLocatorCode);
                                ViewBag.DocRsp = ObjectDumper.Dump(AirReq.GetPNR(univRetRsp));
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                //usually only the error message is useful, not the full stack
                //trace, since the stack trace in is your address space...
                Console.WriteLine("Error : " + e.Message);
                ViewBag.ErrorText = ObjectDumper.Dump(e);
            }

            return View();
        }

        [HttpPost]
        public ActionResult HotelSearch(int noOfPeople, int noOfRooms, string hotelLocation, string currentLocation)
        {
            HotelClient hotel = new HotelClient(noOfPeople, noOfRooms, hotelLocation, currentLocation);
            BaseHotelSearchRsp hotelResponse = hotel.HotelAvailabilty();
            ViewBag.HotelSearchRsp = hotelResponse.HotelSearchResult.ToArray();

            return View();
        }

        [NonAction]
        public XDocument TestRest(string city)
        {
            XDocument xdoc = null;
            string baseUrl = "http://api.openweathermap.org/data/2.5/forecast/daily?";

            string url = baseUrl + string.Format("q={0}&mode=xml&units=metric&cnt=7&APPID=3b21bddd81be0b22aa687c67fb2d5f2f", city);

            var client = new WebClient();
            Stream data = client.OpenRead(url);
            if (data != null)
            {
                var reader = new StreamReader(data);
                string response = reader.ReadToEnd();
                if (!string.IsNullOrEmpty(response))
                {
                    xdoc = XDocument.Parse(response);
                }
            }

            return xdoc;
        }
    }
}