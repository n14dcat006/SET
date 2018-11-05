using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Message
{
    public class ForwardAuthorizationResponse
    {
        private string response;
        private string signedResponse;
        private string issuerCertificate;
        public ForwardAuthorizationResponse(string TransID, int maKQ, string KQ, string issuerPrivateKey, string issuerCert)
        {
            Common c = new Common();
            response = TransID + ":" + maKQ + ":" + KQ;
            signedResponse = c.Sign(issuerPrivateKey, response);
            issuerCertificate = issuerCert;
        }
        public ForwardAuthorizationResponse(string message, string issuerPrivateKey, string issuerCert)
        {
            Common c = new Common();
            response = message;
            signedResponse = c.Sign(issuerPrivateKey, response);
            issuerCertificate = issuerCert;
        }
        public string ToMessage()
        {
            return response + "-" + signedResponse + "-" + issuerCertificate;
        }
    }
}
