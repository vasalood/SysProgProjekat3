using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Lab3SysProg.Model;
using Newtonsoft.Json;

namespace Lab3SysProg.Streams
{
    class YelpReviewStream : IObservable<YelpReview>
    {
        private readonly Subject<YelpReview> _subject = new Subject<YelpReview>();
        private const string apiKey =
        "ezatJaWXz9JfgzWbRS24WMXavqLCfhwiCNBl03yLwKzP7nqEnGCZnNug8Uv3qSc" +
        "YUiwF6e0yQj9uz3Xl1jAGWQM6V1TScR259w0QPceJRxE00hQoyB3M0JgS6eOZZHYx";
        private const string altApiKey = "vkphR3S2WbA61BNdvdxD4Fes5HgglaTsCWbg5CiteCdA4v" +
            "ZG5nYwCaO7ekdceERRE91t4l7b2wHstIraMmJrUpstc_juH5" +
            "pcMHeYokbjVAMwN2gPtNbL_fxG-WSbZHYx";
        public string Location { get; set; }
        public int[] PriceLevels { get; set; }
        private readonly string _businessRequestURL;
        private readonly Func<string, string> _reviewRequestUrlBuilder;
        private HttpClient _client = new HttpClient();
        

        public YelpReviewStream(string location,params int[] priceLevels)
        {
            Location = location;
            PriceLevels = priceLevels;
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", altApiKey);
            _businessRequestURL = $"https://api.yelp.com/v3/businesses/search?location={Location}&categories=cafes";
            foreach(int level in PriceLevels)
            {
                _businessRequestURL += $"&price={level}";
            }
            _reviewRequestUrlBuilder = (businessId) =>
            {
                return $"https://api.yelp.com/v3/businesses/{businessId}/reviews";
            };
        }
        public IDisposable Subscribe(IObserver<YelpReview> observer)
        {
            {
                return _subject.Subscribe(observer);
            }

        }

        public IDisposable SubscribeAndObserveOnScheduler(IObserver<YelpReview> observer)
        {
            return _subject.SubscribeOn(ThreadPoolScheduler.Instance).ObserveOn(Scheduler.CurrentThread).Subscribe(observer);
        }

        public async Task GetReviews()
        {
           /* await Task.Run(async () =>
            {*/
                
                var businessResponse = await _client.GetAsync(_businessRequestURL);
                businessResponse.EnsureSuccessStatusCode();
                string businessStringJson = await businessResponse.Content.ReadAsStringAsync();
                var businessJson = JsonConvert.DeserializeObject<dynamic>(businessStringJson);
                foreach(var business in businessJson.businesses)
                {
                    string id = business.id;
                    var reviewResponse = await _client.GetAsync(_reviewRequestUrlBuilder(id));
                    reviewResponse.EnsureSuccessStatusCode();
                    string reviewStringJson = await reviewResponse.Content.ReadAsStringAsync();
                    var reviewsJson = JsonConvert.DeserializeObject<dynamic>(reviewStringJson).reviews;
                    foreach(var review in reviewsJson)
                    {
                    //Ovde ce ide obrada NER sa NLTKNet
                    YelpReview yelpReview = new YelpReview
                    {
                        Text = review.text
                    };
                        _subject.OnNext(yelpReview);
                    }
                }
                _subject.OnCompleted();
 /*           });*/
        }
    }
}
