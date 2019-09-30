using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using SVGLeasePlanService.Data.DTO;

namespace DataVaultService.OracleUCM
{
    public class OracleUCMHelper
    {
        private string ucmEndpoint;
        private string userName;
        private string password;
        public string LastErrorMessage;
        public OracleUCMHelper(string ucmEndpoint, string userName, string password)
        {
            this.ucmEndpoint = ucmEndpoint;
            this.userName = userName;
            this.password = password;
        }

        public List<OracleUCMFile> ListFiles(string searchMask)
        {
            List<OracleUCMFile> files = new List<OracleUCMFile>();
            int searchRow = 1;
            while (searchRow > 0)
            {
                string p = CreateSearchPacket(String.Format("dOriginalName <SUBSTRING> `{0}`", searchMask), searchRow);
                XmlDocument oResult = new XmlDocument();
                oResult.LoadXml(SendRequest(p));
                searchRow = FillFilesFromResult(oResult, ref files);
            }

            return files;
        }
        public void GetFile(string fieldName, string fieldValue, string outputFile)
        {
            string p = CreateDownloadPacket(fieldName, fieldValue);
            XmlDocument oResult = new XmlDocument();
            SendRequestSaveToFile(p, outputFile);

        }

        private string CreateDownloadPacket(string fieldName, string fieldValue)
        {
            StringBuilder sbRet = new StringBuilder();
            sbRet.AppendLine("<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:ucm=\"http://www.oracle.com/UCM\">");
            sbRet.AppendLine("   <soapenv:Header/>");
            sbRet.AppendLine("   <soapenv:Body>");
            sbRet.AppendLine("      <ucm:GenericRequest webKey=\"cs\">");
            sbRet.AppendLine("         <ucm:Service IdcService=\"GET_FILE\">");
            sbRet.AppendLine("            <ucm:Document>");
            sbRet.AppendLine("               <ucm:Field name=\"#fieldName#\">#fieldValue#</ucm:Field>".Replace("#fieldName#", fieldName).Replace("#fieldValue#", fieldValue));
            sbRet.AppendLine("            </ucm:Document>");
            sbRet.AppendLine("         </ucm:Service>");
            sbRet.AppendLine("      </ucm:GenericRequest>");
            sbRet.AppendLine("   </soapenv:Body>");
            sbRet.AppendLine("</soapenv:Envelope>");

            return sbRet.ToString();
        }

        private int FillFilesFromResult(XmlDocument oResult, ref List<OracleUCMFile> files)
        {
            int searchRow = -1;
            try
            {
                int startRow, endRow, totalRows;

                // Setup Namespace manager
                System.Xml.XmlNamespaceManager oNSMgr = new System.Xml.XmlNamespaceManager(oResult.NameTable);
                //Add the namespaces used in Fusion Opportunity to the XmlNamespaceManager.
                oNSMgr.AddNamespace("env", "http://schemas.xmlsoap.org/soap/envelope/");
                oNSMgr.AddNamespace("ns2", "http://www.oracle.com/UCM");


                startRow = GetIntFromNode(oResult.SelectSingleNode("//ns2:Field[@name='StartRow']", oNSMgr));
                endRow = GetIntFromNode(oResult.SelectSingleNode("//ns2:Field[@name='EndRow']", oNSMgr));
                totalRows = GetIntFromNode(oResult.SelectSingleNode("//ns2:Field[@name='TotalRows']", oNSMgr));
                if (endRow < totalRows)
                {
                    searchRow = endRow + 1;
                }

                System.Xml.XmlNode oResp = oResult.SelectSingleNode("//ns2:ResultSet[@name='SearchResults']", oNSMgr);
                if (oResp != null)
                {
                    foreach (XmlNode xnRow in oResp.ChildNodes)
                    {
                        OracleUCMFile tfile = new OracleUCMFile();
                        tfile.dID = GetIntFromNode(xnRow.SelectSingleNode("./ns2:Field[@name='dID']", oNSMgr));
                        tfile.dCreateDate = GetDateFromNode(xnRow.SelectSingleNode("./ns2:Field[@name='dCreateDate']", oNSMgr));
                        tfile.dDocTitle = xnRow.SelectSingleNode("./ns2:Field[@name='dDocTitle']", oNSMgr).InnerText;
                        tfile.dDocType = xnRow.SelectSingleNode("./ns2:Field[@name='dDocType']", oNSMgr).InnerText;
                        tfile.dOriginalName = xnRow.SelectSingleNode("./ns2:Field[@name='dOriginalName']", oNSMgr).InnerText;
                        tfile.VaultFileSize = GetIntFromNode(xnRow.SelectSingleNode("./ns2:Field[@name='VaultFileSize']", oNSMgr));
                        tfile.dFormat = xnRow.SelectSingleNode("./ns2:Field[@name='dFormat']", oNSMgr).InnerText;
                        files.Add(tfile);
                    }
                }

            }
            catch (Exception ex)
            {
                searchRow = -1; LastErrorMessage = String.Format("Exception during FillFilesFromResult: {0}", ex.Message);
            }
            return searchRow;
        }

