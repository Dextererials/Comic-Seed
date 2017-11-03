using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Open_Domain_Comics_Windows_Windows_10_
{
    public class XML
    {
        public async static Task<bool> IsInternet()
        {
            HttpWebRequest request = null;
            HttpWebResponse response = null;
            try
            {
                request = (HttpWebRequest)WebRequest.Create("http://www.google.com");
                response = (HttpWebResponse)await request.GetResponseAsync();
                request.Abort();
                response.Dispose();
                return true;
            }
            catch (Exception ex)
            {
                request.Abort();
                response.Dispose();
                Debug.WriteLine("EXCEPTION: " + ex.Message);
                return false;
            }
        }
        public async static Task<XDocument> Request_XML_Async(Uri url)
        {
            if (await IsInternet())
            {
                try
                {

                    Debug.WriteLine("REQUESTING XML FROM URL:" + url.AbsoluteUri);
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();
                    XDocument novelList;
                    string xmlString = string.Empty;

                    xmlString = new StreamReader(response.GetResponseStream()).ReadToEnd();
                    request.Abort();
                    response.Dispose();


                    try
                    {
                        novelList = XDocument.Parse(xmlString);
                    }
                    catch
                    {
                        try
                        {
                            Debug.WriteLine("IN XML CATCH");
                            HtmlDocument fixMalformation = new HtmlDocument();
                            fixMalformation.OptionAutoCloseOnEnd = true;
                            fixMalformation.OptionCheckSyntax = false;
                            fixMalformation.OptionFixNestedTags = true;
                            fixMalformation.OptionOutputAsXml = true;
                            fixMalformation.LoadHtml(xmlString);
                            xmlString = fixMalformation.DocumentNode.InnerHtml.ToString();
                            fixMalformation.DetectEncodingHtml(xmlString);
                            novelList = XDocument.Parse(xmlString);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("EXCEPTION: " + ex.Message);
                            return null;
                        }


                    }
                    Debug.WriteLine("RETURNING XML");
                    return novelList;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("EXCEPTION: " + ex.Message);
                    return null;
                }

            }
            else
            {
            }
            Debug.WriteLine("XML RETURNING NULL");
            return null;

        }


    }
}
