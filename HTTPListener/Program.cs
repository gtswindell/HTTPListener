/*

Copyright (c) 2015 Glen Swindell

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
 
 */
using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace HTTPListener
{
    class Program
    {

        public async Task Listen(string prefix, int maxConcurrentRequests, CancellationToken token)
        {
            HttpListener listener = new HttpListener();
            try
            {
                listener.Prefixes.Add(prefix);
                listener.Start();

                var requests = new HashSet<Task>();
                for (int i = 0; i < maxConcurrentRequests; i++)
                    requests.Add(listener.GetContextAsync());

                while (!token.IsCancellationRequested)
                {
                    Task t = await Task.WhenAny(requests);
                    requests.Remove(t);

                    if (t is Task<HttpListenerContext>)
                    {
                        var context = (t as Task<HttpListenerContext>).Result;
                        requests.Add(ProcessRequestAsync(context));
                        requests.Add(listener.GetContextAsync());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public async Task ProcessRequestAsync(HttpListenerContext context)
        {
            StreamReader sr = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
            var data = sr.ReadToEnd();
            Console.WriteLine(data);
            string Output = "<html><body><h1>Hello world</h1><div>Time is: " + DateTime.Now.ToString() + "</div></body></html>";
            byte[] bOutput = System.Text.Encoding.UTF8.GetBytes(Output);
            context.Response.ContentType = "text/html";
            context.Response.ContentLength64 = bOutput.Length;
            Stream OutputStream = context.Response.OutputStream;
            OutputStream.Write(bOutput, 0, bOutput.Length);
            OutputStream.Close();
        }        

        static void Main(string[] args)
        {
            CancellationTokenSource token = new CancellationTokenSource();
            Program prg = new Program();
            Task t = prg.Listen("http://localhost:41000/hello/", 2, token.Token);
            Console.WriteLine("Awaiting a request....");
            Console.ReadLine();
            token.Cancel();
        }
    }
}
