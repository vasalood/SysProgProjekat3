using Lab3SysProg;
using Lab3SysProg.Observers;
using Lab3SysProg.Streams;
using System.Net;
using System.Text;
/*Koristeći principe Reaktivnog programiranja i Yelp API, implementirati
* aplikaciju za analizu komentara za kafiće za dati cenovni rang 
* (price parametar) koji se nalaze na datoj lokaciji.
*Za prikupljene komentare implementirati Named Entity Recognition (NER)
*uz pomoć NLTKSharp biblioteke. Prikazati dobijene rezultate.*/
class Program
{
    const string FAVICON = "favicon.ico";

    public static void Main()
    {
        HttpListener listener = new HttpListener();
        string[] prefixes = new string[]
        {
             "http://localhost:5050/",
             "http://127.0.0.1:5050/",
        };



        foreach (string prefix in prefixes)
        {
            listener.Prefixes.Add(prefix);
        }



        listener.Start();



        while (true)
        {
            HttpListenerContext context = listener.GetContext();
            ProcessRequest(context);
        }



    }
    static async void ProcessRequest(HttpListenerContext context)
    {
        await Task.Run(async () =>
        {



            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;



            try
            {
                string requestUrl = request.RawUrl ?? "/";
                requestUrl = requestUrl.TrimStart('/');
                requestUrl = requestUrl.Replace("%20", " ");

                if (requestUrl == FAVICON)
                    return;
                if (requestUrl.StartsWith("analiza/"))
                {
                    string lokacija;
                    requestUrl = requestUrl.Remove(0, "analiza/".Length);
                    int endIndex = requestUrl.IndexOf('/');
                    if (endIndex == -1)
                        endIndex = requestUrl.Length - 1;
                    lokacija = requestUrl[..endIndex];
                    requestUrl = requestUrl.Remove(0, endIndex + 1);
                    var cenaLevelsStrings = requestUrl.Split('_');
                    List<int> cenaLevels = new List<int>();
                    foreach (string cena in cenaLevelsStrings)
                    {
                        cenaLevels.Add(Int32.Parse(cena));
                    }
                    YelpReviewStream stream = new YelpReviewStream(lokacija, cenaLevels.ToArray());
                    if (requestUrl.Last() != '_')
                        requestUrl += '_';
                    string cene = requestUrl.Replace("_", ", ");
                    cene = cene.Remove(cene.Length - 2, 2);
                    string[] categories = new string[] { "a", "n", "j", "v" };
                    string[] categories2 = new string[] { "a", "p", "c", "i", "t" };
                    string[] categories3 = new string[] { "d", "x", "r", "undefined" };
                    StringRefWrapper wrapper1, wrapper2, wrapper3;
                    wrapper1 = new();
                    wrapper2 = new();
                    wrapper3 = new();
                    YelpReviewObserver subscriber1 =
                    new YelpReviewObserver($"Observer1",
                    lokacija, categories, wrapper1, cenaLevels.ToArray());
                    YelpReviewObserver subscriber2 =
                    new YelpReviewObserver($"Observer2",
                    lokacija, categories2, wrapper2, cenaLevels.ToArray());
                    YelpReviewObserver subscriber3 =
                    new YelpReviewObserver($"Observer3",
                    lokacija, categories3, wrapper3, cenaLevels.ToArray());



                    /*                    var sub1 = stream.SubscribeAndObserveOnScheduler(subscriber1);
                                        var sub2 = stream.SubscribeAndObserveOnScheduler(subscriber2);
                                        var sub3 = stream.SubscribeAndObserveOnScheduler(subscriber3);*/



                    var proxy = stream.GetProxy();
                    var sub1 = proxy.Subscribe(subscriber1);
                    var sub2 = proxy.Subscribe(subscriber2);
                    var sub3 = proxy.Subscribe(subscriber3);
                    await stream.GetReviews();
                    sub1.Dispose();
                    sub2.Dispose();
                    sub3.Dispose();



                    response.StatusCode = 200;
                    response.StatusDescription = "OK";
                    string responseString = $"<html><head><title>OK - 200</title></head><body>" +
                    $"{wrapper1.Value}{wrapper2.Value}{wrapper3.Value}</body></html>";
                    byte[] responseBytes = Encoding.UTF8.GetBytes(responseString);
                    response.ContentLength64 = responseBytes.Length;
                    response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
                    response.OutputStream.Close();



                }
                else
                {
                    throw new Exception("Parsing error");
                }



            }
            catch (Exception e)
            {
                Console.WriteLine($"Zahtev nije uspesno obradjen zbog: {e.Message}");



                response.StatusCode = 400;
                response.StatusDescription = "ERROR";
                string responseString = "<html><head><title>400 - ERROR</title></head><body><h1>GRESKA!</h1></body></html>";
                byte[] responseBytes = Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = responseBytes.Length;
                response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
                response.OutputStream.Close();
            }
        });
    }
}