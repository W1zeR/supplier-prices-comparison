using System.Globalization;
using System.Text;
using System.Text.Json;

namespace SupplierPricesComparison
{
    public class Program
    {
        static readonly CultureInfo Ru = CultureInfo.GetCultureInfo("ru-RU");

        static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            var path = args.Length > 0 ? args[0] : "input.json";

            (var isDataCorrect, var data) = LoadData(path);

            if (!isDataCorrect)
            {
                return 1;
            }

            var warnings = new List<string>();

            RemoveInvalid(data!, warnings);

            Console.WriteLine($"{"Артикул",-8}{"Кол-во",8}{"Было",10}  {"Поставщик",-18}{"Станет",10}{"Экономия",10}");
            Console.WriteLine(new string('-', 66));

            decimal totalSavings = 0;

            foreach (var item in data!.Invoice)
            {
                totalSavings += ProcessInvoiceItem(item, data.Offers);
            }

            Console.WriteLine(new string('-', 66));
            Console.WriteLine($"Суммарная потенциальная экономия: {Money(totalSavings)} рублей");

            foreach (var w in warnings)
                Console.WriteLine("Предупреждение: " + w);

            return 0;
        }

        static decimal ProcessInvoiceItem(InvoiceItem item, List<Offer> offers)
        {
            Offer? best = FindBestOffer(item, offers);

            var (supplier, newPrice, savings) = CalculateSavings(item, best);

            PrintRow(item, supplier, newPrice, savings);

            return savings;
        }

        static (string supplier, decimal newPrice, decimal savings) CalculateSavings(InvoiceItem item, Offer? best)
        {
            if (best == null)
            {
                return ("оставить текущего", item.UnitPrice, 0);
            }

            decimal savings = (item.UnitPrice - best.UnitPrice) * item.Quantity;

            return (best.Supplier, best.UnitPrice, savings);
        }

        static (bool isSuccess, InputData? data) LoadData(string path)
        {
            InputData? data;

            try
            {
                data = JsonSerializer.Deserialize<InputData>(File.ReadAllText(path), JsonOptions);
            }
            catch (Exception ex) when (ex is IOException || ex is JsonException)
            {
                Console.WriteLine($"Не удалось прочитать {path}: {ex.Message}");

                return (false, null);
            }

            if (data == null)
            {
                Console.WriteLine("Файл пустой");

                return (false, null);
            }

            return (true, data);
        }

        static void PrintRow(InvoiceItem item, string supplier, decimal newPrice, decimal savings)
        {
            Console.WriteLine($"{item.Sku,-8}{item.Quantity,8}{Money(item.UnitPrice),10}  {supplier,-18}{Money(newPrice),10}{Money(savings),10}");
        }

        static Offer? FindBestOffer(InvoiceItem item, List<Offer> offers)
        {
            Offer? best = null;

            foreach (var offer in offers)
            {
                if (offer.Sku != item.Sku) continue;
                if (offer.Stock < item.Quantity) continue;
                if (offer.UnitPrice >= item.UnitPrice) continue;

                if (best == null || offer.UnitPrice < best.UnitPrice)
                    best = offer;
            }

            return best;
        }

        static void RemoveInvalid(InputData data, List<string> warnings)
        {
            data.Invoice.RemoveAll(i => IsBad(
                string.IsNullOrWhiteSpace(i.Sku) || i.Quantity <= 0 || i.UnitPrice < 0,
                $"позиция счёта \"{i.Sku}\" пропущена (некорректные данные)",
                warnings));

            data.Offers.RemoveAll(o => IsBad(
                string.IsNullOrWhiteSpace(o.Sku) || string.IsNullOrWhiteSpace(o.Supplier) || o.UnitPrice < 0 || o.Stock < 0,
                $"предложение \"{o.Supplier}/{o.Sku}\" пропущено (некорректные данные)",
                warnings));
        }

        static bool IsBad(bool isBad, string message, List<string> warnings)
        {
            if (isBad)
            {
                warnings.Add(message);
            }

            return isBad;
        }

        static string Money(decimal value) => value.ToString("N0", Ru);
    }
}
