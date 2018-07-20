using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TestParserSite
{
    public class RequestHelper
    {
        private static HttpWebRequest request;
        private static int numRequests = 0;

        public static string GetPageData(string url)
        {
            numRequests++;
            Console.WriteLine(numRequests);
            Console.WriteLine(url);
            request = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse response = null;
            var err = true;
            do
            {
                try
                {
                    response = (HttpWebResponse)request.GetResponse();
                    err = false;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("err:\n" + ex.Message);
                    if (ex.Message == "The remote server returned an error: (429) Too Many Requests.")
                    {
                        System.Threading.Thread.Sleep(30000);
                        request = (HttpWebRequest)WebRequest.Create(url);
                    }
                    else
                        err = false;
                }
            } while (err);

            if (response == null)
                return "";
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var receiveStream = response.GetResponseStream();
                StreamReader readStream = null;
                if (response.CharacterSet == null)
                {
                    readStream = new StreamReader(receiveStream);
                }
                else
                {
                    readStream = new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));
                }
                var data = readStream.ReadToEnd();
                response.Close();
                readStream.Close();

                return data;
            }
            return "";
        }
    }
}
