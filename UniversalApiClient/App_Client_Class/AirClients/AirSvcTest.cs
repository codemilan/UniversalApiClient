/*
This class is responsible for getting flights searched result. 
*/
using UniversalApiClient.AirService;
using UniversalApiClient.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniversalApiClient.Client
{
    class AirSvcTest
    {
        public static String MY_APP_NAME = "UAPI";
        private string origin;
        private string destination;

        public AirSvcTest(string source, string dest)
        {
            origin = source;
            destination = dest;
        }

        public AvailabilitySearchRsp Availability()
        {
            AvailabilitySearchReq request = new AvailabilitySearchReq();
            AvailabilitySearchRsp rsp = new AvailabilitySearchRsp();

            request = SetupRequestForSearch(request);

            AirAvailabilitySearchPortTypeClient client = new AirAvailabilitySearchPortTypeClient("AirAvailabilitySearchPort", WsdlService.AIR_ENDPOINT);
            client.ClientCredentials.UserName.UserName = Helper.RetrunUsername();
            client.ClientCredentials.UserName.Password = Helper.ReturnPassword();
            try
            {
                var httpHeaders = Helper.ReturnHttpHeader();
                client.Endpoint.EndpointBehaviors.Add(new HttpHeadersEndpointBehavior(httpHeaders));

                rsp = client.service(null, request);
                // Console.WriteLine(rsp.AirItinerarySolution.Count());
                // Console.WriteLine(rsp.AirSegmentList.Count());
            }
            catch (Exception se)
            {
                Console.WriteLine("Error : " + se.Message);
                client.Abort();
            }

            return rsp;

            //these checks are just sanity that we can make an availability request
            //assertThat(rsp.getAirItinerarySolution().size(), is(not(0)));
            //assertThat(rsp.getAirSegmentList().getAirSegment().size(), is(not(0)));
        }

        private AvailabilitySearchReq SetupRequestForSearch(AvailabilitySearchReq request)
        {
            // TODO Auto-generated method stub

            //add in the tport branch code
            request.TargetBranch = CommonUtility.GetConfigValue(ProjectConstants.G_TARGET_BRANCH);

            //set the GDS via a search modifier
            String[] gds = new String[] { "1G" };
            AirSearchModifiers modifiers = AirReq.CreateModifiersWithProviders(gds);

            AirReq.AddPointOfSale(request, MY_APP_NAME);

            //try to limit the size of the return... not supported by 1G!
            modifiers.MaxSolutions = string.Format("25");
            request.AirSearchModifiers = modifiers;

            //travel is for denver to san fransisco 2 months from now, one week trip
            SearchAirLeg outbound = AirReq.CreateSearchLeg(origin, destination);
            AirReq.AddSearchDepartureDate(outbound, Helper.daysInFuture(3));
            AirReq.AddSearchEconomyPreferred(outbound);

            //coming back
            SearchAirLeg ret = AirReq.CreateSearchLeg(destination, origin);
            AirReq.AddSearchDepartureDate(ret, Helper.daysInFuture(3));
            //put traveller in econ
            AirReq.AddSearchEconomyPreferred(ret);

            request.Items = new SearchAirLeg[2];
            request.Items.SetValue(outbound, 0);
            request.Items.SetValue(ret, 1);

            return request;
        }
    }
}
