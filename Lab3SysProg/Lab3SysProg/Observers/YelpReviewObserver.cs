using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLTKSharp;
using Lab3SysProg.Model;
using System.Runtime.Versioning;
using System.Collections.Concurrent;

namespace Lab3SysProg.Observers
{
    class YelpReviewObserver : IObserver<YelpReview>
    {
        public string Name { get; set; }
        public string Location { get; set; }
        public List<string> WordList { get; set; } = new List<string>();
        public int[] PriceLevels { get; set; }
        public StringRefWrapper Wrapper { get; set; }

        private object _lockobj = new object();
        private static string[] listOfWords = WordBank.ReturnAllWords().Split("\n");
        private static ConcurrentDictionary<string, string> _wordMap = null;
        public Dictionary<string, int> CounterMap = new Dictionary<string, int>();
        public YelpReviewObserver(string name,string location, string[] wordCategories,StringRefWrapper wrapper,
            params int[] priceLevels)
        {
            Name = name;
            Location = location;
            PriceLevels = priceLevels;
            Wrapper = wrapper;
            foreach(var cat in wordCategories)
            {
                CounterMap[cat] = 0;
            }
            if(_wordMap==null)
            {
                lock(_lockobj)
                {
                    if(_wordMap==null)
                    {
                        _wordMap = new ConcurrentDictionary<string, string>(Environment.ProcessorCount*2, 5000);
                        foreach(var stavka in listOfWords)
                        {
                            string[] tmp = stavka.Split("\t");
                            tmp[1] = tmp[1].Replace(" ", "");
                            _wordMap[tmp[1]]= tmp[2];
                        }
                    }
                }
            }
        }
        public void OnCompleted()
        {
            string priceLevels = "";
            foreach(int level in PriceLevels)
            {
                switch(level)
                {
                    case 4:
                        priceLevels += "$$$$, ";
                        break;
                    case 3:
                        priceLevels += "$$$, ";
                        break;
                    case 2:
                        priceLevels += "$$, ";
                        break;
                    case 1:
                        priceLevels += "$, ";
                        break;
                }
            }
            priceLevels=priceLevels.Remove(priceLevels.Length - 2, 2);
            /*            foreach (string word in WordList)
                        {
                            string wordType = "";
                            _wordMap.TryGetValue(word, out wordType);
                            if (string.IsNullOrEmpty(wordType))
                                wordType = "undefined";
                            Console.WriteLine($" {word} : {wordType}");
                        }*/
            string toPrint = $"<h2>{Name}: NER analiza za {Location}," +
                $" i cenovne nivoe <{priceLevels}> je:</h2><ul>";
            foreach(var stavka in CounterMap)
            {
                toPrint += $"<li>{stavka.Key} : {stavka.Value}</li>";
            }
            toPrint += "</ul><br>";
            Wrapper.Value = toPrint;
            //Console.WriteLine(toPrint);
        }

        public void OnError(Exception error)
        {
            Console.WriteLine($"Doslo je do greske, observer :{Name}\n" +
                $"{error.Message}");
        }

        public void OnNext(YelpReview value)
        {
            string[] stringArray = Tokenizer.Tokenize(value.Text);
            foreach(var str in stringArray)
            {
                string cat = "";
                _wordMap.TryGetValue(str, out cat);
                if(String.IsNullOrEmpty(cat))
                {
                    cat = "undefined";
                }
                int counter;
                if(CounterMap.TryGetValue(cat,out counter))
                {
                    ++counter;
                    CounterMap[cat] = counter;
                }
            }
            /*WordList.AddRange(stringArray);*/
            
        }
    }
}
