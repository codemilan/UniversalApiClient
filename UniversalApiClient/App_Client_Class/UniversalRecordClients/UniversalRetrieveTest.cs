﻿using UniversalApiClient.UniversalService;
using UniversalApiClient.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniversalApiClient.Client
{
    class UniversalRetrieveTest
    {
        internal UniversalRecordRetrieveRsp RetrieveRecord(string urLocatorCode)
        {
            UniversalRecordRetrieveReq univRetReq = new UniversalRecordRetrieveReq();
            UniversalRecordRetrieveRsp univRetRsp;

            univRetReq.TargetBranch = CommonUtility.GetConfigValue(ProjectConstants.G_TARGET_BRANCH);

            BillingPointOfSaleInfo billSaleInfo = new BillingPointOfSaleInfo();
            billSaleInfo.OriginApplication = "UAPI";

            univRetReq.BillingPointOfSaleInfo = billSaleInfo;

            univRetReq.Item = urLocatorCode;

            UniversalRecordRetrieveServicePortTypeClient client = new UniversalRecordRetrieveServicePortTypeClient("UniversalRecordRetrieveServicePort", WsdlService.UNIVERSAL_ENDPOINT);
            client.ClientCredentials.UserName.UserName = Helper.RetrunUsername();
            client.ClientCredentials.UserName.Password = Helper.ReturnPassword();

            try
            {
                var httpHeaders = Helper.ReturnHttpHeader();
                client.Endpoint.EndpointBehaviors.Add(new HttpHeadersEndpointBehavior(httpHeaders));

                univRetRsp = client.service(null, null, univRetReq);
                //Console.WriteLine(lowFareSearchRsp.AirSegmentList.Count());

                if (univRetRsp != null)
                {
                    if (univRetRsp.UniversalRecord.BookingTraveler != null)
                    {
                        IEnumerator travelers = univRetRsp.UniversalRecord.BookingTraveler.GetEnumerator();
                        while (travelers.MoveNext())
                        {
                            var traveler = travelers.Current;
                        }
                    }
                }


                IEnumerator airReservationDetails = univRetRsp.UniversalRecord.Items.GetEnumerator();

                String airLocatorCode;

                while (airReservationDetails.MoveNext())
                {
                    UniversalApiClient.UniversalService.typeBaseAirReservation airReservation = (UniversalApiClient.UniversalService.typeBaseAirReservation)airReservationDetails.Current;
                    airLocatorCode = airReservation.LocatorCode;

                    if (!string.IsNullOrEmpty(airLocatorCode))
                    {
                        AirTicketTest ticketing = new AirTicketTest();
                        ticketing.GenerateTicket(airLocatorCode);
                    }
                }

                return univRetRsp;
            }
            catch (Exception se)
            {
                Console.WriteLine("Error : " + se.Message);
                client.Abort();
                return null;
            }

        }
    }
}
