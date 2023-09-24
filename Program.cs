using System.Text;
using currys_bot;
using OpenQA.Selenium.Chrome;
using HtmlAgilityPack;

class Program
{
    static bool isRunning = false;
    
    // apple watches
    // ipads
    // airpods
    // iphones

    private static List<string> sent = new List<string>();

    private static string[] urls = new[]
    {
        "https://www.currys.co.uk/search-update-grid?cgid=smart-watches-smart-watches-and-fitness-smart-tech&prefn1=brand&prefv1=APPLE&srule=Price%20(Low%20to%20High)&start=0&sz=30&viewtype=listView",
        "https://www.currys.co.uk/search-update-grid?cgid=all-tablets-ipad-tablets-ereaders-computing&prefn1=brand&prefv1=APPLE&srule=Price%20(Low%20to%20High)&start=0&sz=30&viewtype=listView",
        "https://www.currys.co.uk/search-update-grid?cgid=all-headphones-headphones-tv-audio&prefn1=brand&prefv1=APPLE&srule=Price%20(Low%20to%20High)&start=0&sz=30",
        "https://www.currys.co.uk/search-update-grid?cgid=all-mobile-phones-mobile-phones-phones&prefn1=brand&prefv1=APPLE&srule=Price%20(Low%20to%20High)&start=0&sz=30"
    };
    
    //string productUrls = [];
    static async Task Main(string[] args)
    {
        while (true)
        {
            foreach (var pUrl in urls)
            {
                await DoWork(pUrl);
                Console.WriteLine("Function executed. Waiting for the next hour...");
            }
            
            await SendInfo("Completed search not waiting for next routine for ");
            await Task.Delay(TimeSpan.FromMinutes(30)); // Wait for 1 hour
        }
    }

    static void DoWorkCallback(object state)
    {
        Task.Run(() => DoWork(null));
    }

    static async Task DoWork(string url)
    {
        if (isRunning)
            return;
        
        Console.WriteLine("Function executed at: " + DateTime.Now);
        try
        {
            isRunning = true;

            var options = new ChromeOptions();
            options.AddArguments("--log-level=3");
            var driver = new ChromeDriver(options);

            await SendInfo("Starting search... for " + url);
            
            // load this page url with all products on this search
            driver.Navigate()
                .GoToUrl(
                    url);

            // get the page source after it has fully loaded
            string pageSource = driver.PageSource;

            // can close browser no longer need.
            driver.Quit();

            // load the HTML source code into HtmlAgilityPack
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(pageSource);
            
            var newProducts = new List<product>();

            // now lets select each product element
            var productElements = htmlDoc.DocumentNode.SelectNodes("//div[@class='product']");
            if (productElements != null)
            {
                foreach (var productElement in productElements)
                {
                    var name = productElement.SelectSingleNode(".//h2[@class='pdp-grid-product-name']").InnerHtml;
                    
                    // Assuming you have already loaded the HTML into the 'htmlDoc' variable using HtmlAgilityPack
                    var priceInfoNode = productElement.SelectSingleNode(".//div[@class='price-info ']");
                    if (priceInfoNode != null)
                    {
                        var priceNode = priceInfoNode.SelectSingleNode(".//span[@class='value']");
                        if (priceNode != null)
                        {
                            string price = priceNode.InnerText.Trim();
                            if (price.EndsWith(".97"))
                            {

                                // product is on sale as ends with .97
                                Console.WriteLine($"Product {name} is on sale! /n please wait checking stock...:");

                                // lets check if this product is in stock.
                                var linkNode =
                                    productElement.SelectSingleNode(".//a[@class='link text-truncate pdpLink']");
                                var productUrl = linkNode.GetAttributeValue("href", "");

                                var fullUrl = $"https://www.currys.co.uk{productUrl}";

                                // now lets check if this product is in stock.
                                var inStock = await CheckIfInStock(fullUrl);
                                if (inStock)
                                    SendAlert(name, price, fullUrl).Wait();
                            }
                        }
                        else
                        {
                            Console.WriteLine("Price element not found within price-info.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Price-info element not found.");
                    }
                }

            }
            else
            {
                Console.WriteLine("No product elements found on the page.");
            }
            
            await SendInfo("Successfully checked all products..." + url);
        }

        catch (Exception ex)
        {
            await SendInfo("Error occurred..." + url);
            Console.WriteLine($"An error occurred: {ex.Message}");
            return;
        }
        finally
        {
            isRunning = false;
        }
        
    }

    static async Task<bool> CheckIfInStock(string url)
    {
        try
        {
            var options = new ChromeOptions();
            options.AddArguments("--log-level=3"); 
            var driver = new ChromeDriver(options);
            driver.Navigate().GoToUrl(url);
            // Wait for 3 seconds
            await Task.Delay(10000);

            driver.Close();
            
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(driver.PageSource);
            
            var outofstockbutton = htmlDoc.DocumentNode.SelectSingleNode(".//button[@class='add-to-cart btn cta-primary-btn out-of-stock-btn']");
            
            // if this button is null then the product is in stock.
            if (outofstockbutton == null)
            {
                Console.WriteLine("Product is in stock!");
                return true;
            }
            else
            {
                return false;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"An error occurred: {e.Message}");
        }

        return false;
    }
    
    // send alert to discord webhook
    static async Task SendAlert(string title, string price, string url)
    {
        var messege = $"Product is in stock!\nTitle: {title}\nPrice: {price}\nURL: {url}"; // Replace 'url' with your actual URL;

        if (sent.Contains(messege))
        {
            return;
        }
        
        try
        {
            var client = new HttpClient();

            // Construct the message content JSON
            var contentJson = new
            {
                content = $"Product is in stock!\nTitle: {title}\nPrice: {price}\nURL: {url}" // Replace 'url' with your actual URL
            };

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(contentJson);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            var webhookUrl = "https://discord.com/api/webhooks/1155534085716967465/jh_uqY5-i17jW_ZQvPK7oIpe5h7bRaZAGeY3Q4MZu_9pG9QgqdhjqNzVwqo3cjv2YOR5"; // Replace with your actual webhook URL
            var result = await client.PostAsync(webhookUrl, httpContent);

            if (result.IsSuccessStatusCode)
            {
                Console.WriteLine("Message sent successfully!");
                sent.Add(messege);
            }
            else
            {
                Console.WriteLine($"Failed to send message. Status code: {result.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }
    
    static async Task SendInfo(string messege)
    {
        try
        {
            var client = new HttpClient();

            // Construct the message content JSON
            var contentJson = new
            {
                content = $"{messege} --- -  {DateTime.Now}"
            };

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(contentJson);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            var webhookUrl = "https://discord.com/api/webhooks/1155536429640859801/iFNStjRMU5jqUY30GWOoz7SgCdr71YPl5v2XD7kkZGewbgeMeOt_0Bn8XHeQVbi5ny5H"; // Replace with your actual webhook URL
            var result = await client.PostAsync(webhookUrl, httpContent);

            if (result.IsSuccessStatusCode)
            {
                Console.WriteLine("Message sent successfully!");
            }
            else
            {
                Console.WriteLine($"Failed to send message. Status code: {result.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }
}
