using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Data.Json;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Open_Domain_Comics_Windows_Windows_10_
{

    public class ComicBooksDataItem
    {
        public string UniqueId { get; private set; }
        public string Title { get; private set; }
        public string CoverIMG_URL { get; private set; }
        public string Max_IMG_Number { get; private set; }
        public string IMG_FileType { get; private set; }
        public string Content_IMG_URL { get; private set; }

        public ComicBooksDataItem(string uniqueid, string title, string coverIMGURL, string content_IMG_URL, string max_img_Number, string fileExtention)
        {
            this.UniqueId = uniqueid;
            this.Title = title;
            this.CoverIMG_URL = coverIMGURL;
            this.Content_IMG_URL = content_IMG_URL;
            this.Max_IMG_Number = max_img_Number;
            this.IMG_FileType = fileExtention;
        }
        public ObservableCollection<ImageSource> GetImageSourceList
        {
            get
            {
                ObservableCollection<ImageSource> imgSource = new ObservableCollection<ImageSource>();

                for (int i = 0; i < int.Parse(this.Max_IMG_Number); i++)
                {
                    imgSource.Add(new BitmapImage(new Uri(this.Content_IMG_URL + i + this.IMG_FileType)));
                }
                return imgSource;
            }

        }



        public ImageSource GetImageSource
        {
            get
            {
                return new BitmapImage(new Uri(this.Content_IMG_URL + 0 + this.IMG_FileType));

            }
        }
    }
    public class ComicBooksGroup // this is a group of many books with the same name, publisher
    {

        public string UniqueId { get; private set; }
        public string Title { get; private set; }
        public string IMG_URL { get; private set; }
        public ObservableCollection<ComicBookTitleSeries> Items { get; set; }

        public ComicBooksGroup()
        {

        }
        public ComicBooksGroup(string uniqueid, string title, string img_URL)
        {
            this.UniqueId = uniqueid;
            this.Title = title;
            this.IMG_URL = img_URL;
            this.Items = new ObservableCollection<ComicBookTitleSeries>();
        }

        public ImageSource GetImageSource
        {
            get
            {
                return new BitmapImage(new Uri(this.IMG_URL));
            }
        }

        public async Task<ComicBookTitleSeries> GetGroupAsync(string uniqueId)
        {

            var matches = this.Items.Where((g) => g.UniqueId.Equals(uniqueId));
            if (matches.Count() == 1) return matches.First();
            return null;
        }

        public override string ToString()
        {
            return this.Title;
        }
    }
    public class ComicBookTitleSeries
    {
        public string UniqueId { get; private set; }
        public string Title { get; private set; }
        public string ImgPath { get; private set; }
        public string Publisher { get; private set; }
        public string NumberOfBooks { get; private set; }
        public ObservableCollection<ComicBooksDataItem> Items { get; private set; }

        public ComicBookTitleSeries(string uniqeid, string title, string imgpath, string publisher, string numofBooks)
        {
            this.UniqueId = uniqeid;
            this.Title = title;
            this.ImgPath = imgpath;
            this.Publisher = publisher;
            this.NumberOfBooks = numofBooks;
            this.Items = new ObservableCollection<ComicBooksDataItem>();
        }

        public ImageSource GetImageSource
        {
            get
            {
                return new BitmapImage(new Uri(this.ImgPath));
            }
        }
    }
    public sealed class ComicBooksDataSource
    {
        private static ComicBooksDataSource _comicDataSource = new ComicBooksDataSource();
        private ObservableCollection<ComicBooksGroup> _groups = new ObservableCollection<ComicBooksGroup>();
        public ObservableCollection<ComicBooksGroup> Groups
        {
            get { return this._groups; }
        }

        public async static void Create_JsonFile_From_Website()
        {
            //takes about 750 minutes
            Debug.WriteLine("STARTED");

            Debug.WriteLine("CREATING COMICSOURCE");
            ComicBooksDataSource comicSource = new ComicBooksDataSource();

            XDocument categoryPageXML = await XML.Request_XML_Async(new Uri("http://comicbookplus.com/?cbplus=categories"));

            var comicCategoryList = categoryPageXML.Descendants("div").Where(x => x.Attribute("class") != null && x.Attribute("class").Value == "mainbody").Descendants("div").Where(x => x.Attribute("class") != null && x.Attribute("class").Value == "i").ToList();
            Debug.WriteLine("CREATED COMIC SOURCE AND CATEGORY LIST");
            int categoryCounter = 1;

            foreach (var oneCategory in comicCategoryList)
            {
                try
                {
                    Debug.WriteLine(oneCategory + " STARTED");

                    string category = HtmlAgilityPack.HtmlEntity.DeEntitize(oneCategory.Descendants("h2").Where(x => x.Attribute("class") != null && x.Attribute("class").Value == "j").Descendants("a").FirstOrDefault().Value);
                    string imgPath = oneCategory.Descendants("img").FirstOrDefault().Attribute("src").Value;
                    string moreLink = "http://comicbookplus.com/" + oneCategory.Descendants("h2").Where(x => x.Attribute("class") != null && x.Attribute("class").Value == "j").Descendants("a").FirstOrDefault().Attribute("href").Value;


                    Debug.WriteLine(oneCategory + " STRING DONE");
                    XDocument categoryComicBookXML = await XML.Request_XML_Async(new Uri(moreLink));

                    //get the maximum page number and store it into a string 
                    int maxPageNum = 1;
                    try
                    {
                        string pageNumHolder = categoryComicBookXML.Descendants("h2").Where(x => x.Attribute("class") != null && x.Attribute("class").Value == "cbph2").FirstOrDefault().Value.Replace("(Page 1 of ", "").Replace(")", "").Replace(" ", "");
                        maxPageNum = int.Parse(pageNumHolder);
                    }
                    catch (Exception ex)
                    {
                        //there is no page numbers available
                        Debug.WriteLine("EXCEPTION: " + ex.Message);
                    }


                    ComicBooksGroup comicGroup = new ComicBooksGroup("Group-" + categoryCounter, category, imgPath);
                    Debug.WriteLine("CREATED COMICBOOKSGRUOP");
                    // go through all the pages in that category
                    for (int i = 0; i < maxPageNum; i++)
                    {
                        try
                        {
                            string categoryPageURL = moreLink + "_l_n_" + i + "#topcbp";

                            Debug.WriteLine("STARTED XML REQUEST ON CATEGORY:" + category + "ON PAGE:" + i);
                            XDocument comicPageXML = await XML.Request_XML_Async(new Uri(categoryPageURL));
                            Debug.WriteLine("RECIVED REQUEST ON CATEGORY:" + category + "ON PAGE:" + i);
                            //create a list of all the div conatiners because there could be multiple containers
                            var comicBookList = comicPageXML.Descendants("div").Where(x => x.Attribute("class") != null && x.Attribute("class").Value == "cbpLline").ToList();

                            ObservableCollection<ComicBooksGroup> comicDataGroup = new ObservableCollection<ComicBooksGroup>();

                            Debug.WriteLine("CREATED COMICDATAGROUP ON CATEGORY:" + category + "ON PAGE:" + i);


                            Debug.WriteLine("STARTING TO GET BOOKS LIST ON CATEGORY:" + category + "ON PAGE:" + i);
                            int comicBookTitleCounter = 1;
                            foreach (var oneContainer in comicBookList)
                            {
                                Debug.WriteLine("STARTED TO GET DIV CONTAINER ON CATEGORY:" + category + "ON PAGE:" + i + " CONTAINER:" + i);
                                // create a list of each book in that container
                                try
                                {

                                    var books = oneContainer.Descendants("div").Where(x => x.Attribute("itemtype") == null).ToList();
                                    Debug.WriteLine("GOT TO GET BOOKS LIST ON CATEGORY:" + category + "ON PAGE:" + i);
                                    foreach (var oneBook in books) // would be items with collection of books
                                    {
                                        try
                                        {
                                            //comicBookTitleSeries
                                            string title = oneBook.Descendants("td").Where(x => x.Attribute("class") != null && x.Attribute("class").Value == "w").Descendants("a").FirstOrDefault().Value;
                                            string publisher = oneBook.Descendants("td").Where(x => x.Attribute("class") != null && x.Attribute("class").Value == "y").FirstOrDefault().Value;
                                            string numOfBooks = oneBook.Descendants("table").Where(x => x.Attribute("class") != null && x.Attribute("class").Value == "t").Descendants("a").FirstOrDefault().Value;
                                            string oneBookImgPath = oneBook.Descendants("td").Where(x => x.Attribute("class") != null && x.Attribute("class").Value == "v").Descendants("img").FirstOrDefault().Attribute("src").Value;
                                            string moreTitleLink = "http://comicbookplus.com/" + oneBook.Descendants("td").Where(x => x.Attribute("class") != null && x.Attribute("class").Value == "w").Descendants("a").Attributes("href").FirstOrDefault().Value;


                                            Debug.WriteLine("GOT BOOK:" + title + "ON CATEGORY:" + category + "ON PAGE:" + i);
                                            //create a new class to hold all the series of books in that title
                                            ComicBookTitleSeries comicTitleSeries = new ComicBookTitleSeries(comicGroup.UniqueId + "Title-" + comicBookTitleCounter, title, oneBookImgPath, publisher, numOfBooks);

                                            Debug.WriteLine("DONE CREATING COMICBOOKTITLESERIES STARTING XML REQUEST");
                                            XDocument collectioOfBooks = await XML.Request_XML_Async(new Uri(moreTitleLink));
                                            Debug.WriteLine("RECIVED TITLE XML REQUEST");

                                            Debug.WriteLine("CREATED COLLECTION ON CATEGORY:" + category + "ON PAGE:" + i);
                                            //collection of series of the same books
                                            List<XElement> seriesBooks = collectioOfBooks.Descendants("tr").Where(x => x.Attribute("class") != null && x.Attribute("class").Value == "overrow").ToList();

                                            Debug.WriteLine("CREATED LIST<XElement> ON CATEGORY:" + category + "ON PAGE:" + i);
                                            //beacuse the list of the series of the books can be in multiple pages, check to make sure that there are no more pages, if so add them to seriesBooks

                                            var additionalPages = collectioOfBooks.Descendants("tr").Where(x => x.Attribute("class") != null && x.Attribute("class").Value == "tablefooter").Descendants("td").Where(x => x.Attribute("class") != null && x.Attribute("class").Value == "textr").Descendants("a").ToList();

                                            Debug.WriteLine("CREATED ADDITIONALPAGES:" + additionalPages.Count + " ON CATEGORY:" + category + "ON PAGE:" + i);
                                            //check other pages for more data
                                            if (additionalPages.Count != 0)
                                            {

                                                Debug.WriteLine("ADDING XELEMENT TO LIST<>  ON CATEGORY:" + category + "ON PAGE:" + i);
                                                foreach (var onePage in additionalPages)
                                                {
                                                    string nextPage = "http://comicbookplus.com/" + onePage.Attribute("href").Value;

                                                    Debug.WriteLine("REQUESTING XML FROM :" + nextPage);
                                                    XDocument additionalCollectioOfBooks = await XML.Request_XML_Async(new Uri(nextPage));
                                                    Debug.WriteLine("DONE REQUESTING XML FROM :" + nextPage);
                                                    List<XElement> additionalList = additionalCollectioOfBooks.Descendants("tr").Where(x => x.Attribute("class") != null && x.Attribute("class").Value == "overrow").ToList();
                                                    foreach (XElement additionalSingleBook in additionalList)
                                                    {
                                                        Debug.WriteLine("COPYING EACH XELEMENT IN LIST<> TO seriesBOOKS  ON CATEGORY:" + category + "ON PAGE:" + i);
                                                        seriesBooks.Add(additionalSingleBook);
                                                        Debug.WriteLine("DONE COPYING EACH XELEMENT IN LIST<> TO seriesBOOKS  ON CATEGORY:" + category + "ON PAGE:" + i);
                                                    }
                                                    Debug.WriteLine("DONE ADDING XELEMENT TO LIST<>  ON CATEGORY:" + category + "ON PAGE:" + i);
                                                }



                                            }

                                            //list for comicBookTitleSeries Items
                                            Debug.WriteLine("CREATED OBSERVABLECOLLECTION<COMICBOOKSDATAITEM>  ON CATEGORY:" + category + "ON PAGE:" + i);
                                            ObservableCollection<ComicBooksDataItem> comicItemList = await CreateComicItemList(seriesBooks, comicGroup.UniqueId);
                                            Debug.WriteLine("DONE CREATED OBSERVABLECOLLECTION<COMICBOOKSDATAITEM>  ON CATEGORY:" + category + "ON PAGE:" + i);

                                            foreach (ComicBooksDataItem oneComicBook in comicItemList)
                                            {
                                                Debug.WriteLine("COPYING COMICBOOKSDATAITEM FROM LIST TO COMICTITLESERIES" + oneComicBook.Title + " ON CATEGORY:" + category + "ON PAGE:" + i);

                                                comicTitleSeries.Items.Add(oneComicBook);

                                            }

                                            Debug.WriteLine("DONE COPYING COMICBOOKSDATAITEM FROM LIST TO COMICTITLESERIES  ON CATEGORY:" + category + "ON PAGE:" + i);
                                            //add the comicTitleSeries to the group

                                            Debug.WriteLine("ADDING COMICTITLESERIES TO COMICSBOOKGROUPS  ON CATEGORY:" + category + "ON PAGE:" + i);
                                            comicGroup.Items.Add(comicTitleSeries);
                                            Debug.WriteLine("DONE ADDING COMICTITLESERIES TO COMICSBOOKGROUPS  ON CATEGORY:" + category + "ON PAGE:" + i);

                                            comicBookTitleCounter++;



                                        }
                                        catch (Exception ex)
                                        {
                                            Debug.WriteLine("EXCEPTION: " + ex.Message);
                                        }




                                    }
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine("EXCEPTION: " + ex.Message);
                                }




                            }

                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("EXCEPTION: " + ex.Message);
                        }






                    }

                    Debug.WriteLine("ADDING COMICBOOKSGROUP TO COMICSBOOKSDATASOURCE  ON CATEGORY:" + category);
                    comicSource.Groups.Add(comicGroup);
                    categoryCounter++;
                    Debug.WriteLine("DONE ADDING COMICBOOKSGROUP TO COMICSBOOKSDATASOURCE  ON CATEGORY:" + category);


                }
                catch (Exception ex)
                {
                    Debug.WriteLine("EXCEPTION: " + ex.Message);
                }


            }

            Debug.WriteLine("*************************IMPORTANT*******************************");

            foreach (ComicBooksGroup comicGroup in comicSource.Groups)
            {
                ObservableCollection<ComicBookTitleSeries> removeList = new ObservableCollection<ComicBookTitleSeries>();
                foreach (ComicBookTitleSeries comicTitle in comicGroup.Items)
                {
                    if (comicTitle.Items.Count == 0)
                    {
                        removeList.Add(comicTitle);
                    }
                }

                foreach (ComicBookTitleSeries co in removeList)
                {
                    comicGroup.Items.Remove(co);
                }
            }

            string jsonContents = JsonConvert.SerializeObject(comicSource);
            StorageFolder folder = Windows.Storage.ApplicationData.Current.LocalFolder;
            StorageFile sampleFile = await folder.CreateFileAsync("BookList.json", CreationCollisionOption.ReplaceExisting);
            await Windows.Storage.FileIO.WriteTextAsync(sampleFile, jsonContents);
        }


        public static async Task<ObservableCollection<ComicBooksDataItem>> CreateComicItemList(List<XElement> seriesBooks, string uniqueId)
        {
            Debug.WriteLine("CREATING OBSERVABLECOLLECTION<COMICBOOKSDATAITEM");
            ObservableCollection<ComicBooksDataItem> comicGroup = new ObservableCollection<ComicBooksDataItem>();
            Debug.WriteLine("DONE CREATING OBSERVABLECOLLECTION<COMICBOOKSDATAITEM");

            int bookCounter = 1;
            foreach (XElement oneSeries in seriesBooks)
            {
                try
                {
                    Debug.WriteLine("STARTING TO CREATE BOOK LIST XML");
                    string seriesTitle = oneSeries.Descendants("span").Where(x => x.Attribute("itemprop") != null && x.Attribute("itemprop").Value == "name").FirstOrDefault().Value;
                    string imgURL = oneSeries.Descendants("img").FirstOrDefault().Attribute("src").Value;
                    string pages = oneSeries.Descendants("td").Where(x => x.Attribute("itemprop") != null && x.Attribute("itemprop").Value == "numberOfPages").FirstOrDefault().Value;
                    string uploader = oneSeries.Descendants("td").Where(x => x.Attribute("itemprop") != null && x.Attribute("itemprop").Value == "editor").Descendants("a").FirstOrDefault().Value;
                    string seriesLink = oneSeries.Descendants("a").Where(x => x.Attribute("itemprop") != null && x.Attribute("itemprop").Value == "url").FirstOrDefault().Attribute("href").Value;

                    Debug.WriteLine("REQUESTING XML" + "CREATING BOOK LIST:" + seriesTitle + "WITH LINK:" + seriesLink);

                    XDocument singleBook = new XDocument();
                    try
                    {
                        Debug.WriteLine("DONE CREATING XDOCUMENT STARTING TO REQUEST XML");
                        singleBook = await XML.Request_XML_Async(new Uri("http://comicbookplus.com/" + seriesLink));
                        Debug.WriteLine("DONE REQUESTING XML");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("EXCEPTION: " + ex.Message);
                    }
                    try
                    {
                        Debug.WriteLine("DONE REQUESTING XML FROM:" + seriesLink);
                        // we have the tile,imgURL,pages,uploader and series link, all we need are the image urls and the maximum image page number
                        string seriesImgURL = singleBook.Descendants("div").Where(x => x.Attribute("class") != null && x.Attribute("class").Value == "viewer").Descendants("img").FirstOrDefault().Attribute("src").Value;
                        // i dont know why but if you use .value instead of .NextNode you will get an empty string
                        string maxImagePage = singleBook.Descendants("select").Where(x => x.Attribute("id") != null && x.Attribute("id").Value == "comicbookpageselect").Descendants("option").Where(x => x.Attribute("value") != null && x.Attribute("value").Value == "0").First().NextNode.ToString().Replace("1 of ", "").Replace("&amp;nbsp;", "");

                        // remove the extention and the intiger from the filePath
                        int removeInt = seriesImgURL.LastIndexOf('/'); //find the last position of the foward slash to remove the intiger
                        string extention = Path.GetExtension(seriesImgURL);// get the fileType Extention
                        string universalPath = seriesImgURL.Remove(removeInt + 1, 1).Replace(extention, "");

                        Debug.WriteLine("CREATING COMICBOOKSDATAITEM: " + seriesTitle + seriesLink);
                        comicGroup.Add(new ComicBooksDataItem(uniqueId + "Item-" + bookCounter, seriesTitle, imgURL, universalPath, maxImagePage, extention));
                        Debug.WriteLine("DONE CREATING BOOK LIST:" + seriesTitle + "WITH LINK:" + seriesLink);
                        bookCounter++;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("EXCEPTION: " + ex.Message);

                    }



                }
                catch (Exception ex)
                {
                    Debug.WriteLine("EXCEPTION:" + ex.Message);
                }

            }


            return comicGroup;
        }




        public static async Task<IEnumerable<ComicBooksGroup>> GetGroupsAsync()
        {
            await _comicDataSource.GetSampleDataAsync();

            return _comicDataSource.Groups;
        }

        public static async Task<ComicBooksGroup> GetGroupAsync(string uniqueId)
        {
            await _comicDataSource.GetSampleDataAsync();
            // Simple linear search is acceptable for small data sets
            var matches = _comicDataSource.Groups.Where((group) => group.UniqueId.Equals(uniqueId));
            if (matches.Count() == 1) return matches.First();
            return null;
        }

        public static async Task<ComicBookTitleSeries> GetItemAsync(string uniqueId)
        {
            await _comicDataSource.GetSampleDataAsync();
            // Simple linear search is acceptable for small data sets
            var matches = _comicDataSource.Groups.SelectMany(group => group.Items).Where((item) => item.UniqueId.Equals(uniqueId));
            if (matches.Count() == 1) return matches.First();
            return null;
        }

        private async Task GetSampleDataAsync()
        {
            if (this._groups.Count != 0)
                return;


            string file = await Get_String_From_WebsiteDatabase();


            JsonObject jsonObject = JsonObject.Parse(file);
            JsonArray jsonArray = jsonObject["Groups"].GetArray();

            foreach (JsonValue groupValue in jsonArray)
            {
                JsonObject groupObject = groupValue.GetObject();
                ComicBooksGroup group = new ComicBooksGroup(groupObject["UniqueId"].GetString(),
                                                            groupObject["Title"].GetString(),
                                                            groupObject["IMG_URL"].GetString());

                foreach (JsonValue itemValue in groupObject["Items"].GetArray())
                {
                    JsonObject itemObject = itemValue.GetObject();
                    string uniqueId = itemObject["UniqueId"].GetString();
                    string title = itemObject["Title"].GetString();
                    string imgPath = itemObject["ImgPath"].GetString();
                    string publisher = itemObject["Publisher"].GetString();
                    string numOfBooks = itemObject["NumberOfBooks"].GetString();
                    ComicBookTitleSeries comicTitle = new ComicBookTitleSeries(uniqueId, title, imgPath, publisher, numOfBooks);



                    foreach (JsonValue item in itemObject["Items"].GetArray())
                    {
                        JsonObject comicListItem = item.GetObject();
                        string itemUniqueId = comicListItem["UniqueId"].GetString();
                        string itemIitle = comicListItem["Title"].GetString();
                        string itemContent_IMG_URL = comicListItem["Content_IMG_URL"].GetString();
                        string itemMax_IMG_Number = comicListItem["Max_IMG_Number"].GetString();
                        string itemIMG_FileType = comicListItem["IMG_FileType"].GetString();
                        string itemCoverIMG_URL = comicListItem["CoverIMG_URL"].GetString();



                        ComicBooksDataItem oneComicItem = new ComicBooksDataItem(itemUniqueId, itemIitle, itemCoverIMG_URL, itemContent_IMG_URL, itemMax_IMG_Number, itemIMG_FileType);

                        comicTitle.Items.Add(oneComicItem);
                    }


                    group.Items.Add(comicTitle);

                }
                this.Groups.Add(group);
            }
        }

        public async Task<string> Get_String_From_WebsiteDatabase()
        {

            try
            {
                StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
                StorageFile storageFile = await storageFolder.GetFileAsync("BookDataList.json");

                return await FileIO.ReadTextAsync(storageFile, Windows.Storage.Streams.UnicodeEncoding.Utf8);

            }
            catch (Exception ex)
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri("https://od.lk/d/ODhfMTQ0MjcwNl8/BookList.json"));
                HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();
                string xmlString = string.Empty;

                xmlString = new StreamReader(response.GetResponseStream()).ReadToEnd();
                response.Dispose();

                StorageFolder myfolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                StorageFile sampleFile = await myfolder.CreateFileAsync("BookDataList.json", CreationCollisionOption.ReplaceExisting);// this line throws an exception
                await FileIO.WriteTextAsync(sampleFile, xmlString);
                return xmlString;
            }



        }
    }
}
