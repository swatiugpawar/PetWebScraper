using System.Formats.Asn1;
using System.Globalization;
using System.Xml;
using HtmlAgilityPack;
using System.Globalization;
using CsvHelper;
using System.Collections.Concurrent;
using System.Text.Json;
using System.ComponentModel.DataAnnotations;

//var builder = WebApplication.CreateBuilder(args);
//var app = builder.Build();

//app.MapGet("/", () => "Hello World!");

//app.Run();

namespace SimpleWebScraper
{
    public class Program
    {
        // defining a custom class to store 
        // the scraped data 
        public class PetProduct
        {
            public string? StoreId { get; set; }
            public string? StoreName { get; set; }
            public string? StoreWebUrl { get; set; }
            public string? Address { get; set; }
            public string? City { get; set; }
            public string? PostalCode { get; set; }
            public string? StateCode { get; set; }
            public string? Longitude { get; set; }
            public string? Latitude { get; set; }
            public string? CountryCode { get; set; }
            public string? Distance { get; set; }
            public string? Phone { get; set; }
        }
        public static void Main()
        {
            // initializing HAP 
            var web = new HtmlWeb();
            // setting a global User-Agent header 
            web.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/109.0.0.0 Safari/537.36";
            // creating the list that will keep the scraped data 

            var petProducts = new List<PetProduct>();
            // the URL of the first pagination web page 
            var firstPageToScrape = "https://www.1800petmeds.com/vetdirectory?horizontalView=true";
            // the list of pages discovered during the crawling task 
            var pagesDiscovered = new List<string> { firstPageToScrape };
            // the list of pages that remains to be scraped 
            var pagesToScrape = new Queue<string>();
            // initializing the list with firstPageToScrape 
            pagesToScrape.Enqueue(firstPageToScrape);
            // current crawling iteration 
            int i = 1;
            // the maximum number of pages to scrape before stopping 
            int limit = 5;
            // until there is a page to scrape or limit is hit 
            while (pagesToScrape.Count != 0 && i < limit)
            {
                // getting the current page to scrape from the queue 
                var currentPage = pagesToScrape.Dequeue();
                // loading the page 
                var currentDocument = web.Load(currentPage);
                // selecting the list of pagination HTML elements 
                var paginationHTMLElements = currentDocument.DocumentNode.QuerySelectorAll("a.page-numbers");
                // to avoid visiting a page twice 
                foreach (var paginationHTMLElement in paginationHTMLElements)
                {
                    // extracting the current pagination URL 
                    var newPaginationLink = paginationHTMLElement.Attributes["href"].Value;
                    // if the page discovered is new 
                    if (!pagesDiscovered.Contains(newPaginationLink))
                    {
                        // if the page discovered needs to be scraped 
                        if (!pagesToScrape.Contains(newPaginationLink))
                        {
                            pagesToScrape.Enqueue(newPaginationLink);
                        }
                        pagesDiscovered.Add(newPaginationLink);
                    }
                }
                // getting the list of HTML product nodes 
                var productHTMLElements = currentDocument.DocumentNode.QuerySelectorAll(".results .row .card-body .select-store-input");
                var phone= currentDocument.DocumentNode.QuerySelectorAll(".results .row .card-body");
                var storeUrl = phone.QuerySelectorAll(".store-name a");
                var recordCount = 0;
                // iterating over the list of product HTML elements 
                foreach (var productHTMLElement in productHTMLElements)
                { 
                    // scraping logic 
                    var Json = productHTMLElement.Attributes["data-store-info"].Value.Replace("&quot;", @"""");
                    JsonDocument doc=JsonDocument.Parse(Json);
                    JsonElement root=doc.RootElement;

                    var StoreId = root.GetProperty("ID").ToString(); 
                    var StoreName = root.GetProperty("name").ToString();
                    var StoreWebUrl = storeUrl[recordCount].Attributes["href"].Value;
                    var Phone = phone[recordCount].Attributes["id"].Value;
                    var Address = root.GetProperty("address1").ToString();
                    var City= root.GetProperty("city").ToString();
                    var PostalCode = root.GetProperty("postalCode").ToString();
                    var StateCode= root.GetProperty("stateCode").ToString();
                    var Longitude = root.GetProperty("longitude").ToString();
                    var Latitude = root.GetProperty("latitude").ToString();
                    var CountryCode =root.GetProperty("countryCode").ToString();
                    var Distance =root.GetProperty("distance").ToString();

                    //var Phone = currentDocument.DocumentNode.QuerySelectorAll(".results .row .card-body");//root.GetProperty("ID").ToString();
                    var Product = new PetProduct() { 
                        StoreId = StoreId, 
                        StoreName = StoreName,
                        StoreWebUrl = StoreWebUrl,
                        Address=Address.Replace("\n", String.Empty),
                        City=City,
                        PostalCode=PostalCode,
                        StateCode=StateCode,
                        Longitude=Longitude,
                        Latitude=Latitude,
                        CountryCode=CountryCode,
                        Distance=Distance,
                        Phone=Phone,
                    };
                    petProducts.Add(Product);
                    recordCount++;
                }


                // incrementing the crawling counter 
                i++;
            }
            // opening the CSV stream reader 
            using (var writer = new StreamWriter("pet-products.csv"))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                // populating the CSV file 
                csv.WriteRecords(petProducts);
            }
        }
    }
}