        private DateTime? GetDateFromNode(XmlNode xn)
        {
            DateTime? ret = null;
            if (xn != null)
            {
                XmlElement xe = (XmlElement)xn;

                DateTime ret2;
                if (DateTime.TryParse(xn.InnerText, out ret2) == true)
                {
                    ret = ret2;
                }
                else
                {
                    ret = null;
                }

            }
            return ret;
        }

        private int GetIntFromNode(XmlNode xn)
        {
            int rc = -1;
            if (xn != null)
            {
                XmlElement xe = (XmlElement)xn;
                if (int.TryParse(xn.InnerText, out rc) == false)
                {
                    rc = -1;
                }

            }
            return rc;
        }

        private string SendRequest(string packet)
        {
            string stReturn = "";

            //httppost for web objects
            // create the web request with the url to the web service
            HttpWebRequest httpReq = (HttpWebRequest)WebRequest.Create(ucmEndpoint);
            httpReq.Headers.Add("SOAPAction: \"\"");
            httpReq.ContentType = "text/xml;charset=UTF-8";
            httpReq.Accept = "text/xml";
            httpReq.Method = "POST";

            // add your credentials to the HttpWebRequest
            //httpReq.Credentials = new NetworkCredential(stUserID, stPswd);
            httpReq.Credentials = new NetworkCredential(userName, password);

            // Build XML request
            XmlDocument oRequest = new XmlDocument();
            oRequest.LoadXml(packet);

            // Setup the Stream
            Stream oReqStream = httpReq.GetRequestStream();
            oRequest.Save(oReqStream);
            oReqStream.Close();

            // To avoid Cert warnings
            System.Net.ServicePointManager.ServerCertificateValidationCallback = (senderX, certificate, chain, sslPolicyErrors) => { return true; };

            // Send the request & get response
            WebResponse response = httpReq.GetResponse();
            try
            {
                HttpWebResponse oTempResponse = (HttpWebResponse)response;
                System.Net.HttpStatusCode oStatusCode = oTempResponse.StatusCode;
                int iLoop = 1;
                // If we're not OK, try again 3 more times
                while (oStatusCode != HttpStatusCode.OK && iLoop < 3)
                {
                    // Delay 2 minutes (120 milliseconds) and try again...
                    System.Threading.Thread.Sleep(120000);

                    // Store new values
                    iLoop++;
                    response = httpReq.GetResponse();
                    oTempResponse = (HttpWebResponse)response;
                    oStatusCode = oTempResponse.StatusCode;
                }

                // If we eventually got something, process it.
                if (oStatusCode == HttpStatusCode.OK)
                {
                    using (StreamReader rd = new StreamReader(response.GetResponseStream()))
                    {
                        stReturn = rd.ReadToEnd();
                        rd.Close();

                        // If it was mutli-part...
                        if (oTempResponse.ContentType.Contains("multipart"))
                        {
                            // find the xml envelope
                            stReturn = stReturn.Substring(stReturn.IndexOf("<?xml version="), stReturn.IndexOf("</env:Envelope>") + 15 - stReturn.IndexOf("<?xml version="));
                        }
                    }
                }
                else
                {
                    throw new Exception("Bad response received: '" + oTempResponse.StatusCode.ToString() + "' from request: " + packet);
                }
            }
            finally
            {
                if (response != null)
                {
                    ((IDisposable)response).Dispose();
                }
            }
            // Return the value
            return stReturn;
        }

