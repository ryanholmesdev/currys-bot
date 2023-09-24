namespace currys_bot;

public class product
{
    public string name { get; set; }
    public string price { get; set; }
    public string url { get; set; }
    public bool inStock { get; set; }
    
    public product(string name, string price, string url, bool inStock)
    {
        this.name = name;
        this.price = price;
        this.url = url;
        this.inStock = inStock;
    }
}