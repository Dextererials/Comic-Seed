using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Open_Domain_Comics.DataModel
{
    public class ComicBookDataItem //one single book
    {

        public string Title { get; private set; }
        public string Date { get; private set; }
        public string BookPages { get; private set; }
        public string IMG_URL { get; private set; }
        public string IMG_Pages { get; private set; }

        public ComicBookDataItem(string title, string date,string pages, string img_URL,string img_Pages)
        {
            this.Title = title;
            this.Date = date;
            this.BookPages = pages;
            this.IMG_URL = img_URL;
            this.IMG_Pages = img_Pages;

        }

    }

    public class ComicBookDataGroup // many books with one name
    {
        public string Title { get; private set; }
        public string IMG_URL { get; private set; }
        public string NumOfBooks { get; private set; }
        public ObservableCollection<ComicBookDataItem> BookList { get; private set; }


    }

   

    public sealed class ComicBookDataSource
    {
    }
}
