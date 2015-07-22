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
