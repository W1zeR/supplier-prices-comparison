namespace SupplierPricesComparison
{
    public class InvoiceItem
    {
        public string Sku { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class Offer
    {
        public string Supplier { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Stock { get; set; }
    }

    public class InputData
    {
        public List<InvoiceItem> Invoice { get; set; } = [];
        public List<Offer> Offers { get; set; } = [];
    }
}
