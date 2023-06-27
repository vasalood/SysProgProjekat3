using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lab3SysProg.Model
{
    class YelpReview
    {
        public string Text { get; set; }

        public YelpReview(string text)
        {
            Text = text;
        }

        public YelpReview()
        {

        }

        //public string Time_Created { get; set; }

        //public string User { get; set; }
    }
}