        //This method is custom written to save one and only one attachment from a UCM response.  It only works for text files.  To work with binary files it needs to be enhanced
        private void SendRequestSaveToFile(string packet, string fileName)
        {
            //httppost for web objects
            // create the web request with the url to the web service
            HttpWebRequest httpReq = (HttpWebRequest)WebRequest.Create(ucmEndpoint);
            httpReq.Headers.Add("SOAPAction: \"\"");
            httpReq.ContentType = "text/xml;charset=UTF-8";
            httpReq.Accept = "text/xml";
            httpReq.Method = "POST";

            // add your credentials to the HttpWebRequest
            //httpReq.Credentials = new NetworkCredential(stUserID, stPswd);
            httpReq.Credentials = new NetworkCredential(userName, password);

            // Build XML request
            XmlDocument oRequest = new XmlDocument();
            oRequest.LoadXml(packet);

            // Setup the Stream
            Stream oReqStream = httpReq.GetRequestStream();
            oRequest.Save(oReqStream);
            oReqStream.Close();

            // To avoid Cert warnings
            System.Net.ServicePointManager.ServerCertificateValidationCallback = (senderX, certificate, chain, sslPolicyErrors) => { return true; };

            // Send the request & get response
            WebResponse response = httpReq.GetResponse();
            try
            {
                HttpWebResponse oTempResponse = (HttpWebResponse)response;
                System.Net.HttpStatusCode oStatusCode = oTempResponse.StatusCode;
                int iLoop = 1;
                // If we're not OK, try again 3 more times
                while (oStatusCode != HttpStatusCode.OK && iLoop < 3)
                {
                    // Delay 2 minutes (120 milliseconds) and try again...
                    System.Threading.Thread.Sleep(120000);

                    // Store new values
                    iLoop++;
                    response = httpReq.GetResponse();
                    oTempResponse = (HttpWebResponse)response;
                    oStatusCode = oTempResponse.StatusCode;
                }

                // If we eventually got something, process it.
                if (oStatusCode == HttpStatusCode.OK)
                {
                    if (oTempResponse.ContentType.Contains("multipart"))
                    {
                        if (File.Exists(fileName)) File.Delete(fileName);
                        using (StreamWriter swOut = new StreamWriter(fileName, false))
                        {
                            using (StreamReader rd = new StreamReader(response.GetResponseStream()))
                            {
                                int messagePart = 0;
                                while (rd.EndOfStream == false)
                                {
                                    string line = rd.ReadLine();
                                    if (line.StartsWith("------="))
                                    {
                                        messagePart++;
                                    }
                                    else
                                    {
                                        if (messagePart == 2)
                                        {
                                            if (line.StartsWith("Content-ID:"))
                                            {
                                                line = rd.ReadLine(); //Next line is blank, just move past it
                                                string lastLine = null;
                                                while (true)
                                                {
                                                    line = rd.ReadLine();
                                                    if (line == null || line.StartsWith("------=")) //End of attachment
                                                    {
                                                        messagePart++;
                                                        break;
                                                    }

                                                    if (lastLine != null)
                                                        swOut.WriteLine(lastLine);

                                                    lastLine = line;
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            swOut.Close();
                        }
                    }
                    else
                    {
                        throw new Exception("Bad response received: Expected multipart ContentType from request: " + packet);
                    }
                }
                else
                {
                    throw new Exception("Bad response received: '" + oTempResponse.StatusCode.ToString() + "' from request: " + packet);
                }
            }
            finally
            {
                if (response != null)
                {
                    ((IDisposable)response).Dispose();
                }
            }
        }

        private string CreateSearchPacket(string queryText, int startRow)
        {
            StringBuilder ret = new StringBuilder();
            queryText = HttpUtility.HtmlEncode(queryText);
            ret.AppendLine("<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:ucm=\"http://www.oracle.com/UCM\">");
            ret.AppendLine("    <soapenv:Header/>");
            ret.AppendLine("    <soapenv:Body>");
            ret.AppendLine("        <ucm:GenericRequest webKey=\"cs\">");
            ret.AppendLine("            <ucm:Service IdcService=\"GET_SEARCH_RESULTS\">");
            ret.AppendLine("                <ucm:Document>");
            ret.AppendLine("                    <ucm:Field name=\"StartRow\">#startRow#</ucm:Field>".Replace("#startRow#", startRow.ToString()));
            ret.AppendLine("                    <ucm:Field name=\"QueryText\">#queryText#</ucm:Field>".Replace("#queryText#", queryText));
            ret.AppendLine("                </ucm:Document>");
            ret.AppendLine("            </ucm:Service>");
            ret.AppendLine("        </ucm:GenericRequest>");
            ret.AppendLine("    </soapenv:Body>");
            ret.AppendLine("</soapenv:Envelope>");

            return ret.ToString();
        }
    }
   
}
